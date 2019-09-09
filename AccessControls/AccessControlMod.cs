using System;
using System.Collections;
using System.Collections.Generic;


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
	public class AccessControlsMod: ModSystem
	{
		private const string _AccessControlNodesKey = @"ACCESS_CONTROL_NODES";
		public const string _KeyIDKey = @"key_id";//for JSON attribute, DB key sequence
		public const string _persistedStateKey = @"ACL_PersistedState";

		private ICoreServerAPI ServerAPI;
		private ICoreAPI CoreAPI;
		private ICoreClientAPI ClientAPI;
		private PlayerDataManager PlayerDatamanager;

		private ModSystemBlockReinforcement brs;

		private SortedDictionary<long, ChunkACNodes> Server_ACN;//Track changes - and commit every ## minutes, in addition to server shutdown data-storage, chunk unloads
		private Dictionary<BlockPos, LockStatus> Client_LockLookup;//By BlockPos - for fast cached lookup. only hold 100's of entries
		private ACLPersisted PersistedState;

		private Item KeyItem;
		private Item CombolockItem;
		private Item UndeployedKeylockItem;//Key & Lock together
		private Item DeployedKeylockItem;

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
		}


		public override void StartServerSide(ICoreServerAPI api)
		{
			this.ServerAPI = api;
			this.PlayerDatamanager = api.Permissions as PlayerDataManager;
			base.StartServerSide(api);

			Initialize();
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

		//Pull data out of BRS first - or intercept its channel packets?
		//Then superceed it 100% ?

		public LockStatus LockState(BlockPos pos, IPlayer forPlayer)
		{
			if (CoreAPI.Side == EnumAppSide.Client) 
			{
				if (Client_LockLookup.ContainsKey(pos.Copy( ))) 
				{
					return Client_LockLookup[pos.Copy( )];
				}
				else
				{
					UpdateLocalLockCache(pos );//Needs to be invoked each time keys change in inventory?!
				}
			} 
			else 
			{
				//Server instance

			}

			return LockStatus.None;
		}


		/// <summary>
		/// Backwardsy compatible method - for lockable behaviors
		/// </summary>
		/// <returns>when locked.</returns>
		/// <param name="position">Position.</param>
		/// <param name="player">Player.</param>
		/// <param name="code">Code.</param>
		public bool LockedForPlayer(BlockPos position, IPlayer forPlayer, AssetLocation code)
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
								return false;//By discreet entry - combo's add these
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
				}  else if (controlNode.LockStyle == LockKinds.Key) //************** End of: Classic / Combination LOCKS ***********
				{
					//Search inventory for matching KeyID item...each time!
					foreach (var inventory in forPlayer.InventoryManager.Inventories) 
					{
						IInventory actual = inventory.Value;
						foreach (ItemSlot itmSlot in actual) 
						{
							if (itmSlot.Empty == false && itmSlot.Itemstack.Class == EnumItemClass.Item) 
							{
								if (itmSlot.Itemstack.Item.ItemId == KeyItem.ItemId) 
								{
									//The right key?
									var tempKey = itmSlot.Itemstack.Item;
									if (tempKey.Attributes.KeyExists(_KeyIDKey)) 
									{
									int tempKeyId = tempKey.Attributes[_KeyIDKey].AsInt(-1);
									if (tempKeyId == controlNode.KeyID.Value) 
										{
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

		public bool LockedForPlayer(BlockPos position, IPlayer forPlayer)
		{
			var aclNode = RetrieveACN(position);

			if (aclNode != null) 			
			{
				if (aclNode.LockStyle == LockKinds.Key) 
				{
					//Check player Inventory for Keys... do they have a key for THIS lock?
					var matchingKeyID = aclNode.KeyID.HasValue ? aclNode.KeyID.Value : 0;


					foreach (IInventory item in forPlayer.InventoryManager.Inventories.Values) 
					{

					}
				}

				if (aclNode.LockStyle == LockKinds.Combination) 
				{
					//Check lock if AccessControlNode.PermittedPlayers.PlayerUID is present?

				}

				if (aclNode.LockStyle == LockKinds.Classic) 
				{
					return !(aclNode.OwnerPlayerUID == forPlayer.PlayerUID);
				}
			}

			return false;
		}


		public void ApplyLock(BlockSelection blockSel, IPlayer player, ItemSlot lockSource)
		{
			//Log it alot!

			GenericLock theLock = lockSource.Itemstack.Item as GenericLock;


			if (theLock.LockStyle == LockKinds.Combination) {

			}

			if (theLock.LockStyle == LockKinds.Key) {
				//keyCode.HasValue

			}


		}

		public void AlterLockAt(BlockSelection blockSel, IPlayer player, LockKinds lockType, byte[] combinationCode = null, uint? keyCode = null)
		{ 
			
		}

		protected void UpdateLocalLockCache(BlockPos pos)
		{
			throw new NotImplementedException( );
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
				

				if (Server_ACN[chunkIndex].Entries.TryGetValue(byBlockPos, out node)) 
				{
					return node;
				}


			} else 
			{
				//Retrieve and add to local cache
				IServerChunk targetChunk;
				byte[] data;

				if (ServerAPI.WorldManager.AllLoadedChunks.TryGetValue(chunkIndex, out targetChunk)) {
					data = targetChunk.GetServerModdata(_AccessControlNodesKey);


				} else 
				{
					//An unloaded chunk huh...
					targetChunk = ServerAPI.WorldManager.GetChunk(byBlockPos);
					data = targetChunk.GetServerModdata(_AccessControlNodesKey);

				}

				if (data != null && data.Length > 0) {

					ChunkACNodes acNodes = SerializerUtil.Deserialize<ChunkACNodes>(data);

					Server_ACN.Add(chunkIndex, acNodes);

					acNodes.Entries.TryGetValue(byBlockPos, out node);

				} else 
				{
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

		internal int NextKeyID
		{ 
			get { return ++PersistedState.KeyId_Sequence;}
		}

		public bool AttemptAccess( IPlayer byPlayer, BlockPos atPosition, byte[] guess = null )
		{
			var acn = RetrieveACN(atPosition);

			if (acn.LockStyle == LockKinds.Combination) 
			{

			} else 
			{
				Mod.Logger.Warning("Attempt to access with mis-matching lock types! BY: {0}", byPlayer.PlayerName);
			}

			return false;//Not it.
		}


		#endregion

		private void Initialize( )
		{
			var rawBytes = ServerAPI.WorldManager.SaveGame.GetData(_persistedStateKey);
			if (rawBytes != null && rawBytes.Length > 0)
			{
				this.PersistedState = SerializerUtil.Deserialize<ACLPersisted>(rawBytes);
			}
			else
			{
				ACLPersisted newPersistedState = new ACLPersisted( );

				var aclPersistBytes = SerializerUtil.Serialize<ACLPersisted>(newPersistedState);

				ServerAPI.WorldManager.SaveGame.StoreData(_persistedStateKey, aclPersistBytes);

				this.PersistedState = newPersistedState;
			}
		}


}
}

