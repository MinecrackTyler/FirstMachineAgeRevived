using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace FirstMachineAge
{
	public abstract class GenericLock : Item 
	{
		private const string _lockStyleKey = @"lock-style";
		private const string _comboKey = @"combo";
		private const string _lockTier = @"lock-tier";
		private const uint MinimumComboDigits = 2;

		protected ICoreServerAPI ServerAPI { get; set; }
		protected ILogger Logger { get; set; }
		protected ICoreClientAPI ClientAPI { get; set; }
		protected AccessControlsMod AccessControlsMod { get; set; }

		public LockKinds LockStyle { get; protected set;}

		public uint LockTier { get; protected set; }

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

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

			if (this.Attributes.Exists && this.Attributes.KeyExists(_lockStyleKey)) {
				this.LockStyle = this.Attributes[_lockStyleKey].AsObject<LockKinds>(LockKinds.None);
			}

			if (LockStyle != LockKinds.None && this.Attributes.KeyExists(_lockTier)) 
			{
				this.LockTier = ( uint )this.Attributes[_lockTier].AsInt(0); 
			}
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


		/// <summary>
		/// Stores AccessControlNode in Tree-Attributes.
		/// </summary>
		/// <remarks>AccessControlNode ->  alterable Attributes (which are strangely part of 'ItemStack'...)</remarks>
		/// <param name="acn">Control node settings.</param>
		protected TreeAttribute TreeAttributesFromACN(AccessControlNode acn)
		{
			//Copy Combo number, keyID, type, ect...
			switch (acn.LockStyle) 
			{
			case LockKinds.Classic:
			//OwnerId?

			break;



			}

			//itemstack.Collectible.Attributes["clothescategory"].AsString(null);
			return null;
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

		public uint? KeyID(ItemSlot sourceSlot)
		{
			if (sourceSlot.Itemstack.Attributes.HasAttribute(AccessControlsMod._KeyIDKey)) {
				return ( uint? )sourceSlot.Itemstack.Attributes.GetInt(AccessControlsMod._KeyIDKey);
			} else {
				return new uint( );
			}
		}
	}
}

