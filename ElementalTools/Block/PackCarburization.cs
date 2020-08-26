using System;
using System.Linq;
using System.Text;


using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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

		internal const string outputOverrideKey = @"outputOverride";
		internal const string maxQuantityKey = @"maxQuantity";
		internal const string extraCookTimeKey = @"extraCookTime";

		internal const float maxInnerTemperature = 1000f;

		//Recipie Options #1: Charcoal & Bonemeal & Blue-clay
		//Recipie Options #2: Leather & Fat & Blue-clay

		//Heat to 'Red' hot for ~ 30-60 minutes (equive to ??? game cook time ??? )

		//Output 'Blister' steel... not 'Shear' steel

		internal PackCarburizationEntity Entity(BlockPos here)
		{
		var pcEnt = api.World.BlockAccessor.GetBlockEntity(here) as PackCarburizationEntity;

		if (pcEnt == null) {
		api.World.Logger.Warning($"PackCarburization [{here}]: {this.EntityClass} Not-present/NULL! (regenerating)");
		api.World.BlockAccessor.SpawnBlockEntity(ElementalToolsSystem.PackCarburizationEntityNameKey, here);
		pcEnt = api.World.BlockAccessor.GetBlockEntity(here) as PackCarburizationEntity;
		}
		return pcEnt; 
		}

		public float SteelTransitionTemp {
			//Celcius
			get {
			if (this.Attributes[steelTransitionTempKey].Exists) 
				{ return this.Attributes[steelTransitionTempKey].AsFloat(); }

			return 750f;// Or 900C?
			}
		}

		public float SteelTransitionTime {
			//Seconds
			get
			{
			if (this.Attributes[steelTransitionTempKey].Exists) { return this.Attributes[steelTransitionTempKey].AsFloat( ); }

			return 38f;
			}
		}

		public string State {
			get
			{
			return this.Variant[@"type"];			
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
		//Failsafe[s]
		if (byRecipe == null || byRecipe.Ingredients == null || byRecipe.IngredientPattern == null || byRecipe.Output == null ) {
		string name = "unset!";
		name = byRecipe?.Name.ToString( );
		api.World.Logger.Error("Invalid / Incomplete / Corrupt Recipe: {0}", name);
		return;
		}

		//Find the one Tool head / or anything Iron.
        var ironThingSlot = (from inputSlot in allInputslots
										where inputSlot.Empty == false										
			                			 where inputSlot.Itemstack.Collectible.IsFerricMetal()
		                	 			select inputSlot).Single();
		int ironQtyMax = 1, ironQty = 1;
		if (byRecipe.Ingredients.ContainsKey(ElementalToolsSystem.RecipieWildcard))
		{
		ironQty = byRecipe.Ingredients[ElementalToolsSystem.RecipieWildcard].Quantity;
		}

		if (byRecipe.Attributes != null && byRecipe.Attributes.KeyExists(maxQuantityKey)) {
		ironQtyMax = byRecipe.Attributes[maxQuantityKey].AsInt(1);
		}
		//Category: survival/itemtypes/toolhead/
		//Variant(s):   metal,	material
		//tool-stock, tool-heads, scale, chainmail 
		//NOT: Ingots, Whole Anvils, big Gears, chunky large things....thats more casting iron...

		//outputSlot.Itemstack.Attributes = ironThingSlot.Itemstack.Attributes.Clone( );

		ItemStack[ ] encapsulatedItems = new ItemStack[ ] { ironThingSlot.Itemstack.Clone( ) };
		encapsulatedItems.First( ).StackSize = Math.Min(ironQty,ironQtyMax);;
     	SetContents(outputSlot.Itemstack, encapsulatedItems );
		if (byRecipe.Attributes !=null && byRecipe.Attributes.KeyExists(outputOverrideKey)) {
			SetOutputOverride(outputSlot.Itemstack, byRecipe.Attributes[outputOverrideKey].AsString( ));
			}
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
		 * SetTemperature <<< Different Formula? // TODO: Thermal conduction of clay/ceramic...
		 * 
		 */
		public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{//TimeSpan - would have been far better a return type
		var extraCookTime = GetExtraCookTime(inputSlot.Itemstack);

		return SteelTransitionTime + extraCookTime;
		}

		public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
		return SteelTransitionTemp;
		}

		public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
		{
		//return base.CanSmelt(world, cookingSlotsProvider, inputStack, outputStack);

		//Does pack contain a Iron item/block ?
		
		//Output stack should be _EMPTY_
		var stuffedInside = GetContents(world, inputStack);
		if (stuffedInside != null && stuffedInside.Length > 0) {
		var firstThing = stuffedInside.First( );
		var isIron = firstThing.Collectible.IsFerricMetal( );
		//#if DEBUG
		//world.Logger.VerboseDebug("Iron contents to smelt? {0} -> {1}", isIron, firstThing.GetName());
		//#endif
		
		return isIron;		
		}

		return false;
		}

		public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
		{
		#if DEBUG
			world.Logger.VerboseDebug("Invoked: 'DoSmelt' CookSlots#{1} In.stk: {0} ", (inputSlot.Empty ? "empty" : inputSlot.Itemstack.Collectible.Code.ToShortString()),cookingSlotsProvider.Slots.Length);
		#endif
					
		//Remap metal type of contained item...Iron beccomes Austentic 'steel' - Quenching is ITEM SPECIFIC!
		//Change own 'type' to "fired"...
		ItemStack[ ] stuffInside = GetContents(world, inputSlot.Itemstack);
		var overrideCode = GetOutputOverride(inputSlot.Itemstack);
		//ItemStack smeltedStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone(); //transform - to 'fired' pack
		if (stuffInside != null && stuffInside.Length > 0) {
		ItemStack contentStack = stuffInside.First( );
		PackCarburization firedPack = world.GetBlock(ElementalToolsSystem.fired_carburizationPackCode) as PackCarburization;
		
		var temperature = contentStack.Collectible.GetTemperature(world, contentStack);
		ItemStack outputStack = new ItemStack(firedPack);
		//outputStack.SetFrom( firedPack);						

		if (contentStack.Class == EnumItemClass.Block) {
		var oldThing = contentStack.Block.Code;
		var transumtedThing = overrideCode ?? contentStack.Block.TransmuteByVariants(
			new string[ ] { ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.MaterialNameKey },
			ElementalToolsSystem.SteelNameKey);		

		var convertedBlock = world.GetBlock(transumtedThing);

		if (convertedBlock == null) {
		world.Logger.VerboseDebug("Non-existant (Block): {1} from {0} !", oldThing, transumtedThing);
		outputSlot.Itemstack = null;
		inputSlot.Itemstack = null;
		inputSlot.MarkDirty( );
		outputSlot.MarkDirty( ); //?
		return;
		}

		contentStack.Block.Code = transumtedThing;
		contentStack.Id = convertedBlock.Id;
		#if DEBUG
		world.Logger.VerboseDebug("Transmuting (Block): {0} >>> {1}", oldThing, transumtedThing);		
		#endif
		}
		else {
		var oldThing = contentStack.Item.Code;
		var transumtedThing = overrideCode ?? contentStack.Item.TransmuteByVariants(
			new string[ ] { ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.MaterialNameKey },
			ElementalToolsSystem.SteelNameKey);
		
		var convertedItem = world.GetItem(transumtedThing);

		if (convertedItem == null) {
		world.Logger.VerboseDebug("Non-existant (Item): {1} from {0} !", oldThing, transumtedThing);
		outputSlot.Itemstack = null;
		inputSlot.Itemstack = null;
		inputSlot.MarkDirty( );
		outputSlot.MarkDirty(); //?
		return;
		}

		contentStack.Item.Code = transumtedThing;
		contentStack.Id = convertedItem.Id;
		#if DEBUG
		world.Logger.VerboseDebug("Transmuting (Item): {0} >>> {1}", oldThing, transumtedThing);		
		#endif
		}
		outputSlot.Itemstack = outputStack.Clone();

		temperature = Math.Min(temperature, maxInnerTemperature);
		contentStack.Collectible.SetTemperature(world, contentStack, temperature);//TODO: Temperature clamping inside contents of stack...

		ItemStack[ ] transmutedItems = new ItemStack[ ] { contentStack.Clone( ) };
		transmutedItems.First( ).StackSize = 1;//There can be only 1, per pack
		SetContents(outputSlot.Itemstack, transmutedItems);
		
		inputSlot.Itemstack = null;		

		#if DEBUG
		world.Logger.VerboseDebug("Contents of pack: {0}", contentStack);
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

		public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
		{
		EnumTool? tool = itemslot.Itemstack?.Collectible?.Tool;

		if (tool == EnumTool.Hammer || tool == EnumTool.Pickaxe || tool == EnumTool.Shovel || tool == EnumTool.Sword || tool == EnumTool.Spear || tool == EnumTool.Axe || tool == EnumTool.Hoe) {
		if (counter % 5 == 0 || remainingResistance <= 0) {
		double posx = blockSel.Position.X + blockSel.HitPosition.X;
		double posy = blockSel.Position.Y + blockSel.HitPosition.Y;
		double posz = blockSel.Position.Z + blockSel.HitPosition.Z;
		player.Entity.World.PlaySoundAt(remainingResistance > 0 ? Sounds.GetHitSound(player) : Sounds.GetBreakSound(player), posx, posy, posz, player, true, 16, 1);
		}

		return remainingResistance - 0.05f;
		}

		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
		SimpleParticleProperties ash =
			new SimpleParticleProperties(
				9, 18,
				ColorUtil.ToRgba(127, 32, 32, 32),
				new Vec3d(pos.X, pos.Y, pos.Z),
				new Vec3d(pos.X + 1, pos.Y + 1, pos.Z + 1),
				new Vec3f(-0.2f, -0.1f, -0.2f),
				new Vec3f(0.2f, 0.2f, 0.2f),
				1.5f,
				0,
				0.5f,
				1.0f,
				EnumParticleModel.Quad
			);

		ash.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -200);
		ash.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2);

		world.SpawnParticles(ash);

		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{
			return Lang.Get(this.Code.Domain +":block-"+this.Code.Path);//Domain needed...
		}

		private void SetOutputOverride(ItemStack containerStack, string overrideCode)
		{
		if (!string.IsNullOrEmpty(overrideCode)) {
		containerStack.Attributes.SetString(outputOverrideKey, overrideCode);
		}
		}

		private AssetLocation GetOutputOverride(ItemStack containerStack)
		{
		if (containerStack.Attributes != null && containerStack.Attributes.HasAttribute(outputOverrideKey)) 
			{
			var code =  new AssetLocation(ElementalToolsSystem.fmaKey, containerStack.Attributes.GetString(outputOverrideKey));

			return code;
			}
		return null;
		}

		private int GetExtraCookTime(ItemStack containerStack)
		{
		if (containerStack.Attributes  != null && containerStack.Attributes.HasAttribute(extraCookTimeKey)) {
		return containerStack.Attributes.GetInt(extraCookTimeKey, 0);
		}
		return 0;
		}
	}
}

