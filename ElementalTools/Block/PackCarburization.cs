using System;
using System.Linq;
using System.Text;


using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElementalTools
{
	/// <summary>
	///Breakable Carburization 'box'; for Steel making.
	/// </summary>
	public class PackCarburization : BlockContainer
	{
		public const string steelTransitionTempKey = @"SteelTransitionTemp";
		public const string steelTransitionTimeKey = @"SteelTransitionTime";


		//Recipie Options #1: Charcoal & Bonemeal & Blue-clay
		//Recipie Options #2: Leather & Fat & Blue-clay

		//Heat to 'Red' hot for ~ 30-60 minutes (equive to ??? game cook time ??? )

		internal PackCarburizationEntity Entity(BlockPos here)
		{
		var pcEnt = api.World.BlockAccessor.GetBlockEntity(here) as PackCarburizationEntity;

		if (pcEnt == null) {
		api.World.Logger.Warning($"PackCarburization [{here}]: BlockEntity NULL! (regenerating)");
		api.World.BlockAccessor.SpawnBlockEntity(ElementalToolsSystem.PackCarburizationEntityNameKey, here);
		}
		return null; 
		}

		public float SteelTransitionTemp {
			//Celcius
			get {
			if (this.Attributes[steelTransitionTempKey].Exists) 
				{ return this.Attributes[steelTransitionTempKey].AsFloat(); }

			return 999f;
			}
		}

		public float SteelTransitionTime {
			//Seconds
			get
			{
			if (this.Attributes[steelTransitionTempKey].Exists) { return this.Attributes[steelTransitionTempKey].AsFloat( ); }

			return 999f;
			}
		}

		/// <summary>
		/// Clone Recipie component data / attributes
		/// </summary>
		/// <returns>The created by crafting.</returns>
		/// <param name="allInputslots">All inputslots.</param>
		/// <param name="outputSlot">Output slot.</param>
		/// <param name="byRecipe">By recipe.</param>
		public override void OnCreatedByCrafting(ItemSlot[ ] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
		{
		//Find the one Tool head / or anything Iron.
        var ironThingSlot = (from inputSlot in allInputslots
										where inputSlot.Empty == false
										//where       inputSlot.Itemstack.Collectible.MatterState         
			                			 where inputSlot.Itemstack.Collectible.IsFerricMetal()
		                	 			select inputSlot).Single();
		//Category: survival/itemtypes/toolhead/
		//Variant(s):   metal,	material
		//tool-stock, tool-heads, plates, scale, lamellae, chainmail 
		//NOT: Ingots, Whole Anvils, big Gears, chunky large things....this ain't mass-production...

		//outputSlot.Itemstack.Attributes = ironThingSlot.Itemstack.Attributes.Clone( );

		ItemStack[ ] encapsulatedItems = new ItemStack[ ] { ironThingSlot.Itemstack.Clone( ) };//More than 1 or Quantity *#
     	SetContents(outputSlot.Itemstack, encapsulatedItems );


		}

		/// <summary>
		/// When being selected in Creative mode...
		/// </summary>
		/// <returns>The pick block.</returns>
		/// <param name="world">World.</param>
		/// <param name="pos">Position.</param>
		public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
		{/*
		ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));

		BlockEntityContainer bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer;

		if (bec != null) {
		SetContents(stack, bec.GetContentStacks( ));
		}


		BlockEntityCrock becrock = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCrock;
		if (becrock == null) return stack;

		ItemStack[ ] stacks = becrock.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray( );
		if (becrock.RecipeCode != null) {
		stack.Attributes.SetString("recipeCode", becrock.RecipeCode);
		stack.Attributes.SetFloat("quantityServings", becrock.QuantityServings);
		stack.Attributes.SetBool("sealed", becrock.Sealed);
		}

		return stack;
		*/
		return null;
		}


		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		//Add tooltip indicating contents...temperature, elapsed heat time, ect...
		float temp = GetTemperature(world, inSlot.Itemstack);//TODO: Get REAL Ambient
		if (temp > 20) {
		dsc.AppendLine(Lang.Get("Temperature: {0:F1}°C", temp));
		}

		var stuffedInside = GetContents(world, inSlot.Itemstack);
		if (stuffedInside != null) 
		{
		dsc.Append("Contents: \n");
		foreach (var thing in stuffedInside) {
		dsc.AppendFormat("{1} \u00d7 {0}\n", thing.GetName( ),thing.StackSize);
		}

		}

		}





		/* 
		 * GetMeltingDuration 
		 * GetMeltingPoint
		 * CanSmelt
		 * DoSmelt
		 * SetTemperature <<< Different Formula? // TODO: Thermal conduction of ceramic...
		 * 
		 */
		public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
		#if DEBUG
		return 5f;
		#endif
		//Randomize?
		return SteelTransitionTime;
		}

		public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
		return SteelTransitionTemp;
		}

		public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
		{
		//return base.CanSmelt(world, cookingSlotsProvider, inputStack, outputStack);

		//Does pack contain a Iron item/block ?
		//Is there an 'Upgrade' for that exact Item / Block ?
		//Output stack should be _EMPTY_
		var stuffedInside = GetContents(world, inputStack);
		if (stuffedInside != null) {
		var stack = stuffedInside.First( );
		var isIron = stack.Collectible.IsFerricMetal( );
		#if DEBUG
		world.Logger.VerboseDebug("Iron ready to smelt? {0}", isIron);
		#endif
		return isIron;		
		}

		return false;
		}

		public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
		{
		#if DEBUG
			world.Logger.VerboseDebug("Invoked: 'DoSmelt' CookSlots#{1} In.stk: {0} ", (inputSlot.Empty ? "empty" : inputSlot.Itemstack.Collectible.Code.ToShortString()),cookingSlotsProvider.Slots.Length);
		#endif

		//base.DoSmelt(world, cookingSlotsProvider, inputSlot, outputSlot);
		//Remap metal type of contained item...Iron beccomes Austentic 'steel' - Quenching is ITEM SPECIFIC!
		//Change own 'type' to "fired"...
		ItemStack[ ] stuffInside = GetContents(world, inputSlot.Itemstack);
		
		//ItemStack smeltedStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone(); //transform - to 'fired' pack
		if (stuffInside != null && stuffInside.Length > 0) {
		ItemStack contentStack = stuffInside.First( );
		PackCarburization firedPack = world.GetBlock(ElementalToolsSystem.fired_carburizationPackCode) as PackCarburization;
		
		var temperature = contentStack.Collectible.GetTemperature(world, contentStack);
		ItemStack outputStack = new ItemStack(firedPack);
		//outputStack.SetFrom( firedPack);						

		if (contentStack.Class == EnumItemClass.Block) {
		var oldThing = contentStack.Block.Code;
		var transumtedThing = contentStack.Block.TransmuteByVariants(
			new string[ ] { ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.MaterialNameKey },
			ElementalToolsSystem.SteelNameKey);
		
		var convertedThing = world.GetBlock(transumtedThing);

		if (convertedThing == null) {
		world.Logger.VerboseDebug("Non-existant (Block): {1} from {0} !", oldThing, transumtedThing);
		outputSlot.Itemstack = null;
		inputSlot.Itemstack = null;
		inputSlot.MarkDirty( );
		outputSlot.MarkDirty( ); //?
		return;
		}

		contentStack.Block.Code = transumtedThing;
		contentStack.Id = convertedThing.Id;
		#if DEBUG
		world.Logger.VerboseDebug("Transmuting (Block): {0} >>> {1}", oldThing, transumtedThing);		
		#endif
		}
		else {
		var oldThing = contentStack.Item.Code;
		var transumtedThing = contentStack.Item.TransmuteByVariants(
			new string[ ] { ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.MaterialNameKey },
			ElementalToolsSystem.SteelNameKey);
		
		var convertedThing = world.GetItem(transumtedThing);

		if (convertedThing == null) {
		world.Logger.VerboseDebug("Non-existant (Item): {1} from {0} !", oldThing, transumtedThing);
		outputSlot.Itemstack = null;
		inputSlot.Itemstack = null;
		inputSlot.MarkDirty( );
		outputSlot.MarkDirty(); //?
		return;
		}

		contentStack.Item.Code = transumtedThing;
		contentStack.Id = convertedThing.Id;
		#if DEBUG
		world.Logger.VerboseDebug("Transmuting (Item): {0} >>> {1}", oldThing, transumtedThing);
		
		#endif
		}


		outputStack.Attributes = contentStack.Attributes.Clone( );
		outputStack.TempAttributes = contentStack.TempAttributes.Clone( );
		outputStack.Collectible.SetTemperature(world, outputStack, temperature);
		firedPack.SetContents(outputStack, GetContents(world, contentStack));
		SetTemperature(world, outputStack, temperature - 100f, false);
		outputSlot.Itemstack = outputStack;
		inputSlot.Itemstack = null;
		//inputSlot.MarkDirty( );
		//outputSlot.MarkDirty(); //?
		#if DEBUG
		world.Logger.VerboseDebug("Finished: 'DoSmelt' " );
		#endif	
		}
		else {
		#if DEBUG
		world.Logger.Warning("Pack contents emtpy?!? ");
		#endif
		}

		
		}

		public override bool CanSpoil(ItemStack itemstack)
		{
		return false;
		}

		public override void OnGroundIdle(EntityItem entityItem)
		{
		//How often does this get called?
		IWorldAccessor world = entityItem.World;
		if (world.Side.IsClient()) return;

			if ((entityItem.Swimming || entityItem.FeetInLiquid) ) 
			{
			//Something happens...in liquid phase water
			var blockHere = world.BlockAccessor.GetBlock(entityItem.Pos.AsBlockPos);
			if (blockHere.Code.BeginsWith(GlobalConstants.DefaultDomain, "water")) {
					//blockHere.LiquidCode == "water"
					ItemStack[ ] stacks = GetContents(world, entityItem.Itemstack);

					//Spawn first?
					world.SpawnItemEntity(stacks.First(), entityItem.ServerPos.XYZ);
					//Or destroy? and eject contents?
					entityItem.Die(EnumDespawnReason.Death);

					}
			}
			//RESEARCH: Block to EntityItem transition - need to customize or attach event handlers there ?


		}


	}
}

