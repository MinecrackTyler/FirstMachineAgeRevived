using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{
	/// <summary>
	/// Push events to Messagebus on certain INVENTORY hotbar actions
	/// </summary>
	public class HotbarObserverBehavior : EntityBehavior
	{
		public const string HotbarChannelName = @"HotbarEvents";
		protected static List<AssetLocation> ItemFilterList;

		protected HotbarObserverData TrackedItemData;


		public override string PropertyName( )
		{
		return @"HotbarObserver";
		}

		public EntityPlayer Player {
			get { return this.entity as EntityPlayer; }
		}

		public ServerCoreAPI ServerAPI {
			get { return this.entity.Api as ServerCoreAPI; }
		}

		public HotbarObserverBehavior(Entity entity) : base(entity)
		{
		if (ItemFilterList == null) {
		MetalRecoverySystem metalRecoveryMod = entity.Api.ModLoader.GetModSystem<MetalRecoverySystem>( );
		ItemFilterList = metalRecoveryMod.ItemFilterList;
		}
		}



		public override void OnEntitySpawn( )
		{
		AttachEvents( );
		}

		public override void OnEntityLoaded( )
		{
		AttachEvents( );
		}


		private void AttachEvents( )
		{
			if (this.entity.Api.Side.IsServer( )) 
			{
			#if DEBUG
			ServerAPI.Logger.VerboseDebug("Hotbar Observer Online for: {0}", Player.GetName( ));
			#endif

			//Attach event observer...	
			Player.RightHandItemSlot.Inventory.SlotModified += Mainhand_InventorySlotChanging;
			Player.RightHandItemSlot.MarkedDirty += Mainhand_MarkedDirty;
			}
		}


		private void Mainhand_InventorySlotChanging(int slotID)
		{

		if (slotID != Player.Player.InventoryManager.ActiveHotbarSlotNumber) 
		{
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Ingoring (Slot switching); Event-Slot #{1} -> HB #{0} ", Player.Player.InventoryManager.ActiveHotbarSlotNumber, slotID);
		#endif
		return;
		}

		var watchedSlot = Player.RightHandItemSlot; //InventoryManager.ActiveHotbarSlot;
		if (!watchedSlot.Empty) {

		if (watchedSlot.Itemstack.Class == EnumItemClass.Item) 
		{
			if (ItemFilterList.Contains(watchedSlot?.Itemstack.Item.Code)) 
			{									
			//starts empty	|| Slot changes
				if ((TrackedItemData == null || TrackedItemData.SlotID != Player.Player.InventoryManager.ActiveHotbarSlotNumber) ) 
				{
				var hitpoints = Player.RightHandItemSlot?.Itemstack?.Hitpoints( );
				if (hitpoints.HasValue && hitpoints.Value >= 1)
					{
					TrackedItemData = new HotbarObserverData(Player.Player.InventoryManager.ActiveHotbarSlotNumber, watchedSlot.Itemstack.Item, Player.PlayerUID);
					#if DEBUG
					ServerAPI.Logger.VerboseDebug("Tracking {0} in #{1}; H.P.[{2}]", TrackedItemData.ItemCode.ToShortString( ), slotID, hitpoints);
					#endif
					}
				}
			}
			else 
			{
			TrackedItemData = null;//Untrack other item	
			#if DEBUG
			ServerAPI.Logger.VerboseDebug("Ignoring (filtered item) in #{0}", slotID);
			#endif
			}
		}
		else 
		{
		TrackedItemData = null;//Ignore Blocks
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Ignoring (block) in #{0}", slotID);
		#endif
		}

		}
		}

		private bool Mainhand_MarkedDirty( )
		{
		//mabey send Message if slot had item of interest before?
		if (TrackedItemData != null ) {
		int? hitpoints = Player.RightHandItemSlot?.Itemstack?.Hitpoints( );
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("DirtyEvent: Tracked Slot#{0} is {1}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
		if (!Player.RightHandItemSlot.Empty && Player.RightHandItemSlot.Itemstack.Class == EnumItemClass.Item) {
		ServerAPI.Logger.VerboseDebug("^ Active Item: {0}, Slot#{2}, H.P.[{1}]", Player.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code, hitpoints ?? -1, Player.Player.InventoryManager.ActiveHotbarSlotNumber );
		}
		#endif

			if (Player.Player.InventoryManager.ActiveHotbarSlotNumber == TrackedItemData.SlotID) 
			{
				if (Player.RightHandItemSlot.Empty) 
				{
				//Apparently; 'RightHandItemSlot' isn't accurate
				var hotbarInv = Player.Player.InventoryManager.GetHotbarInventory( );
				var hotSlot = hotbarInv[TrackedItemData.SlotID];
				if (!hotSlot.Empty) return false;

				#if DEBUG
				ServerAPI.Logger.VerboseDebug("Tracked Slot Cleared! #{0} WAS {1}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
				#endif	
				ServerAPI.Event.PushEvent(HotbarChannelName, TrackedItemData);
				TrackedItemData = null;
				} 
				else if ( ItemFilterList.Contains(Player.RightHandItemSlot?.Itemstack?.Item?.Code) && hitpoints <= 0)
				{
				#if DEBUG
				ServerAPI.Logger.VerboseDebug("Tracked Slot HP=0!, #{0} WAS {1}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
				#endif
				ServerAPI.Event.PushEvent(HotbarChannelName, TrackedItemData);
				TrackedItemData = null;		
				}		
			}					
		}
		return false;//When should this be true? 
		}
	}
}

