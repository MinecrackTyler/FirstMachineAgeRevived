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

		//Heat to 'Red' hot for ~ 60 minutes (equive to ??? game cook time ??? )

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
										where inputSlot.Itemstack.Collectible.Variant.KeyValueMatch(ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.IronNameKey) 											
                                   		|| inputSlot.Itemstack.Collectible.Variant.KeyValueMatch(ElementalToolsSystem.MaterialNameKey, ElementalToolsSystem.IronNameKey)	
		                	 			select inputSlot).Single();
		//Category: survival/itemtypes/toolhead/
		//Variant(s):   metal,	material
		//tool-heads, plates, scale, lamellae, chainmail 
		//NOT: Ingots, Whole Anvils, big Gears, chunky large things....this ain't mass-production...

		//outputSlot.Itemstack.Attributes = ironThingSlot.Itemstack.Attributes.Clone( );

		ItemStack[] outputItemStacks=null;
     	SetContents(outputSlot.Itemstack, outputItemStacks );


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

		}

		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		{
			//Add tooltip indicating contents...temperature, elapsed heat time, ect...
		
		return String.Empty;
		}



		/* 
		 * GetMeltingDuration 
		 * GetMeltingPoint
		 * CanSmelt
		 * DoSmelt
		 * 
		 * 
		 */
		public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
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
		

		return false;
		}

		public override  void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
		{
		//base.DoSmelt(world, cookingSlotsProvider, inputSlot, outputSlot);
		//Remap metal type of contained item...
		//Change own 'type' to "fired"...

		//ItemStack smeltedStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone(); //transform - to 'fired' pack

		//outputSlot.MarkDirty();
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

		if (entityItem.Swimming && world.Rand.NextDouble( ) < 0.01) 
			{
			//Something happens...

			}
		}


	}
}

