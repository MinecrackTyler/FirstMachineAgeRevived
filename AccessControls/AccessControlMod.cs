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

namespace FirstMachineAge
{
	/// <summary>
	/// Access controls mod. 
	/// </summary>
	public partial class AccessControlsMod : ModSystem
	{

		public static string _LockLocationKey = @"lock_pos";
		public static string _variantDoorPartKey = @"part";
		public const string _keyCodeName = @"key";//The first part of code-name
		public const string _lockMaterial = @"material";//second part of code-name (lock or key)
														//Example:    fma:key-iron

		public const string _itemDescription = @"description";



		#region Mod System
		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;		

		RegisterStuff(api);
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
		this.ClientAPI = api;		

		InitializeClientSide( );
		}


		public override void StartServerSide(ICoreServerAPI api)
		{
		this.ServerAPI = api;
		this.PlayerDatamanager = api.Permissions as PlayerDataManager;

		if (ServerAPI.World is ServerMain) {
		this.ServerMAIN = ServerAPI.World as ServerMain;
		}
		else {
		Mod.Logger.Error("Cannot access 'ServerMain' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.LoadGame, InitializeServerSide);
		
		
		}

		public override double ExecuteOrder( )
		{
		return 0.19;
		}
		#endregion

		/*
		 // Client side data
        Dictionary<long, Dictionary<int, BlockReinforcement>> reinforcementsByChunk = new Dictionary<long, Dictionary<int, BlockReinforcement>>();

		Dictionary<ChunkPos, Dictionary<BlockPos, BlockReinforcement>>

		  clientChannel = api.Network
	        .RegisterChannel("blockreinforcement")
	        .RegisterMessageType(typeof(ChunkReinforcementData))
	        .SetMessageHandler<ChunkReinforcementData>(onData)

		   	data = chunk.GetModdata("reinforcements");

		 	Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = null;

			reinforcmentsOfChunk = SerializerUtil.Deserialize<Dictionary<int, BlockReinforcement>>(data);   
		*/




		#region Access Control Interface

		public void AdjustBlockPostionForMultiBlockStructure(ref BlockPos blockPos)
		{
		//SO far - this means ONLY for class: BlockDoor .... the 'upper' part.

		var thatBlock = CoreAPI.World.BlockAccessor.GetBlock(blockPos);

		/*
		class: "BlockDoor", >> BlockBaseDoor

		{ code: "part", states: ["down", "up"] },
		{ code: "state", states: ["closed", "opened"] },
		*/

		if (thatBlock != null && thatBlock.Id > 0 && thatBlock is BlockBaseDoor) {
		BlockBaseDoor doorBase = thatBlock as BlockBaseDoor;

		if (doorBase.Variant.ContainsKey(_variantDoorPartKey)) {
		string doorPart = doorBase.Variant[_variantDoorPartKey];
		if (doorPart.Equals("down", StringComparison.OrdinalIgnoreCase)) 
			{
			Mod.Logger.VerboseDebug("Adjusting blockPos ({0}) becomes upper door half...", blockPos);
			blockPos = blockPos.UpCopy( );
			}
		}
		}
		}

		//BRS - should be transfer'd on-fly to ACL format...from pre-loader



		public LockStatus LockState(BlockPos pos, IPlayer forPlayer)
		{
		if (CoreAPI.Side.IsClient( )) {
			if (Client_LockLookup.ContainsKey(pos.Copy( ))) 
			{
			return Client_LockLookup[pos.Copy( )].LockState;
			}
			else 
			{			
			return LockStatus.None;			
			}
		}
		else {
		//Server instance
		Vec3i locksChunk = ServerAPI.World.BlockAccessor.ToChunkPos(pos);

		if (Server_ACN.ContainsKey(locksChunk) && Server_ACN[locksChunk].Entries.ContainsKey(pos)) {
		var controlNode = Server_ACN[locksChunk].Entries[pos];
		var locked = EvaulateACN_Rule(forPlayer, controlNode);

		switch (controlNode.LockStyle) {

		case LockKinds.Classic:
			return locked ? LockStatus.Locked : LockStatus.Unlocked;

		case LockKinds.Combination:
			return locked ? LockStatus.ComboUnknown : LockStatus.ComboKnown;

		case LockKinds.Key:
			return locked ? LockStatus.KeyNope : LockStatus.KeyHave;

		}
		}
		}

		return LockStatus.None;
		}

		public uint LockTier(BlockPos pos, IPlayer forPlayer)
		{
		if (CoreAPI.Side.IsClient( )) {
		if (Client_LockLookup.ContainsKey(pos.Copy( ))) {
		return Client_LockLookup[pos.Copy( )].Tier;
		}
		else {
		//TODO: Force fetch from Server ?

		}
		}
		else {
		//Server instance
		Vec3i locksChunk = ServerAPI.World.BlockAccessor.ToChunkPos(pos);

		if (Server_ACN.ContainsKey(locksChunk) && Server_ACN[locksChunk].Entries.ContainsKey(pos)) {
		var controlNode = Server_ACN[locksChunk].Entries[pos];
		return controlNode.Tier;
		}
		}

		return 0;
		}

		public string LockOwnerName(BlockPos pos, IPlayer forPlayer)
		{
		if (CoreAPI.Side.IsClient( )) {
		if (Client_LockLookup.ContainsKey(pos.Copy( ))) {
		return Client_LockLookup[pos.Copy( )].OwnerName;
		}
		else {
		//TODO: Force fetch from Server ?

		}
		}
		else {
		//Server instance
		Vec3i locksChunk = ServerAPI.World.BlockAccessor.ToChunkPos(pos);

		if (Server_ACN.ContainsKey(locksChunk) && Server_ACN[locksChunk].Entries.ContainsKey(pos)) {
		var controlNode = Server_ACN[locksChunk].Entries[pos];

		return ServerAPI.World.PlayerByUid(controlNode.OwnerPlayerUID).PlayerName;
		}
		}

		return String.Empty;
		}


		/// <summary>
		/// Backwardsy compatible method - for lockable behaviors
		/// </summary>
		/// <returns>when locked.</returns>
		/// <param name="position">Position.</param>
		/// <param name="player">Player.</param>
		/// <param name="code">AssetCode.</param>
		public bool LockedForPlayer(BlockPos position, IPlayer forPlayer, AssetLocation code = null)
		{

		if (CoreAPI.Side.IsClient( )) {
		return this.CheckClientsideIsLocked(position, forPlayer);
		}

		var controlNode = RetrieveACN(position.Copy( ));

		if (controlNode.LockStyle != LockKinds.None) {
		return EvaulateACN_Rule(forPlayer, controlNode);
		}

		return false;
		}

		public AccessControlNode GrantIndividualPlayerAccess(IServerPlayer fromPlayer, BlockPos position, string reason)
		{
		throw new NotImplementedException( );
		}

		public AccessControlNode RevokeIndividualPlayerAccess(IServerPlayer fromPlayer, BlockPos position, string reason)
		{
		throw new NotImplementedException( );
		}

		public AccessControlNode GrantGroupAccess(string groupUID, BlockPos position, string reason)
		{
		throw new NotImplementedException( );
		}

		public AccessControlNode RevokeGroupAccess(string groupUID, BlockPos position, string reason)
		{
		throw new NotImplementedException( );
		}



		/// <summary>
		/// Checks the clientside is locked.
		/// </summary>
		/// <returns>If locked is TRUE.</returns>
		/// <param name="position">Position.</param>
		/// <param name="forPlayer">For player.</param>
		public bool CheckClientsideIsLocked(BlockPos position, IPlayer forPlayer)
		{
			
		if (Client_LockLookup.ContainsKey(position)) 
		{
			if (Client_LockLookup[position].LockState == LockStatus.ComboUnknown ||
				Client_LockLookup[position].LockState == LockStatus.KeyNope ||
				Client_LockLookup[position].LockState == LockStatus.Locked ||
				Client_LockLookup[position].LockState == LockStatus.Unknown) 
			{
				return true;
			}		
		}

		return false;//Probably not...
		}
		 
		


		public void ApplyLock(BlockSelection blockSel, IPlayer player, ItemSlot itemSlot, string desc = null)
		{
			bool success = true;

			GenericLock theLock = itemSlot.Itemstack.Item as GenericLock;
			string material = theLock.Variant[_lockMaterial];

			BlockPos blockPos = blockSel.Position.Copy( );


			//TODO: Adjust position(s) with N block high doors, but player selected 'lower' part...
			AdjustBlockPostionForMultiBlockStructure(ref blockPos);

			//Client path only updates local cache?
			if (CoreAPI.Side.IsClient( )) {
				AddLock_ClientCache(blockPos, theLock);
				return;
			}

			//Server continues
			IServerPlayer serverPlayer = player as IServerPlayer;
			Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(blockPos);

			Mod.Logger.VerboseDebug("Applying lock; {0} T{1} @ {2} by {3}", theLock.LockStyle, theLock.LockTier, blockSel.Position, player.PlayerName);

			AccessControlNode newLockACN = new AccessControlNode(player.PlayerUID, theLock.LockStyle );

			if (theLock.LockStyle == LockKinds.Combination) 
			{				
				newLockACN.LockStyle = LockKinds.Combination;
				newLockACN.CombinationCode = theLock.CombinationCode(itemSlot);
				newLockACN.Tier = theLock.LockTier;

				if (newLockACN.CombinationCode == null) {
				Mod.Logger.Warning("Undefined Combination # for existant lock - can't apply!");
				}
			}

			if (theLock.LockStyle == LockKinds.Key) {
				newLockACN.KeyID = theLock.KeyID(itemSlot).GetValueOrDefault(PersistedState.KeyId_Sequence);

				//Perform inventory item swap to true Key from lock (create key first...)
				GenericKey matchingKey = ServerAPI.World.GetItem(new AssetLocation(_domain,_keyCodeName+"-"+material)) as GenericKey;

				ItemStack itemStackForKey =  new ItemStack(matchingKey, 1);
				GenericKey.WriteACL_ItemStack(ref itemStackForKey, newLockACN, blockPos);

				serverPlayer.InventoryManager.TryGiveItemstack(itemStackForKey, true);
				//Mark slot dirty?
			}

		if (Server_ACN.ContainsKey(chunkPos)) {
		Server_ACN[chunkPos].Entries.Add(blockPos, newLockACN);		
		}
		else 
		{
		Mod.Logger.Debug("Created ChunkACNodes for {0}", chunkPos);
		Server_ACN.Add(chunkPos, new ChunkACNodes(chunkPos));
		Server_ACN[chunkPos].Entries.Add(blockPos, newLockACN);
		}

		if (success) {
			//Mark this host chunk had an ACL added, thus altered.
			Server_ACN[chunkPos].Altered = true;

			//Send message to player that object was locked with X type lock (and combo / key#)	
			//Send out ACN update selective broadcast msg...
			UpdateBroadcast(serverPlayer, blockPos, newLockACN );
		}




		}

		public void RemoveLock(BlockSelection blockSel, IPlayer player)
		{

		BlockPos blockPos = blockSel.Position.Copy( );		

		//Client path only updates local cache?
		if (CoreAPI.Side.IsClient( )) {
		RemoveLock_ClientCache(blockPos);
		return;
		}

		//Server continues
		IServerPlayer serverPlayer = player as IServerPlayer;
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(blockPos);

		Mod.Logger.VerboseDebug("Remove ACL entry @{0} by {1}", blockSel.Position, player.PlayerName);

		AccessControlNode remLockACN = Server_ACN[chunkPos].Entries[blockPos];
		remLockACN.LockStyle = LockKinds.None;//Remove from other players ACN caches'

		Server_ACN[chunkPos].Entries.Remove(blockPos);		
				

		//Send message to player that object was locked with X type lock (and combo / key#)	
		//Send out ACN update selective broadcast msg...
		UpdateBroadcast(serverPlayer, blockPos, remLockACN);
		


		}



		/// <summary>
		/// Retrieves the ACN data
		/// </summary>
		/// <returns> Access Control node for a BlocKPos.</returns>
		/// <param name="byBlockPos">By block position.</param>
		public AccessControlNode RetrieveACN(BlockPos byBlockPos)
		{
			var chunkPos= ServerAPI.World.BulkBlockAccessor.ToChunkPos(byBlockPos);
			AccessControlNode node = new AccessControlNode( );

			if (this.Server_ACN.ContainsKey(chunkPos)) {


				if (Server_ACN[chunkPos].Entries.TryGetValue(byBlockPos, out node)) {
					return node;
				}


			} else {
				
			}


			return node;
		}

		//	byte[] GetServerModdata (string key);
		//void SetServerModdata(string key, byte[] data);

		//public byte[] GetData(string name)
		//public void StoreData(string name, byte[] value)

		//_oAzFHaLM7aeBn6i00bHS72XxcA9 ISaveGame

	public IDictionary<BlockPos, AccessControlNode> RetrieveACNs_ByChunk(Vec3i byChunkPos)
	{	
		if (Server_ACN.ContainsKey(byChunkPos))
		{
			return Server_ACN[byChunkPos].Entries;
		}
		else 
		{


		}

		return null;
	}

	

	protected bool AttemptAccess(IPlayer byPlayer, BlockPos atPosition, byte[] guess = null)
	{
		var acn = RetrieveACN(atPosition);

		if (acn.LockStyle == LockKinds.Combination) {

		} else {
			Mod.Logger.Warning("Attempt to access with mis-matching lock types! BY: {0}", byPlayer.PlayerName);
		}

		return false;//Not it.
	}


	#endregion



	}

}