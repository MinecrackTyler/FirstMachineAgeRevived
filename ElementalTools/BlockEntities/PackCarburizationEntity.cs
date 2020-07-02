using System;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ElementalTools
{
	public class PackCarburizationEntity : BlockEntityContainer
	{
		//Item Container for Iron object to convert...
		internal InventoryGeneric internalInventory;


		public override InventoryBase Inventory {
			get
			{
			return internalInventory;
			}
		}

		public override string InventoryClassName {
			get
			{
			return @"Packcarburization_Inventory";
			}
		}

		public new PackCarburization Block {
			get { return base.Block as PackCarburization; }
		}


		public PackCarburizationEntity( )
		{
		internalInventory = new InventoryGeneric(1, null, null);
		}

		public override void Initialize(ICoreAPI api)
		{
		base.Initialize(api);
		}

		protected override void OnTick(float dt)
		{
		//Soak up Carbon; Transform into S-T-E-E-L .... eventually.
		if (!internalInventory.IsEmpty) {
		foreach (ItemSlot slot in internalInventory) {

		if (slot.Itemstack == null) continue;

		AssetLocation objCode = slot.Itemstack.Collectible.Code;
		float ownTemp = 5f;//this.Block.GetTemperature
		slot.Itemstack.Collectible.SetTemperature(this.Api.World, slot.Itemstack, ownTemp, true);

		if (ownTemp > this.Block.SteelTransitionTemp) {
			//Convert here or on 'DoSmelt' ?
		}

		}
		MarkDirty(true);
		}

		}

		//Duplicated?
		public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder dsc)
		{
		//base.GetBlockInfo(forPlayer, dsc);

		/* 
		 * Contents: 1x Iron/Steel Chisel, Drill-bit, files, ect...
		 */
		dsc.Append("Contents: ");

		if (internalInventory.IsEmpty) {
		dsc.Append("Nothing.\n");
		}
		else {
		ItemStack stack = internalInventory[0].Itemstack;
		dsc.AppendFormat("{0}\u2715 {1}\n", stack.StackSize, stack.GetName( ));
		}

		}
	}
}

