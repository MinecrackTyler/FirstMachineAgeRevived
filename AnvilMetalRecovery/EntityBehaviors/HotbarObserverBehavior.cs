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



		public override void Initialize(EntityProperties properties, Vintagestory.API.JsonObject attributes)
		{
		if (this.entity.Api.Side.IsServer( )) {
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Hotbar Observer Online for: {0}", Player.GetName());
		#endif

		//Attach event observer...	
		Player.RightHandItemSlot.Inventory.SlotModified += Mainhand_InventorySlotChanging;
		Player.RightHandItemSlot.MarkedDirty += Mainhand_ItemSlotCleared;

		}
		}

		private void Mainhand_InventorySlotChanging(int slotID)
		{
		var watchedSlot = Player.RightHandItemSlot;
		if (!watchedSlot.Empty) {
					
		if (watchedSlot.Itemstack.Class == EnumItemClass.Item && TrackedItemData == null) {
		if (!ItemFilterList.Contains(watchedSlot?.Itemstack.Item.Code)) return;

		var durability = Player.RightHandItemSlot?.Itemstack?.Hitpoints();
		//starts empty		
		TrackedItemData = new HotbarObserverData(slotID, watchedSlot.Itemstack.Item, Player.PlayerUID);
		#if DEBUG		
		if (durability.HasValue)
		ServerAPI.Logger.VerboseDebug("Slot Occupied by {1} in #{0}; Dur.{2}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ), durability.Value);
		else ServerAPI.Logger.VerboseDebug("Slot Occupied by {1} in #{0}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
		#endif
		}
		else if (watchedSlot.Itemstack.Class == EnumItemClass.Item && TrackedItemData != null) {
		if (!ItemFilterList.Contains(watchedSlot?.Itemstack.Item.Code)) return;

		var durability = Player.RightHandItemSlot?.Itemstack?.Hitpoints( );
		//Changed						
		TrackedItemData = new HotbarObserverData(slotID, watchedSlot.Itemstack.Item, Player.PlayerUID);
		#if DEBUG
		if (durability.HasValue) 
		ServerAPI.Logger.VerboseDebug("Slot Changes to {1} in #{0}; Dur.{2}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ),durability.Value);
		else ServerAPI.Logger.VerboseDebug("Slot Changes to {1} in #{0}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
		#endif
		}
		else if (watchedSlot.Itemstack.Class == EnumItemClass.Block && TrackedItemData != null) {
		TrackedItemData = null;
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Slot Clear (non-item) in #{0}", slotID);
		#endif
		}
		}
		else {
		if (TrackedItemData?.SlotID == slotID) {
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Same Slot Cleared (empty) in #{0} - possible stack-erase?", slotID);
		#endif
		}
		else { TrackedItemData = null; }
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Slot (empty) in #{0}", slotID);
		#endif
		}
		}

		private bool Mainhand_ItemSlotCleared( )
		{
		//send Message if slot had item of interest before, and durability (now?) close to zero
		if (TrackedItemData != null && Player.RightHandItemSlot.Empty ) {
			#if DEBUG
			ServerAPI.Logger.VerboseDebug("Slot MarkedDirty #{0} WAS {1}", TrackedItemData.SlotID, TrackedItemData.ItemCode.ToShortString( ));
			#endif

			ServerAPI.Event.PushEvent(HotbarChannelName, TrackedItemData);
			TrackedItemData = null;			
		}
		return false;//When should this be true? 
		}
	}
}

