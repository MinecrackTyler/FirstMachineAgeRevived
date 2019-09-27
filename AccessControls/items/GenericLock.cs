using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public abstract class GenericLock : ItemPadlock 
	{
		private const string _lockStyleKey = @"lock-style";
		private const string _comboKey = @"combo";
		private const string _lockTier = @"lock-tier";
		private const uint MinimumComboDigits = 2;

		protected ICoreServerAPI ServerAPI { get; set; }
		protected ILogger Logger { get; set; }
		protected ICoreClientAPI ClientAPI { get; set; }
		protected AccessControlsMod AccessControlsMod { get; set; }

		public LockKinds LockStyle { 
			get
			{
			return Attributes[_lockStyleKey].AsObject<LockKinds>(LockKinds.None);
			}
		}

		public uint LockTier {
			get
			{
			return ( uint )Attributes[_lockTier].AsInt(0);
			}  
		}

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);//Just for client interactions

			if (api.Side.IsServer( )) {
				this.ServerAPI = ( ICoreServerAPI )api;
				this.Logger = this.ServerAPI.World.Logger;
				AccessControlsMod = ServerAPI.World.Api.ModLoader.GetModSystem<AccessControlsMod>( );
			}

			if (api.Side.IsClient( )) {
				this.ClientAPI = ( ICoreClientAPI )api;
				this.Logger = this.ClientAPI.World.Logger;
				AccessControlsMod = ClientAPI.World.Api.ModLoader.GetModSystem<AccessControlsMod>( );
			}

			Logger.VerboseDebug("{0} ~ OnLoaded", base.Code.ToString());
		}


		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
		bool lockable = false;

		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorLockable>( )) 
		{		
		IPlayer player = (byEntity as EntityPlayer).Player;

			if (this.api.Side.IsClient( )) 
			{
			lockable = !AccessControlsMod.CheckClientsideIsLocked(blockSel.Position, player);
			}

			if (this.api.Side.IsServer( )) 
			{			
			lockable = !AccessControlsMod.LockedForPlayer(blockSel.Position, player);
			}


		if (lockable == false) 
		{
		(byEntity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "cannotlock", Lang.Get("ingameerror-cannotlock"));
		}
		else {		
		(byEntity.World.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("lockapplied"));
				
		AccessControlsMod.ApplyLock(blockSel, player, slot);
		slot.TakeOut(1);
		slot.MarkDirty( );
		//TODO: GUI for Description after lock applied, it - ACN edits the desc...
		}

		handling = EnumHandHandling.PreventDefault;
		return;
		}

		handling = EnumHandHandling.NotHandled;
		}

		//or? OnCreatedByCrafting -- generate keyID and/or combo?
		public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
		{
			//Set keyid,combo if unset...
			if (this.LockStyle == LockKinds.Combination) {

				var comboCode = CombinationCode(slot);

				if (comboCode == null) 
				{
					GenerateCombination(slot, this);
				}
			} else if (this.LockStyle == LockKinds.Key) {

				var keyId = KeyID(slot);
				if (keyId.HasValue == false) {
					GenerateKeyId(slot, this);
				}
			}

		}

		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

			if (LockStyle == LockKinds.Combination) {
				dsc.AppendFormat("\nCombination#:");

				var comboCode = CombinationCode(inSlot);
				if (comboCode != null) {
					foreach (var digit in comboCode) {
						dsc.AppendFormat(" {0:D}\t", digit);
					}
				} else {
					dsc.AppendFormat("\nCombination ????");
				}
			}

			if (LockStyle == LockKinds.Key) {

				var keyId = KeyID(inSlot);
				if (keyId.HasValue) dsc.AppendFormat("\nKeyID#: {0}", keyId);
			}

			if (LockTier > 0) {
				dsc.AppendFormat("\nTier#: {0}", this.LockTier);
			}
		}





		protected void GenerateCombination(ItemSlot slot, GenericLock genericLock)
		{
			Random randNum = new Random( );
			byte[] comboArray = new byte[this.LockTier + MinimumComboDigits];

			for (int index = 0; index < this.LockTier + MinimumComboDigits; index++) 
			{				
				comboArray[index] = ( byte )randNum.Next(0, 9); //Extra high tiers - non-base10 ?
			}

			slot.Itemstack.Attributes.SetBytes(_comboKey, comboArray);
		}

		protected void GenerateKeyId(ItemSlot slot, GenericLock genericLock)
		{			
			slot.Itemstack.Attributes.SetInt(AccessControlsMod._KeyIDKey, this.AccessControlsMod.NextKeyID);
		}


		public byte[] CombinationCode(ItemSlot sourceSlot)
		{
			if (sourceSlot.Itemstack.Attributes.HasAttribute(_comboKey)) {
				return sourceSlot.Itemstack.Attributes.GetBytes(_comboKey);
			} else {
				return null;
			}
		}

		public int? KeyID(ItemSlot sourceSlot)
		{
			if (sourceSlot.Itemstack.Attributes.HasAttribute(AccessControlsMod._KeyIDKey)) {
				return sourceSlot.Itemstack.Attributes.GetInt(AccessControlsMod._KeyIDKey);
			} else {
				return new int( );
			}
		}
	}
}

