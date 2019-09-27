using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Collections.ObjectModel;


using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.Common;
using Vintagestory.API.Config;

namespace FirstMachineAge
{
	/// <summary>
	/// Access controls mod (Internals, private implimentations)
	/// </summary>
	public partial class AccessControlsMod
	{
		private const string _domain = "FMA";
		private const string _AccessControlNodesKey = @"ACCESS_CONTROL_NODES";
		private const string _channel_name = @"AccessControl";
		internal const string _KeyIDKey = @"key_id";//for JSON attribute, DB key sequence
		internal const string _persistedStateKey = @"ACL_PersistedState";



		private ICoreServerAPI ServerAPI;
		private ServerMain ServerMAIN;
		private PlayerDataManager PlayerDatamanager;

		private ICoreAPI CoreAPI;
		private ICoreClientAPI ClientAPI;

		private ModSystemBlockReinforcement brs;

		private Dictionary<Vec3i, ChunkACNodes> Server_ACN;//Track changes - and commit every ## minutes, in addition to server shutdown data-storage, chunk unloads
		private Dictionary<BlockPos, LockCacheNode> Client_LockLookup;//By BlockPos - for fast local lookup. pre-computed by server...
		private ACLPersisted PersistedState;//Holds; Sequence counters...
		private SortedDictionary<string, HashSet<Vec3i>> previousChunkSet_byPlayerUID;
		private SortedDictionary<string, HashSet<int>> playerKeyIDs_byPlayerUID;//Future thread access collision?

		//Comm. Channels
		private IClientNetworkChannel accessControl_ClientChannel;
		private IServerNetworkChannel accessControl_ServerChannel;

		private Thread portunus_thread;


		#region Internals
		private void InitializeServerSide( )
		{
		//Replace blockBehaviorLockable - but only 'game' domain entires...
		var rawBytes = ServerAPI.WorldManager.SaveGame.GetData(_persistedStateKey);
		if (rawBytes != null && rawBytes.Length > 1) {
		this.PersistedState = SerializerUtil.Deserialize<ACLPersisted>(rawBytes);
		Mod.Logger.Debug("Loaded Persisted state");
		}
		else 
		{		
		ACLPersisted newPersistedState = new ACLPersisted( );

		var aclPersistBytes = SerializerUtil.Serialize<ACLPersisted>(newPersistedState);

		ServerAPI.WorldManager.SaveGame.StoreData(_persistedStateKey, aclPersistBytes);

		this.PersistedState = newPersistedState;
		Mod.Logger.Debug("Created Persisted state");
		}
					
		Server_ACN = new Dictionary<Vec3i, ChunkACNodes>();
		previousChunkSet_byPlayerUID = new SortedDictionary<string, HashSet<Vec3i>>( );
		playerKeyIDs_byPlayerUID = new SortedDictionary<string, HashSet<int>>( );

		//Await lock-GUI events, send cache updates via NW channel...
		accessControl_ServerChannel = ServerAPI.Network.RegisterChannel(_channel_name);
		accessControl_ServerChannel.RegisterMessageType<LockGUIMessage>( );
		accessControl_ServerChannel.SetMessageHandler<LockGUIMessage>(LockGUIMessageHandler);				

		ServerAPI.Event.PlayerJoin += TrackPlayerJoins;
		ServerAPI.Event.PlayerLeave += TrackPlayerLeaves;

		portunus_thread = new Thread(Portunus);
		portunus_thread.Name = "Portunus";
		portunus_thread.Priority = ThreadPriority.Lowest;
		portunus_thread.IsBackground = true;

		//Re-wake Portunus to send out _possible_ updates for mutated LockStatus changes, and save altered ACL from chunks...
		ServerAPI.Event.RegisterGameTickListener(AwakenPortunus, 1000);

		//Attach events to persist ACL data to chunks on server shutdown		
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, PreloadACLData);
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, PersistACLData);
		//TODO: Also chunk unload events??? (ideally - should be moot since data would mabey be already saved?)
		ServerAPI.Event.DidBreakBlock += RemoveACN_byBlockBreakage;

