using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

		public float Temperature { get; private set; }


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
		
		slot.Itemstack.Collectible.SetTemperature(this.Api.World, slot.Itemstack, Temperature, true);

		if (Temperature > this.Block.SteelTransitionTemp) {
		//Convert here or on 'DoSmelt' ?
		//Which is really mostly about the clay container...not quenching to make it Martensite

		}

		}
		MarkDirty(true);
		}

		}

		//Duplicated?
		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
		
		if (Temperature > 20) {
		dsc.AppendLine(Lang.Get("Temperature: {0:F1}°C", Temperature));
		}

		/* 
		 * Contents: 1x Iron/Steel Chisel, Drill-bit (unsharpened rod), files, ect...
		 */
		dsc.Append("Contents: \n");

		if (internalInventory.IsEmpty) {
		dsc.Append("Nothing.\n");
		}
		else {
		
		foreach (var thing in internalInventory) {
		dsc.AppendFormat("{1} \u00d7 {0}\n", thing.Itemstack.GetName( ), thing.StackSize);
		}
		}

		}

		//OnBlockPlaced -- Perform base call!

	}
}

