using System;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ElementalTools
{
	public class PackCarburizationEntity : BlockEntityContainer
	{
		private const string _invName = @"carburization_pack";
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
			return _invName;
			}
		}

		public new PackCarburization Block {
			get { return base.Block as PackCarburization; }
		}

		public float Temperature { get; set; }


		public PackCarburizationEntity( ) 
		{			
			internalInventory = new InventoryGeneric(1, _invName+"-0", null);//required: 'Instance ID' is a Dummy...token (real value set later)
		}


		protected override void OnTick(float dt)
		{

		if (this.Temperature > 20) {
		this.Temperature -= 1f;//Rain? Compute vs. ambient temp / biome, on snow/ice...	   
		}

		if (!internalInventory.Empty) {
		foreach (ItemSlot slot in internalInventory) {

		if (slot.Empty) continue;

		AssetLocation objCode = slot.Itemstack.Collectible.Code;
		
		slot.Itemstack.Collectible.SetTemperature(this.Api.World, slot.Itemstack, Temperature, false);

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

		if (internalInventory.Empty) {
		dsc.Append("Nothing.\n");
		}
		else {
		
		foreach (var thing in internalInventory) {
		if (thing.Empty) continue;
		dsc.AppendFormat("{1} \u00d7 {0}\n", thing.Itemstack.GetName( ), thing.StackSize);
		}
		}

		}


		public override void OnBlockPlaced(ItemStack byItemStack = null)
		{
		if (byItemStack != null ) {
		var contents = this.Block.GetContents(this.Api.World, byItemStack);
		if (contents == null || contents.Length == 0) 
		{
		Api.World.Logger.VerboseDebug("Empty contents from stack !");
		return;
		}
		var temp = this.Block.GetTemperature(this.Api.World, byItemStack);
		internalInventory[0].Itemstack = contents.First( ).Clone( );
		
		//internalInventory[0].Itemstack.SetFrom(byItemStack);				
		this.Temperature = temp;
		}
		else {
		Api.World.Logger.VerboseDebug("No stackdata ?! - thus empty...");
		}
		}


	}
}