		Mod.Logger.StoryEvent("...a tumbler turns, and opens\t*click*");
		}

		private void InitializeClientSide( )
		{
		accessControl_ClientChannel = ClientAPI.Network.RegisterChannel(_channel_name);
		accessControl_ClientChannel.RegisterMessageType<LockStatusList>( );

		accessControl_ClientChannel.SetMessageHandler<LockStatusList>(RecieveACNUpdate);//RX: Cache update 

		Client_LockLookup = new Dictionary<BlockPos, LockCacheNode>( );

		}

		internal void LoadACN_fromChunk(Vec3i chunkPos)
		{
		//Retrieve and add to local cache
		IServerChunk targetChunk;
		byte[ ] data = null;
		long chunkIndex = ServerAPI.World.BulkBlockAccessor.ToChunkIndex3D(chunkPos);

		if (!ServerAPI.WorldManager.AllLoadedChunks.TryGetValue(chunkIndex, out targetChunk)) {
		//An unloaded chunk huh...
		Mod.Logger.Debug("Un-loaded chunk hit! {0}", chunkPos);

		targetChunk = ServerAPI.WorldManager.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
		}
		
		//TODO: Remove when bug in API is fixed!
		if (targetChunk is ServerChunk) 
			{
			ServerChunk srvChunk = targetChunk as ServerChunk;

			if (srvChunk.ServerSideModdata == null) 
			{
			srvChunk.ServerSideModdata = new Dictionary<string, byte[ ]>( );			
			}

			data = srvChunk.GetServerModdata(_AccessControlNodesKey);
			}

		if (data != null && data.Length > 0) {
		#if DEBUG
		Mod.Logger.VerboseDebug("Data for ACNs present in chunk: {0}", chunkPos);
		#endif

		ChunkACNodes acNodes = SerializerUtil.Deserialize<ChunkACNodes>(data);

		Server_ACN.Add(chunkPos.Clone( ), acNodes);
		}
		else if (targetChunk != null){
		#if DEBUG
		Mod.Logger.VerboseDebug("Absent ACN structures for chunk: {0}", chunkPos);
		#endif
		//Setup new AC Node list for this chunk.
		ChunkACNodes newAcNodes = new ChunkACNodes( );

		Server_ACN.Add(chunkPos.Clone( ), newAcNodes);
		}

		}

		private void PreloadACLData( )
		{
		Mod.Logger.VerboseDebug("pre-load for {0} chunks", ServerAPI.WorldManager.AllLoadedChunks.Keys.Count);
		long[ ] pileOfindex3Ds = new long[ServerAPI.WorldManager.AllLoadedChunks.Keys.Count];
		ServerAPI.WorldManager.AllLoadedChunks.Keys.CopyTo(pileOfindex3Ds, 0);

		foreach (var index3D in pileOfindex3Ds) {
		Vec3i chunkLoc = ServerMAIN.WorldMap.ChunkPosFromChunkIndex3D(index3D);
		LoadACN_fromChunk(chunkLoc);
		}
		}

		private void PersistACLData( )
		{
		Mod.Logger.Debug("ACN Data persistence routines activated");

		foreach (var entry in this.Server_ACN) {
		if (entry.Value.Altered == false) continue;

		IServerChunk targetChunk;

		long chunkIndex = ServerAPI.World.BulkBlockAccessor.ToChunkIndex3D(entry.Key);

		if (ServerAPI.WorldManager.AllLoadedChunks.TryGetValue(chunkIndex, out targetChunk)) {

		byte[ ] data = SerializerUtil.Serialize<ChunkACNodes>(entry.Value);
		targetChunk.SetServerModdata(_AccessControlNodesKey, data);
		}
		else {
		//An unloaded chunk...argh!.
		//throw new ApplicationException("This was a Terrible Idea - loading a chunk while shutting down!");

		targetChunk = ServerAPI.WorldManager.GetChunk(entry.Key.X, entry.Key.Y, entry.Key.Z);
		byte[ ] data = SerializerUtil.Serialize<ChunkACNodes>(entry.Value);
		targetChunk.SetServerModdata(_AccessControlNodesKey, data);
		}



		}
		}

		internal int NextKeyID {
			get { return ++PersistedState.KeyId_Sequence; }
		}


		internal void AlterLockAt(BlockSelection blockSel, IPlayer player, LockKinds lockType, byte[ ] combinationCode = null, uint? keyCode = null)
		{

		}

		/// <summary>
		/// Invoke when player has **SUCCESSFULLY** added a lock to some block
		/// </summary>
		/// <param name="pos">Block Position.</param>
		/// <param name="theLock">The subject lock.</param>
		protected void AddLock_ClientCache(BlockPos pos, GenericLock theLock)
		{
		if (this.Client_LockLookup.ContainsKey(pos.Copy( ))) {
		Mod.Logger.Warning("Can't overwrite cached lock entry located: {0}", pos);
		}
		else {
		var lockStateNode = new LockCacheNode( );

		lockStateNode.Tier = theLock.LockTier;

		switch (theLock.LockStyle) {
		case LockKinds.None:
			Mod.Logger.Error("Adding a non-lock to ClientCache, this is in error!");
			break;

		case LockKinds.Classic:
			lockStateNode.LockState = LockStatus.Unlocked;
			break;

		case LockKinds.Combination:
			lockStateNode.LockState = LockStatus.ComboKnown;
			break;

		case LockKinds.Key:
			lockStateNode.LockState = LockStatus.KeyHave;//Track via Inventory update watcher, client side too?
			break;
		}


		this.Client_LockLookup.Add(pos.Copy( ), lockStateNode);
		}
		}

		protected void RemoveLock_ClientCache(BlockPos pos)
		{
		if (this.Client_LockLookup.ContainsKey(pos.Copy( ))) {
		this.Client_LockLookup.Remove(pos.Copy( ));
		}
		else {
		Mod.Logger.VerboseDebug("Trying to remove non-existant cache entry: {0}", pos);
		}
		}


		internal void Send_Lock_GUI_Message(string playerUID, BlockPos position, byte[ ] comboGuess)
		{
		//Client Side:  send guess attempt to server
		LockGUIMessage msg = new LockGUIMessage(position, comboGuess);

		accessControl_ClientChannel.SendPacket<LockGUIMessage>(msg);

		#if DEBUG
		Mod.Logger.VerboseDebug("Sent message triggerd by Lock GUI");
		#endif
		}

		private void LockGUIMessageHandler(IServerPlayer fromPlayer, LockGUIMessage networkMessage)
		{
		var subjectACN = RetrieveACN(networkMessage.position);
		if (subjectACN.LockStyle == LockKinds.Combination) {
		if (subjectACN.CombinationCode == networkMessage.comboGuess) {
		Mod.Logger.Notification("Player {0} used correct combination; for lock @{1}", fromPlayer.PlayerName, networkMessage.position.PrettyCoords(CoreAPI));

		//Update server ACN
		subjectACN = GrantIndividualPlayerAccess(fromPlayer, networkMessage.position, "guessed combo");

		//Send single entry client ACL update message back
		SendClientACNUpdate(fromPlayer, networkMessage.position, subjectACN);


		}
		else {
		Mod.Logger.Notification("Player {0} tried wrong combination; for lock @{1}", fromPlayer.PlayerName, networkMessage.position.PrettyCoords(CoreAPI));
		//TODO: Track count of players failed guesses.

		}
		}
		}

		private void RecieveACNUpdate(LockStatusList networkMessage)
		{
		//Client side; update local cache from whatever server sent

		if (networkMessage != null && networkMessage.LockStatesByBlockPos != null) {
		#if DEBUG
		Mod.Logger.VerboseDebug("ACN Rx from Server; {0} nodes", networkMessage.LockStatesByBlockPos.Count);
		#endif

		foreach (var update in networkMessage.LockStatesByBlockPos) {
		if (Client_LockLookup.ContainsKey(update.Key)) {

		if (update.Value.LockState != LockStatus.None) {
		//Replace
		Client_LockLookup[update.Key.Copy( )] = update.Value;
		}
		else {
		Client_LockLookup.Remove(update.Key.Copy( ));
		}

		}
		else {
		//New
		Client_LockLookup.Add(update.Key.Copy( ), update.Value);
		}

		}
		}
		}

		private void SendClientACNUpdate(IServerPlayer toPlayer, BlockPos pos, AccessControlNode subjectACN)
		{
		//Send packet to client on channel - ONE ACN update only [Single lock update]
		
		LockStatusList lsl = ComputeLSLFromACN(pos, subjectACN, toPlayer);

		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl);
		}



		private void SendClientACNMultiUpdates(IServerPlayer toPlayer, ICollection<KeyValuePair<BlockPos, AccessControlNode>> nodesList)
		{
		//TODO: side-Thread this ?
		ConnectedClient client = this.ServerMAIN.GetClientByUID(toPlayer.PlayerUID);

		if (client.IsPlayingClient) {
		//Pre-cooked 'cache' ACLs Computed for the current player in question
		LockStatusList lsl = ComputeLSLFromACNs(nodesList, toPlayer);

		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl, toPlayer);
		}
		}

		private void UpdateBroadcast(IServerPlayer byPlayer,  BlockPos blockPos,  AccessControlNode updatedLock)
		{

		foreach (IServerPlayer tgtPlayer in ServerAPI.World.AllOnlinePlayers) 
		{		
		LockStatusList specificLSL = ComputeLSLFromACN(blockPos, updatedLock, tgtPlayer);

		accessControl_ServerChannel.SendPacket<LockStatusList>(specificLSL, tgtPlayer);
		}

		}

		private void TrackPlayerJoins(IServerPlayer byPlayer)
		{
		previousChunkSet_byPlayerUID.Add(byPlayer.PlayerUID, new HashSet<Vec3i>( ));
		playerKeyIDs_byPlayerUID.Add(byPlayer.PlayerUID, PlayersKeyInventory(byPlayer));

		AttachInventoryObservers(byPlayer);
		
		AwakenPortunus(0f);//Send LSL's for this new-player
		}

		private void TrackPlayerLeaves(IServerPlayer byPlayer)
		{
		previousChunkSet_byPlayerUID.Remove(byPlayer.PlayerUID);
		playerKeyIDs_byPlayerUID.Remove(byPlayer.PlayerUID);
		}

		private void AwakenPortunus(float delayed)
		{
		//Start / Re-start thread to computed ACL node list to clients
		#if DEBUG
			Mod.Logger.VerboseDebug("Portunus re-trigger [{0}]", portunus_thread.ThreadState);
		#endif

		if (portunus_thread.ThreadState == ThreadState.Unstarted) {
		portunus_thread.Start( );
		}
		else if (portunus_thread.ThreadState == ThreadState.WaitSleepJoin) {
		//(re)Wake the sleeper!
		portunus_thread.Interrupt( );
		}
		}

		private void Portunus( )
		{
	wake:

		try {
		//For all online players - ACN's for *new* chunks entered/in
		foreach (var player in ServerAPI.World.AllOnlinePlayers) {//TODO: Parallel.ForEach

		//var client = this.ServerMAIN.GetConnectedClient(player.PlayerUID);

		var center = ServerAPI.World.BlockAccessor.ToChunkPos(player.Entity.ServerPos.AsBlockPos.Copy( ));

		var alreadyUpdatedSet = previousChunkSet_byPlayerUID[player.PlayerUID];
		//All of them for nearest 27 CHUNKs, contacting ['new' Chunk ]
		var fresh_chunks = Helpers.ComputeChunkBubble(center).Except(alreadyUpdatedSet).ToList( );

		var introducedNodes = new List<KeyValuePair<BlockPos, AccessControlNode>>( );

		foreach (var chunkPosUpdate in fresh_chunks) {
		var ACNs_byChunk = RetrieveACNs_ByChunk(chunkPosUpdate);

		if (ACNs_byChunk != null && ACNs_byChunk.Count > 0) {
		introducedNodes.AddRange(ACNs_byChunk);
		}
		}

		if (introducedNodes.Count > 0) {
		#if DEBUG
		Mod.Logger.VerboseDebug("Player {0} will get {1} ACNs from chunk {2}", player.PlayerName, introducedNodes.Count, center);
		#endif

		SendClientACNMultiUpdates(player as IServerPlayer, introducedNodes);

		previousChunkSet_byPlayerUID[player.PlayerUID].AddRange(fresh_chunks);
		}

		//Keys should already be marking their previous/current owner (called externally) - ACN




		//var chunk_ACLs_Cached = this.previousChunkSet_byPlayerUID.

		}

		//Persist & SAVE-COMMIT Altered ACNs !

		foreach (var alteredEntry in Server_ACN.TakeWhile(node => node.Value.Altered == true)) {
		byte[ ] data = SerializerUtil.Serialize<ChunkACNodes>(alteredEntry.Value);

		IServerChunk updatingChunk = ServerAPI.WorldManager.GetChunk(alteredEntry.Key.X, alteredEntry.Key.Y, alteredEntry.Key.Z);

		updatingChunk.SetServerModdata(_AccessControlNodesKey, data);

		alteredEntry.Value.Altered = false;
		}

		//Then sleep until interupted again, and repeat

		Mod.Logger.VerboseDebug("Thread '{0}' about to sleep indefinitely.", Thread.CurrentThread.Name);

		Thread.Sleep(Timeout.Infinite);

		} catch (ThreadInterruptedException) {

		Mod.Logger.VerboseDebug("Thread '{0}' awoken.", Thread.CurrentThread.Name);
		goto wake;

		} catch (ThreadAbortException) {
		Mod.Logger.VerboseDebug("Thread '{0}' aborted.", Thread.CurrentThread.Name);

		} finally {
		Mod.Logger.VerboseDebug("Thread '{0}' executing finally block.", Thread.CurrentThread.Name);
		}
		}


		private LockStatusList ComputeLSLFromACNs(ICollection<KeyValuePair<BlockPos, AccessControlNode>> nodesByPos, IServerPlayer byPlayer)
		{
		var stati = new Dictionary<BlockPos, LockCacheNode>( );

		foreach (var entry in nodesByPos) {
		bool locked = EvaulateACN_Rule(byPlayer, entry.Value);

		LockCacheNode lcn = new LockCacheNode( );

		switch (entry.Value.LockStyle) {
		case LockKinds.None:
			lcn.LockState = LockStatus.None;//Only used for 'remove' orders...
			break;

		case LockKinds.Classic:
			lcn.LockState = locked ? LockStatus.Locked : LockStatus.Unlocked;
			break;

		case LockKinds.Combination:
			lcn.LockState = locked ? LockStatus.ComboUnknown : LockStatus.ComboKnown;
			lcn.Tier = entry.Value.Tier;
			break;

		case LockKinds.Key:
			lcn.LockState = locked ? LockStatus.KeyNope : LockStatus.KeyHave;
			break;
		}

		stati.Add(entry.Key, lcn);
		}

		return new LockStatusList(stati);
		}

		private LockStatusList ComputeLSLFromACN(BlockPos blockPos , AccessControlNode updatedLock, IServerPlayer tgtPlayer)
		{

		bool locked = EvaulateACN_Rule(tgtPlayer, updatedLock);

		LockCacheNode lcn = new LockCacheNode( );

		switch (updatedLock.LockStyle) {
		case LockKinds.None:
			lcn.LockState = LockStatus.None;//Only used for 'remove' orders...
			break;

		case LockKinds.Classic:
			lcn.LockState = locked ? LockStatus.Locked : LockStatus.Unlocked;
			break;

		case LockKinds.Combination:
			lcn.LockState = locked ? LockStatus.ComboUnknown : LockStatus.ComboKnown;
			lcn.Tier = updatedLock.Tier;
			break;

		case LockKinds.Key:
			lcn.LockState = locked ? LockStatus.KeyNope : LockStatus.KeyHave;
			break;
		}

		LockStatusList singleLockState = new LockStatusList(blockPos, lcn);

		return singleLockState;
		}

		/// <summary>
		/// Evaulates the AC node rule.
		/// </summary>
		/// <returns>TRUE == LOCKED</returns>
		/// <param name="forPlayer">For player.</param>
		/// <param name="controlNode">Control node.</param>
		private bool EvaulateACN_Rule(IPlayer forPlayer, AccessControlNode controlNode)
		{
		if (controlNode.LockStyle == LockKinds.Classic || controlNode.LockStyle == LockKinds.Combination) {
		//Is it yours?
		if (controlNode.OwnerPlayerUID == forPlayer.PlayerUID) return false;

		//In same faction? 
		if (controlNode.PermittedPlayers != null & controlNode.PermittedPlayers.Count > 0) {

		foreach (var perp in controlNode.PermittedPlayers) {
		if (perp.PlayerUID == forPlayer.PlayerUID) {
		return false;//By discrete entry - combo's have these
		}

		if (perp.GroupID.HasValue) {
		PlayerGroup targetGroup = PlayerDatamanager.PlayerGroupsById[perp.GroupID.Value];
		if (PlayerDatamanager.PlayerDataByUid.ContainsKey(forPlayer.PlayerUID)) {
		ServerPlayerData theirGroup = PlayerDatamanager.PlayerDataByUid[forPlayer.PlayerUID];

		if (theirGroup.PlayerGroupMemberships.ContainsKey(perp.GroupID.Value)) {
		return false;//Is member of group, thus granted.
		}
		}
		}

		}
		}

		return true; //Locked BY DEFAULT: [Classic-lock] or [Combo-locks]
		}
		else if (controlNode.LockStyle == LockKinds.Key) //************** End of: Classic / Combination LOCKS ***********
		{
		//Inventory key moves tracked - just verify a player 'has' matching key ID in theirs

		HashSet<int> keys = playerKeyIDs_byPlayerUID[forPlayer.PlayerUID];

		if (controlNode.KeyID.HasValue) {
		return !keys.Contains(controlNode.KeyID.Value);
		}
		else {
		Mod.Logger.Warning("Problem; a Key ACN has no KeyID#  [OwnerUID: {0}]", controlNode.OwnerPlayerUID);
		}

		return true;
		}//************** End of: KEY LOCKS ***********


		return false;//No lock here!
		}

		private HashSet<int> PlayersKeyInventory(IServerPlayer byPlayer)
		{
		HashSet<int> keyIds = new HashSet<int>( );

		foreach (var inventory in byPlayer.InventoryManager.Inventories) {
		foreach (ItemSlot slot in inventory.Value) {
		if (slot.Empty) continue;

		if (slot.Itemstack.Class == EnumItemClass.Item &&
			slot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)) 
			{
			var keyId = GenericKey.KeyID(slot.Itemstack);
			//PlayerUID ?
			keyIds.Add(keyId);
			}
		}

		}

		return keyIds;
		}

		private void AttachInventoryObservers(IServerPlayer byPlayer)
		{
		Mod.Logger.VerboseDebug("Attach Inventory monitors for: {0}", byPlayer.PlayerName);

		IInventory hotbarInv = byPlayer.InventoryManager.GetHotbarInventory( );
		IInventory backpacksInv = byPlayer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
		IInventory craftGridInv = byPlayer.InventoryManager.GetOwnInventory(GlobalConstants.craftingInvClassName);		
		IInventory groundInv = byPlayer.InventoryManager.GetOwnInventory(GlobalConstants.groundInvClassName);
		IInventory mouseInv = byPlayer.InventoryManager.GetOwnInventory(GlobalConstants.mousecursorInvClassName);
		
			hotbarInv.SlotModified += (slotNum) => { AttainKeyBySlotChange(byPlayer.PlayerUID , GlobalConstants.hotBarInvClassName ,slotNum); };

			backpacksInv.SlotModified += (slotNum) => { AttainKeyBySlotChange(byPlayer.PlayerUID, GlobalConstants.backpackInvClassName, slotNum); };

			craftGridInv.SlotModified += (slotNum) => {	AttainKeyBySlotChange(byPlayer.PlayerUID, GlobalConstants.craftingInvClassName, slotNum); };
			//Ground, mouse -> remove items
			groundInv.SlotModified += (slotNum) => { LoseKeyBySlotChange(byPlayer.PlayerUID, GlobalConstants.groundInvClassName, slotNum); };

			mouseInv.SlotModified += (slotNum) => { LoseKeyBySlotChange(byPlayer.PlayerUID, GlobalConstants.mousecursorInvClassName, slotNum); };
		}


		internal void AttainKeyBySlotChange(string playerUID, string inventoryClass, int slotNum)
		{
		int? keyID;
		BlockPos lockLocation;
		var player = ServerAPI.World.PlayerByUid(playerUID);

		IInventory theInv = player.InventoryManager.GetOwnInventory(inventoryClass);

		ItemSlot alteredSlot = theInv[slotNum];

		if (alteredSlot.Empty) return;

		if (alteredSlot.StorageType == EnumItemStorageFlags.General &&
			alteredSlot.Itemstack.Class == EnumItemClass.Item &&
			alteredSlot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)
	   		) 
			{
			#if DEBUG		
				Mod.Logger.VerboseDebug("Key appears on slot# {0} in Inv: {1} for {2}", slotNum, alteredSlot.Inventory.InventoryID, player.PlayerName);
			#endif
							
			var ivbp = alteredSlot.Inventory as InventoryBasePlayer;

			keyID = GenericKey.KeyID(alteredSlot.Itemstack);
			lockLocation = GenericKey.LockLocation(alteredSlot.Itemstack);

			if (keyID.HasValue && playerKeyIDs_byPlayerUID.ContainsKey(playerUID)) 
			{
				playerKeyIDs_byPlayerUID[playerUID].Add(keyID.Value);
			}
			
			
			}
		

		}

		private void RemoveACN_byBlockBreakage(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
		{
		BlockPos adjPos = blockSel.Position.Copy( ); 
		AdjustBlockPostionForMultiBlockStructure(ref adjPos);
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(adjPos);
			if (Server_ACN.ContainsKey(chunkPos)) 
			{
				if (Server_ACN[chunkPos].Entries.ContainsKey(adjPos)) {
					var toRemoveACN = Server_ACN[chunkPos].Entries[adjPos];
					toRemoveACN.LockStyle = LockKinds.None;

					Server_ACN[chunkPos].Entries.Remove(adjPos);

					UpdateBroadcast(byPlayer, adjPos, toRemoveACN);

					Mod.Logger.Notification("player {0} broke @({1}) with ACN Owned by [{2}]", byPlayer.PlayerName, adjPos, toRemoveACN.OwnerPlayerUID);
				}
			}	
		}

		internal void LoseKeyBySlotChange(string playerUID, string inventoryClass, int slotNum)
		{
		int? keyID;
		BlockPos lockLocation;
		var player = ServerAPI.World.PlayerByUid(playerUID);

		IInventory theInv = player.InventoryManager.GetInventory(inventoryClass);

		ItemSlot alteredSlot = theInv[slotNum];

		if (alteredSlot.Empty) return;

		if (alteredSlot.StorageType == EnumItemStorageFlags.General &&
			alteredSlot.Itemstack.Class == EnumItemClass.Item &&
			alteredSlot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)
	   		) {
			#if DEBUG
			Mod.Logger.VerboseDebug("Key lost on slot# {0} in Inv: {1} for {2}", slotNum, alteredSlot.Inventory.InventoryID, player.PlayerName);
			#endif

			var ivbp = alteredSlot.Inventory as InventoryBasePlayer;

			keyID = GenericKey.KeyID(alteredSlot.Itemstack);
			lockLocation = GenericKey.LockLocation(alteredSlot.Itemstack);

			if (keyID.HasValue && playerKeyIDs_byPlayerUID.ContainsKey(playerUID)) 
				{
				playerKeyIDs_byPlayerUID[playerUID].Remove(keyID.Value);
				}
			}
		}



		#endregion
	}
}

