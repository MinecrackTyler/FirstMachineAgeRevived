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
		private const string _domain = @"fma";
		private const string _AccessControlNodesKey = @"ACCESS_CONTROL_NODES";
		private const string _channel_name = @"AccessControl";
		internal const string _KeyIDKey = @"key_id";//for JSON attribute, DB key sequence
		internal const string _persistedStateKey = @"ACL_PersistedState";



		private ICoreServerAPI ServerAPI;
		private ServerMain ServerMAIN;
		private PlayerDataManager PlayerDatamanager;

		private ICoreAPI CoreAPI;
		private ICoreClientAPI ClientAPI;

		//private ModSystemBlockReinforcement brs;
		private ACLPersisted PersistedState;//Holds; Sequence counters...
		private Dictionary<Vec3i, ChunkACNodes> Server_ACN;//Track changes - and commit every ## minutes, in addition to server shutdown data-storage, chunk unloads
		private Dictionary<BlockPos, LockCacheNode> Client_LockLookup;//By BlockPos - for fast local lookup. pre-computed by server...

		private SortedDictionary<string, HashSet<Vec3i>> previousChunkSet_byPlayerUID;
		private SortedDictionary<int, KeyValuePair<BlockPos, AccessControlNode>> ACNs_byKeyID;//Built on extraction of ACN when loading chunks [1:1]

		private SortedDictionary<string, HashSet<int>> playerKeyIDs_byPlayerUID;//All Keys ~current
		private SortedDictionary<string, HashSet<int>> playerLostKeyIDs_byPlayerUID;//Kept only for single cycle
		private SortedDictionary<string, HashSet<int>> playerGainKeyIDs_byPlayerUID;//Kept only for single cycle


		//Comm. Channels
		private IClientNetworkChannel accessControl_ClientChannel;
		private IServerNetworkChannel accessControl_ServerChannel;

		private Thread portunus_thread;


		#region Internals
		private void InitializeServerSide( )
		{
		//Replace blockBehaviorLockable - but only 'game' domain entires...
		var rawBytes = ServerAPI.WorldManager.SaveGame.GetData(_persistedStateKey);
		if (rawBytes != null ) {
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
		playerLostKeyIDs_byPlayerUID = new SortedDictionary<string, HashSet<int>>( );
		playerGainKeyIDs_byPlayerUID = new SortedDictionary<string, HashSet<int>>( );
		ACNs_byKeyID = new SortedDictionary<int, KeyValuePair<BlockPos, AccessControlNode>>( );

		//Await lock-GUI events, send cache updates via NW channel...
		accessControl_ServerChannel = ServerAPI.Network.RegisterChannel(_channel_name);
		accessControl_ServerChannel = accessControl_ServerChannel.RegisterMessageType<LockGUIMessage>( );
		accessControl_ServerChannel = accessControl_ServerChannel.RegisterMessageType<LockStatusList>( );
		accessControl_ServerChannel = accessControl_ServerChannel.SetMessageHandler<LockGUIMessage>(LockGUIMessageHandler);				

		ServerAPI.Event.PlayerJoin += TrackPlayerJoins;
		ServerAPI.Event.PlayerLeave += TrackPlayerLeaves;

		portunus_thread = new Thread(Portunus);
		portunus_thread.Name = "Portunus";
		portunus_thread.Priority = ThreadPriority.Lowest;
		portunus_thread.IsBackground = true;

		//Re-wake Portunus to send out _possible_ updates for mutated LockStatus changes, and save altered ACL from chunks...
		ServerAPI.Event.RegisterGameTickListener(AwakenPortunus, 2000);

		//Attach events to persist ACL data to chunks on server shutdown		
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, PreloadACLData);
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, PersistACLData);
		//TODO: Also chunk unload events??? (ideally - should be moot since data would mabey be already saved?)
		ServerAPI.Event.DidBreakBlock += RemoveACN_byBlockBreakage;
		ServerAPI.RegisterCommand(new LocksmithCmd(this.ServerAPI));

		Mod.Logger.StoryEvent("...a tumbler turns, and opens\t*click*");
		Mod.Logger.VerboseDebug("ACN done server-side Init");
		}

		private void RegisterStuff(ICoreAPI api)
		{

		api.RegisterItemClass("ItemCombolock", typeof(ItemCombolock));
		api.RegisterItemClass("ItemKeylock", typeof(ItemKeylock));
		api.RegisterItemClass("ItemKey", typeof(GenericKey));
		api.RegisterBlockBehaviorClass("Lockable", typeof(BlockBehaviorComplexLockable));		
		}

		private void InitializeClientSide( )
		{
		Mod.Logger.VerboseDebug("Server side channel: \"{0}\" is {1} ", _channel_name,ClientAPI.Network.GetChannelState(_channel_name));
		accessControl_ClientChannel = ClientAPI.Network.RegisterChannel(_channel_name);
		accessControl_ClientChannel = accessControl_ClientChannel.RegisterMessageType<LockGUIMessage>( );
		accessControl_ClientChannel = accessControl_ClientChannel.RegisterMessageType<LockStatusList>( );

		accessControl_ClientChannel = accessControl_ClientChannel.SetMessageHandler<LockStatusList>(RecieveACNUpdate);//RX: Cache update 

		Mod.Logger.Debug("{0} channel connected: {1}", accessControl_ClientChannel.ChannelName, accessControl_ClientChannel.Connected);

		Client_LockLookup = new Dictionary<BlockPos, LockCacheNode>( );
		Mod.Logger.VerboseDebug("ACN done client-side Init");
		}

		internal bool ACN_IsNew(BlockPos blockPos)
		{
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(blockPos);

		if (Server_ACN.ContainsKey(chunkPos )) 
		{
			if (Server_ACN[chunkPos].Entries.ContainsKey(blockPos)) 
				{
					return false;
				}
		}

		return true;
		}

		internal void LoadACN_fromChunk(Vec3i chunkPos, bool verboseMsg = false)
		{
		//ATTEMPT to Retrieve and add to local cache: ACN, KeyID lookups...
		IServerChunk targetChunk;
		byte[ ] data = null;
		long chunkIndex = ServerAPI.World.BulkBlockAccessor.ToChunkIndex3D(chunkPos);
					
		if (!ServerAPI.WorldManager.AllLoadedChunks.TryGetValue(chunkIndex, out targetChunk)) {
		//An unloaded chunk huh...
		Mod.Logger.Debug("Un-loaded chunk hit! {0}", chunkPos);

		targetChunk = ServerAPI.WorldManager.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
		}

		if (targetChunk != null) 
		{
		data = targetChunk.GetServerModdata(_AccessControlNodesKey);		
		}

		if (data != null && data.Length > 0) {
		
		ChunkACNodes existingNodes = SerializerUtil.Deserialize<ChunkACNodes>(data);

		#if DEBUG
		Mod.Logger.VerboseDebug("ACNs present in chunk: {0} - Nodes# {1}", chunkPos,existingNodes.Entries.Count);
		#endif

		Server_ACN.Add(chunkPos.Clone( ), existingNodes);

		var keyCounter = existingNodes.Entries.Count(acn => acn.Value.LockStyle == LockKinds.Key);
		if (keyCounter > 0) 
		{
		Mod.Logger.VerboseDebug("ACN has {0} key-lock entries to track", keyCounter);

		foreach (KeyValuePair<BlockPos, AccessControlNode> kvp in existingNodes.Entries.Where(acn => acn.Value.LockStyle == LockKinds.Key)) 
		{
			if (kvp.Value.KeyID.HasValue) {
			ACNs_byKeyID.Add(kvp.Value.KeyID.Value, kvp);
			}
		}

		}

		} else {
		#if DEBUG
		if (verboseMsg) Mod.Logger.VerboseDebug("Absent ACN data for chunk: {0}! (placeholder added)", chunkPos);
		#endif
		//Setup new AC Node list for this chunk.
		ChunkACNodes placeHolderNodes = new ChunkACNodes();

		Server_ACN.Add(chunkPos.Clone( ), placeHolderNodes);
		}

		}

		internal void AddACN_ToServerACNs(BlockPos blockPos, AccessControlNode node )
		{
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(blockPos);
		bool success = false;

		if (Server_ACN.ContainsKey(chunkPos) ) {

			if (Server_ACN[chunkPos].Entries.ContainsKey(blockPos)) 
			{
			var existingACN = Server_ACN[chunkPos].Entries[blockPos];
				if (existingACN.LockStyle == LockKinds.None) {
				Server_ACN[chunkPos].Entries[blockPos] = node;
				Server_ACN[chunkPos].Altered = true;
						Mod.Logger.Debug("Overwrote 'None' -> '{0}', Chunk at {1}", node.LockStyle, chunkPos);
				success = true;
				}
				else {						
					Mod.Logger.Error("Rejecting overwrite of ACN @{0} was: ({1}){2} to ({3}){4}", blockPos, 
					                existingACN.OwnerPlayerUID,
				                 	existingACN.LockStyle,
									node.OwnerPlayerUID,
									node.LockStyle
					                );
				}
			}
			else {
			Mod.Logger.Debug("Appending New ACN Chunk at {0}", chunkPos);
			Server_ACN[chunkPos].Entries.Add(blockPos, node);
			Server_ACN[chunkPos].Altered = true;
			success = true;
			}

		}
		else {
		Mod.Logger.Debug("Created ChunkACNodes for {0}", chunkPos);
		Server_ACN.Add(chunkPos, new ChunkACNodes(chunkPos));
		Server_ACN[chunkPos].Entries.Add(blockPos, node);
		Server_ACN[chunkPos].Altered = true;
		Server_ACN[chunkPos].OriginChunk = chunkPos.Clone( );
		success = true;
		}

		if (success && node.LockStyle == LockKinds.Key) {
			if (ACNs_byKeyID.ContainsKey(node.KeyID.Value)) {
			//Duplicate??
			Mod.Logger.Error("Duplicate ACN_byKeyID ?! #{0} @{1}", node.KeyID.Value, blockPos);
			}
			else {
				ACNs_byKeyID.Add(node.KeyID.Value, new KeyValuePair<BlockPos, AccessControlNode>(blockPos, node));
			}

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

		var alteredCount = Server_ACN.Count(ac => ac.Value.Altered == true);
		if (alteredCount > 0) Mod.Logger.Debug("There are {0} unsaved chunk Nodes ( should be zero !)", alteredCount);

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

		var aclPersistBytes = SerializerUtil.Serialize<ACLPersisted>(this.PersistedState);
		ServerAPI.WorldManager.SaveGame.StoreData(_persistedStateKey, aclPersistBytes);
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
		protected void AddLock_ClientCache(BlockPos pos, GenericLock theLock, IPlayer owner)
		{
		if (this.Client_LockLookup.ContainsKey(pos.Copy( ))) {
		Mod.Logger.Warning("Can't overwrite cached lock entry located: {0}", pos);
		}
		else {				
		var lockStateNode = new LockCacheNode( );

		lockStateNode.Tier = theLock.LockTier;
		lockStateNode.OwnerName = owner.PlayerName;

		switch (theLock.LockStyle) {
		case LockKinds.None:
			Mod.Logger.Error("Adding a non-lock to ClientCache, this is in error!");//Adding that is. Place-holders may exist like this
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
		Mod.Logger.Debug("Added cached lock entry located: {0}", pos);
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
		if (networkMessage != null && networkMessage.LockStatesByBlockPos != null) 
		{
			#if DEBUG
			Mod.Logger.VerboseDebug("Rx from Server; {0} LSL-nodes", networkMessage.LockStatesByBlockPos.Count);
			#endif

			foreach (var update in networkMessage.LockStatesByBlockPos) 
			{
			#if DEBUG
			if (update.Value.LockState != LockStatus.None) Mod.Logger.VerboseDebug("pos {0} LS: {1}", update.Key, update.Value.LockState);
			#endif
			if (Client_LockLookup.ContainsKey(update.Key)) 
				{
				//Replace
				Client_LockLookup[update.Key.Copy( )] = update.Value;		
				}
			else 
				{
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

		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl, toPlayer);
		}



		private void SendClientACNMultiUpdates(IServerPlayer toPlayer, ICollection<KeyValuePair<BlockPos, AccessControlNode>> nodesList)
		{
		//TODO: side-Thread this ?
		ConnectedClient client = this.ServerMAIN.GetClientByUID(toPlayer.PlayerUID);

		if (client.State != EnumClientState.Offline) {
		//Pre-cooked 'cache' ACLs Computed for the current player in question
		LockStatusList lsl = ComputeLSLFromACNs(nodesList, toPlayer);

		#if DEBUG
		Mod.Logger.VerboseDebug("Sending {0} LSL(s) for {1}", lsl.LockStatesByBlockPos.Count, toPlayer.PlayerName);
		#endif
		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl, toPlayer);
		}
		}

		private void UpdateBroadcast(IServerPlayer byPlayer,  BlockPos blockPos,  AccessControlNode updatedLock)
		{
		Mod.Logger.Debug("Broadcast single ACN @{0} ", blockPos);
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

		if (ServerAPI.Server.CurrentRunPhase == EnumServerRunPhase.RunGame && ServerAPI.World.AllOnlinePlayers.Count( ) > 0) 
		{
			//Start / Re-start thread to computed ACL node list to clients
			#if DEBUG
			Mod.Logger.VerboseDebug("Portunus re-trigger [{0}]", portunus_thread.ThreadState);
			#endif

			if (portunus_thread.ThreadState.HasFlag(ThreadState.Unstarted)) {
			portunus_thread.Start( );
			}
			else if (portunus_thread.ThreadState.HasFlag(ThreadState.WaitSleepJoin)) {
			//(re)Wake the sleeper!
			portunus_thread.Interrupt( );
			}
		}

		}

		private void Portunus( )
		{
		wake:
		Mod.Logger.VerboseDebug("Portunus thread awoken");
		try {
		//####################### Fetch ACN's for *INTRODUCED* (to A.C. system) Chunks... ######################	
		uint newChunkCount = 0;
		foreach (var chunkEntry in ServerAPI.WorldManager.AllLoadedChunks) {

		Vec3i loadedPos = ServerMAIN.WorldMap.ChunkPosFromChunkIndex3D(chunkEntry.Key);

			if (Server_ACN.ContainsKey(loadedPos) == false) 
			{			
			LoadACN_fromChunk(loadedPos);
			newChunkCount++;
			}
		}
		if (newChunkCount > 0) Mod.Logger.Debug("Noticed {0} new chunks", newChunkCount);

		//####################### For all online players - ACN's for *new* chunks entered/in ###########################
		foreach (var player in ServerAPI.World.AllOnlinePlayers) {//TODO: Parallel.ForEach
		//var client = this.ServerMAIN.GetConnectedClient(player.PlayerUID);

		var center = ServerAPI.World.BlockAccessor.ToChunkPos(player.Entity.ServerPos.AsBlockPos.Copy( ));

		HashSet<Vec3i> alreadyUpdatedSet = null;
		if (previousChunkSet_byPlayerUID.TryGetValue(player.PlayerUID, out alreadyUpdatedSet)) 
		{
		//All of them for nearest 27 CHUNKs, contacting ['new' Chunk ]
		var fresh_chunks = Helpers.ComputeChunkBubble(center).Except(alreadyUpdatedSet).ToList( );

		var introducedNodes = new List<KeyValuePair<BlockPos, AccessControlNode>>( );

		foreach (var chunkPosUpdate in fresh_chunks) {
		var ACNs_byChunk = RetrieveACNs_ByChunk(chunkPosUpdate);

		if (ACNs_byChunk != null && ACNs_byChunk.Count > 0) {
		introducedNodes.AddRange(ACNs_byChunk);
		}
		}
			if (introducedNodes.Count > 0) 
			{
			#if DEBUG
			Mod.Logger.VerboseDebug("Player {0} will get {1} ACNs about chunk {2}", player.PlayerName, introducedNodes.Count, center);
			#endif

			SendClientACNMultiUpdates(player as IServerPlayer, introducedNodes);

			previousChunkSet_byPlayerUID[player.PlayerUID].AddRange(fresh_chunks);
			}
		}
		}

		//################### Key holding status changes ########################		
		if (playerLostKeyIDs_byPlayerUID.Count > 0 || playerGainKeyIDs_byPlayerUID.Count > 0) {
		Stack<string> playerKeyChanges = new Stack<string>( playerGainKeyIDs_byPlayerUID.Keys.Union(playerLostKeyIDs_byPlayerUID.Keys));

		while (playerKeyChanges.Count > 0) {
		string pid = playerKeyChanges.Pop( );

		IServerPlayer tgtPlayer = ServerAPI.World.PlayerByUid(pid) as IServerPlayer;

		Mod.Logger.VerboseDebug("Player {0} held key(s) changed ~ re-evaluating ACNs", tgtPlayer.PlayerName);
		//Extract ACN from Key's stored position data...

		var ACNs_comboKeys = new List<KeyValuePair<BlockPos, AccessControlNode>>( ); 

		if (playerLostKeyIDs_byPlayerUID.ContainsKey(pid)) ACNs_comboKeys = GetACNs_byKeyID(playerLostKeyIDs_byPlayerUID[pid]);		
						
		if (playerGainKeyIDs_byPlayerUID.ContainsKey(pid)) ACNs_comboKeys = ACNs_comboKeys.Concat(GetACNs_byKeyID(playerGainKeyIDs_byPlayerUID[pid])).ToList();

		if ( ACNs_comboKeys.Count > 0) {
		var keyCount = playerKeyIDs_byPlayerUID.ContainsKey(pid) ? playerKeyIDs_byPlayerUID [pid].Count: 0;
		Mod.Logger.VerboseDebug("Player {0} - {1} key(s) updates for {2} ACNs", tgtPlayer.PlayerName, keyCount ,ACNs_comboKeys.Count);
		SendClientACNMultiUpdates(tgtPlayer, ACNs_comboKeys);

		//Done set - cleanup;				
		if (playerGainKeyIDs_byPlayerUID.ContainsKey(pid)) playerGainKeyIDs_byPlayerUID[pid].Clear( );
		if (playerLostKeyIDs_byPlayerUID.ContainsKey(pid)) playerLostKeyIDs_byPlayerUID[pid].Clear( );
		}

		}

		}	

		//########################### Persist & SAVE-COMMIT Altered ACNs ! #########################
		var alteredCount = Server_ACN.Count(ac => ac.Value.Altered == true);
		if (alteredCount > 0) Mod.Logger.Debug("There are {0} altered chunk Nodes to persist", alteredCount);

		foreach (var alteredEntry in Server_ACN.Where(node => node.Value.Altered == true).ToList()) {
		byte[ ] data = SerializerUtil.Serialize<ChunkACNodes>(alteredEntry.Value);

		IServerChunk updatingChunk = ServerAPI.WorldManager.GetChunk(alteredEntry.Key.X, alteredEntry.Key.Y, alteredEntry.Key.Z);

		updatingChunk.SetServerModdata(_AccessControlNodesKey, data);

		alteredEntry.Value.Altered = false;
		Mod.Logger.VerboseDebug("Stored ACN(s) for pos {0}", alteredEntry.Key);
		}

		//Then sleep until interupted again, and repeat

		Mod.Logger.VerboseDebug("Thread '{0}' about to sleep indefinitely.", Thread.CurrentThread.Name);

		Thread.Sleep(Timeout.Infinite);

		} catch (ThreadInterruptedException) {

		Mod.Logger.VerboseDebug("Thread '{0}' interupted.", Thread.CurrentThread.Name);
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
			lcn.OwnerName = ServerAPI.World.PlayerByUid(entry.Value.OwnerPlayerUID).PlayerName;
			break;

		case LockKinds.Combination:
			lcn.LockState = locked ? LockStatus.ComboUnknown : LockStatus.ComboKnown;
			lcn.OwnerName = ServerAPI.World.PlayerByUid(entry.Value.OwnerPlayerUID).PlayerName;
			lcn.Tier = entry.Value.Tier;
			break;

		case LockKinds.Key:
			lcn.LockState = locked ? LockStatus.KeyNope : LockStatus.KeyHave;
			lcn.OwnerName = ServerAPI.World.PlayerByUid(entry.Value.OwnerPlayerUID).PlayerName;
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
			lcn.LockState = LockStatus.None;//Only used for lock 'removal'
			break;

		case LockKinds.Classic:
			lcn.LockState = locked ? LockStatus.Locked : LockStatus.Unlocked;
			lcn.OwnerName = ServerAPI.World.PlayerByUid(updatedLock.OwnerPlayerUID).PlayerName;
			break;

		case LockKinds.Combination:
			lcn.LockState = locked ? LockStatus.ComboUnknown : LockStatus.ComboKnown;
			lcn.OwnerName = ServerAPI.World.PlayerByUid(updatedLock.OwnerPlayerUID).PlayerName;
			lcn.Tier = updatedLock.Tier;
			break;

		case LockKinds.Key:
			lcn.LockState = locked ? LockStatus.KeyNope : LockStatus.KeyHave;
			lcn.OwnerName = ServerAPI.World.PlayerByUid(updatedLock.OwnerPlayerUID).PlayerName;
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
			if (keyId > 0) keyIds.Add(keyId.Value);
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
					
			if (
			alteredSlot.Itemstack.Class == EnumItemClass.Item &&
			alteredSlot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)
	   		) 
			{
			#if DEBUG
			Mod.Logger.VerboseDebug("AttainKeyBySlotChange: {0} {1} {2}", player.PlayerName, inventoryClass, slotNum);
			Mod.Logger.VerboseDebug("Itemstack(code): {0}", alteredSlot.Itemstack.Collectible.Code);
			#endif

		var ivbp = alteredSlot.Inventory as InventoryBasePlayer;

			keyID = GenericKey.KeyID(alteredSlot.Itemstack);
			lockLocation = GenericKey.LockLocation(alteredSlot.Itemstack);

			#if DEBUG
			Mod.Logger.VerboseDebug("Key #{0} gained  slot# {1} in Inv: {2} for {3}", keyID, slotNum, alteredSlot.Inventory.InventoryID, player.PlayerName);
			#endif

			if (keyID.HasValue && playerKeyIDs_byPlayerUID.ContainsKey(playerUID)) 
			{
				playerKeyIDs_byPlayerUID[playerUID].Add(keyID.Value);
				if (playerGainKeyIDs_byPlayerUID.ContainsKey(playerUID)) { playerGainKeyIDs_byPlayerUID[playerUID].Add(keyID.Value); }
					else 
					{ 
						playerGainKeyIDs_byPlayerUID.Add(playerUID, new HashSet<int>()); 
						playerGainKeyIDs_byPlayerUID[playerUID].Add(keyID.Value);
					}
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
					if (toRemoveACN.LockStyle != LockKinds.None) Mod.Logger.Notification("player {0} broke @({1}) with ACN Owned by [{2}]", byPlayer.PlayerName, adjPos, toRemoveACN.OwnerPlayerUID);
				}
			}	
		}

		internal void LoseKeyBySlotChange(string playerUID, string inventoryClass, int slotNum)
		{
		int? keyID;
		BlockPos lockLocation;
		var player = ServerAPI.World.PlayerByUid(playerUID);

		IInventory theInv = player.InventoryManager.GetOwnInventory(inventoryClass);

		ItemSlot alteredSlot = theInv[slotNum];
		
		if (alteredSlot.Empty) return;

		if (
			alteredSlot.Itemstack.Class == EnumItemClass.Item &&
			alteredSlot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)
	   		) 
			{
			#if DEBUG
			Mod.Logger.VerboseDebug("LoseKeyBySlotChange: {0} {1} {2}", player.PlayerName, inventoryClass, slotNum);
			Mod.Logger.VerboseDebug("Itemstack(code): {0}", alteredSlot.Itemstack.Collectible.Code);
			#endif

		var ivbp = alteredSlot.Inventory as InventoryBasePlayer;

			keyID = GenericKey.KeyID(alteredSlot.Itemstack);
			lockLocation = GenericKey.LockLocation(alteredSlot.Itemstack);

		#if DEBUG
		Mod.Logger.VerboseDebug("Key #{0} lost on slot# {1} in Inv: {2} for {3}", keyID, slotNum, alteredSlot.Inventory.InventoryID, player.PlayerName);
		#endif

		if (keyID.HasValue && playerKeyIDs_byPlayerUID.ContainsKey(playerUID)) 
				{
				playerKeyIDs_byPlayerUID[playerUID].Remove(keyID.Value);//Having duplicate keys - is a problem still.				
				if (playerLostKeyIDs_byPlayerUID.ContainsKey(playerUID)) { playerLostKeyIDs_byPlayerUID[playerUID].Add(keyID.Value); }
				else 
					{
					playerLostKeyIDs_byPlayerUID.Add(playerUID, new HashSet<int>( ));
					playerLostKeyIDs_byPlayerUID[playerUID].Add(keyID.Value);
					}
				}
			}
		}

		private List<KeyValuePair<BlockPos, AccessControlNode>> GetACNs_byKeyID(HashSet<int> setKeyIDs)
		{
		List<KeyValuePair<BlockPos, AccessControlNode>> acnList = new List<KeyValuePair<BlockPos, AccessControlNode>>( );
					
		foreach (int keyID in setKeyIDs) 
		{		
		 	if (ACNs_byKeyID.ContainsKey(keyID)) {
			acnList.Add(ACNs_byKeyID[keyID]);
			}//else: log error? missing ACN in Keys#
		}	           

		return acnList;
		}

		private List<AccessControlNode> ExtractACNsFromKeysFromPlayerInventory(string playerID)
		{
		var targetPlayer = ServerAPI.World.PlayerByUid(playerID);
		List<AccessControlNode> associatedACNs = new List<AccessControlNode>( );

		foreach (var inventory in targetPlayer.InventoryManager.Inventories) {
			foreach (ItemSlot slot in inventory.Value) {
			if (slot.Empty) continue;

			if (slot.Itemstack.Collectible.ItemClass == EnumItemClass.Item &&
				slot.Itemstack.Item.Code.BeginsWith(_domain, _keyCodeName)) 
					{
					var keyId = GenericKey.KeyID(slot.Itemstack);
					var ACN_location = GenericKey.LockLocation(slot.Itemstack);

					if (keyId == null || ACN_location == null) {
							Mod.Logger.Error("Corrupt '{3}' Item in {0}'s {2} Slot#{1}", targetPlayer.PlayerName,inventory.Value.GetSlotId(slot), inventory.Value.InventoryID,slot.Itemstack.Item.Code);
					continue;
					}

					Vec3i locksChunk = ServerAPI.World.BlockAccessor.ToChunkPos(ACN_location);

				if (Server_ACN.ContainsKey(locksChunk) && Server_ACN[locksChunk].Entries.ContainsKey(ACN_location)) {
				var controlNode = Server_ACN[locksChunk].Entries[ACN_location];//Instead by KEY#

				associatedACNs.Add(controlNode);
				}
				else {
					Mod.Logger.Warning("Key-lock without ACN match K#{0} @{1}", keyId, ACN_location);
				}
			}
			}

			}

		return associatedACNs;
		}

		#endregion
	}
}

