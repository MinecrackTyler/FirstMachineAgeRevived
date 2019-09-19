using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;


using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace FirstMachineAge
{
	public class AccessControlsMod : ModSystem
	{
		private const string _AccessControlNodesKey = @"ACCESS_CONTROL_NODES";
		internal const string _KeyIDKey = @"key_id";//for JSON attribute, DB key sequence
		internal const string _persistedStateKey = @"ACL_PersistedState";
		private const string _channel_name = @"AccessControl";

		private ICoreServerAPI ServerAPI;
		private ServerMain ServerMAIN;
		private PlayerDataManager PlayerDatamanager;

		private ICoreAPI CoreAPI;
		private ICoreClientAPI ClientAPI;


		private ModSystemBlockReinforcement brs;

		private SortedDictionary<long, ChunkACNodes> Server_ACN;//Track changes - and commit every ## minutes, in addition to server shutdown data-storage, chunk unloads
		private Dictionary<BlockPos, LockCacheNode> Client_LockLookup;//By BlockPos - for fast local lookup. pre-computed by server...
		private ACLPersisted PersistedState;

		//Comm. Channels
		private IClientNetworkChannel accessControl_ClientChannel;
		private IServerNetworkChannel accessControl_ServerChannel;

		private Item KeyItem;
		private Item CombolockItem;
		private Item UndeployedKeylockItem;//Key & Lock together
		private Item DeployedKeylockItem;


		private SortedDictionary<string, HashSet<Vec3i>> previousChunkSet_byPlayerUID;

		private Thread portunus_thread;


		#region Mod System
		public override bool ShouldLoad(EnumAppSide forSide)
		{
			return true;
		}

		public override void Start(ICoreAPI api)
		{
			this.CoreAPI = api;
			base.Start(api);
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			this.ClientAPI = api;
			base.StartClientSide(api);

			InitializeClientSide( );
		}


		public override void StartServerSide(ICoreServerAPI api)
		{
			this.ServerAPI = api;
			this.PlayerDatamanager = api.Permissions as PlayerDataManager;

			if (ServerAPI.World is ServerMain) {
				this.ServerMAIN = ServerAPI.World as ServerMain;
			} else {
				Mod.Logger.Error("Cannot access 'ServerMain' class:  API (implimentation) has changed, Contact Developer!");
				return;
			}

			base.StartServerSide(api);

			InitializeServerSide( );
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


		#region Internals
		private void InitializeServerSide( )
		{
			//Replace blockBehaviorLockable - but only 'game' domain entires...
			var rawBytes = ServerAPI.WorldManager.SaveGame.GetData(_persistedStateKey);
			if (rawBytes != null && rawBytes.Length > 0) {
				this.PersistedState = SerializerUtil.Deserialize<ACLPersisted>(rawBytes);
			} else {
				ACLPersisted newPersistedState = new ACLPersisted( );

				var aclPersistBytes = SerializerUtil.Serialize<ACLPersisted>(newPersistedState);

				ServerAPI.WorldManager.SaveGame.StoreData(_persistedStateKey, aclPersistBytes);

				this.PersistedState = newPersistedState;
			}

			//Await lock-GUI events, send cache updates via NW channel...
			accessControl_ServerChannel = ServerAPI.Network.RegisterChannel(_channel_name);
			accessControl_ServerChannel.RegisterMessageType<LockGUIMessage>( );

			accessControl_ServerChannel.SetMessageHandler<LockGUIMessage>(LockGUIMessageHandler);

			//Re-wake Poller thread to send out _possible_ updates for cache
			ServerAPI.Event.RegisterGameTickListener(AwakenPortunus, 1000);

			ServerAPI.Event.PlayerJoin += TrackPlayerJoins;
			ServerAPI.Event.PlayerLeave += TrackPlayerLeaves;

			portunus_thread = new Thread(Portunus);
			portunus_thread.Name = "Portunus";
			portunus_thread.Priority = ThreadPriority.Lowest;
			portunus_thread.IsBackground = true;

		}

	

	private void InitializeClientSide( )
	{
		accessControl_ClientChannel = ClientAPI.Network.RegisterChannel(_channel_name);
		accessControl_ClientChannel.RegisterMessageType<LockStatusList>( );

		accessControl_ClientChannel.SetMessageHandler<LockStatusList>(RecieveACNUpdate);//RX: Cache update 


	}

	internal int NextKeyID {
		get { return ++PersistedState.KeyId_Sequence; }
	}


	internal void AlterLockAt(BlockSelection blockSel, IPlayer player, LockKinds lockType, byte[] combinationCode = null, uint? keyCode = null)
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
		} else {
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

	internal void Send_Lock_GUI_Message(string playerUID, BlockPos position, byte[] comboGuess)
	{
		//Client Side still - send guess attempt to server
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


			} else {
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
					} else {
						Client_LockLookup.Remove(update.Key.Copy( ));
					}

				} else {
					//New
					Client_LockLookup.Add(update.Key.Copy( ), update.Value);
				}

			}
		}
	}

	private void SendClientACNUpdate(IServerPlayer toPlayer, BlockPos pos, AccessControlNode subjectACN)
	{
		//Send packet to client on channel - ONE ACN update only [Single lock update]
		LockCacheNode state = new LockCacheNode( );



		LockStatusList lsl = new LockStatusList(pos, state);

		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl);
	}



	private void SendClientACNMultiUpdates(IServerPlayer toPlayer, IList<AccessControlNode> nodesList)
	{
		//TODO: side-Thread this!
		//Send packet to client on channel - All of them for nearest 27 CHUNKs, contacting ['new' Chunk loads]
		ConnectedClient client = this.ServerMAIN.GetClientByUID(toPlayer.PlayerUID);

		//client.DidSendChunk(index3d_chunk);



		//Pre-cooked 'cache' ACLs Computed for *EVERY* player that is aware of loading chunk
		/*
		LockStatusList lsl = new LockStatusList( nodesList);



		accessControl_ServerChannel.SendPacket<LockStatusList>(lsl);
		*/
	}

	private void TrackPlayerJoins(IServerPlayer byPlayer)
	{
		previousChunkSet_byPlayerUID.Add(byPlayer.PlayerUID, new HashSet<long>( ));
	}

	private void TrackPlayerLeaves(IServerPlayer byPlayer)
	{
		previousChunkSet_byPlayerUID.Remove(byPlayer.PlayerUID);
	}

	private void AwakenPortunus(float delayed)
	{
		//Start / Re-start thread to computed ACL node list to clients

		if (portunus_thread.ThreadState == ThreadState.Unstarted) {
			portunus_thread.Start( );
		} else {
				//(re)Wake the sleeper!
				portunus_thread.Interrupt( ); 
		}
	}

	private void Portunus( )
	{
	wake:

		try {
			//For all online players
				foreach(var player in ServerAPI.World.AllOnlinePlayers)
				{
					var client = this.ServerMAIN.GetConnectedClient(player.PlayerUID);

					var center = ServerAPI.World.BlockAccessor.ToChunkPos(player.Entity.ServerPos.AsBlockPos.Copy( ));

					var alreadyUpdatedSet = previousChunkSet_byPlayerUID[player.PlayerUID];

					var ACLs_for_chunk = Helpers.ComputeChunkBubble(center).Except(alreadyUpdatedSet).ToList();

					//client.DidSendChunk
					//What chunk ACLs are unsent to client?


					//Do work of calculating stuff... then send packets

					//var chunk_ACLs_Cached = this.previousChunkSet_byPlayerUID.

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


	#endregion

	#region Access Control Interface

	//Pull data out of BRS first - or intercept its channel packets?
	//Then superceed it 100% ?

	public LockStatus LockState(BlockPos pos, IPlayer forPlayer)
	{
		if (CoreAPI.Side.IsClient( )) {
			if (Client_LockLookup.ContainsKey(pos.Copy( ))) {
				return Client_LockLookup[pos.Copy( )].LockState;
			} else {
				//TODO: Force fetch from Server ?

			}
		} else {
			//Server instance

		}

		return LockStatus.None;
	}

	public uint LockTier(BlockPos pos, IPlayer forPlayer)
	{
		if (CoreAPI.Side.IsClient( )) {
			if (Client_LockLookup.ContainsKey(pos.Copy( ))) {
				return Client_LockLookup[pos.Copy( )].Tier;
			} else {
				//TODO: Force fetch from Server ?

			}
		} else {
			//Server instance

		}

		return 0;
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
		if (brs.IsLocked(position, forPlayer)) //Replace with local cache?
		{
			var controlNode = RetrieveACN(position.Copy( ));

			if (controlNode.LockStyle == LockKinds.Classic || controlNode.LockStyle == LockKinds.Combination) {
				//Is it yours?
				if (controlNode.OwnerPlayerUID == forPlayer.PlayerUID) return false;

				//In same faction? 
				if (controlNode.PermittedPlayers != null & controlNode.PermittedPlayers.Count > 0) {

					foreach (var perp in controlNode.PermittedPlayers) {
						if (perp.PlayerUID == forPlayer.PlayerUID) {
							return false;//By discrete entry - combo's add these
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
			} else if (controlNode.LockStyle == LockKinds.Key) //************** End of: Classic / Combination LOCKS ***********
		   {
				//Search inventory for matching KeyID item...each time!
				foreach (var inventory in forPlayer.InventoryManager.Inventories) {
					IInventory actual = inventory.Value;
					foreach (ItemSlot itmSlot in actual) {
						if (itmSlot.Empty == false && itmSlot.Itemstack.Class == EnumItemClass.Item) {
							if (itmSlot.Itemstack.Item.ItemId == KeyItem.ItemId) {
								//The right key?
								var tempKey = itmSlot.Itemstack.Item;
								if (tempKey.Attributes.KeyExists(_KeyIDKey)) {
									int tempKeyId = tempKey.Attributes[_KeyIDKey].AsInt(-1);
									if (tempKeyId == controlNode.KeyID.Value) {
										return false;//Key works in lock
									}

								}
							}

						}
					}
				}
				return true;

			}//************** End of: KEY LOCKS ***********


		}

		return false;//No lock here!
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


	public void ApplyLock(BlockSelection blockSel, IPlayer player, ItemSlot itemSlot)
	{
		bool success = false;

		GenericLock theLock = itemSlot.Itemstack.Item as GenericLock;

		//TODO: Handle position(s) with N block high doors?


		//Client path only updates local cache?
		if (CoreAPI.Side.IsClient( )) {
			AddLock_ClientCache(blockSel.Position, theLock);
			return;
		}

		//Server continues


		Mod.Logger.VerboseDebug("Applying lock; {0} #{1} @ {2} by {3}", theLock.LockStyle, theLock.LockTier, blockSel.Position, player.PlayerName);

		if (theLock.LockStyle == LockKinds.Combination) {
			var combo = theLock.CombinationCode(itemSlot);

			if (combo == null) {
				Mod.Logger.Warning("Undefined Combination # for existant lock - can't apply!");
			} else {



			}

		}

		if (theLock.LockStyle == LockKinds.Key) {
			//keyCode.HasValue

			//Need to do an item swap too
		}

		if (success) {
			//Send message to player that object was locked with X type lock (and comco / key#)	
			//Send out ACN update selective broadcast msg...

		}




	}



	/// <summary>
	/// Retrieves the ACN data
	/// </summary>
	/// <returns> Access Control node for a BlocKPos.</returns>
	/// <param name="byBlockPos">By block position.</param>
	public AccessControlNode RetrieveACN(BlockPos byBlockPos)
	{
		long chunkIndex = ServerAPI.World.BulkBlockAccessor.ToChunkIndex3D(byBlockPos);
		AccessControlNode node = new AccessControlNode( );

		if (this.Server_ACN.ContainsKey(chunkIndex)) {


			if (Server_ACN[chunkIndex].Entries.TryGetValue(byBlockPos, out node)) {
				return node;
			}


		} else {
			//Retrieve and add to local cache
			IServerChunk targetChunk;
			byte[] data;

			if (ServerAPI.WorldManager.AllLoadedChunks.TryGetValue(chunkIndex, out targetChunk)) {
				data = targetChunk.GetServerModdata(_AccessControlNodesKey);


			} else {
				//An unloaded chunk huh...
				targetChunk = ServerAPI.WorldManager.GetChunk(byBlockPos);
				data = targetChunk.GetServerModdata(_AccessControlNodesKey);

			}

			if (data != null && data.Length > 0) {

				ChunkACNodes acNodes = SerializerUtil.Deserialize<ChunkACNodes>(data);

				Server_ACN.Add(chunkIndex, acNodes);

				acNodes.Entries.TryGetValue(byBlockPos, out node);

			} else {
				//Setup new AC Node list for this chunk.
				ChunkACNodes newAcNodes = new ChunkACNodes( );

				Server_ACN.Add(chunkIndex, newAcNodes);


			}
		}


		return node;
	}

	//	byte[] GetServerModdata (string key);
	//void SetServerModdata(string key, byte[] data);

	//public byte[] GetData(string name)
	//public void StoreData(string name, byte[] value)

	//_oAzFHaLM7aeBn6i00bHS72XxcA9 ISaveGame





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