using System;
using System.Text;
using System.Linq;


using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace ElementalTools
{
	/// <summary>
	/// GENERIC Steel item. (Tool / Weapon ) [Possibly: Temperable and/or Hardenable ]
	/// </summary>
	public class SteelWrapItem<OrigItem>: SteelBaseItem where OrigItem : Item, new()
	{		
		private OrigItem WrappedItem;//Special placeholder replica - for calling ancestor (base) class



		/*
		public virtual float GetAttackPower (IItemStack withItemStack)
		public virtual float GetDurability (IItemStack itemstack) //Leave unchanged - it never increases...
		public virtual float GetMiningSpeed (IItemStack itemstack, Block block)
		public virtual void DamageItem (IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
		public virtual void OnConsumedByCrafting (ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		public virtual void OnHeldDropped (IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)

		public int MiningTier  // public int ToolTier;
		public virtual float GetMiningSpeed; MiningSpeed //Speed up for Edged Picks?

		public override void OnAttackingWith
		public virtual bool OnBlockBrokenWith (IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)


		public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)

		public virtual bool MatchesForCrafting -- //Refect if trying to oversharpen
		 * */

		public SteelWrapItem( ) //Since It Invokes that for the new type of T anyways...
		{
		WrappedItem = new OrigItem();		
		}

		public SteelWrapItem(int itemId) : base(itemId)
		{
		WrappedItem = new OrigItem();
		WrappedItem.ItemId = itemId;
		WrappedItem.MaxStackSize = 1;
		}

		public override void OnLoaded(ICoreAPI api)
		{
		//Needs to fully populate equivalent <item>		
		PopulatePlaceholderItemFields();		
		}

		public override void OnUnloaded(ICoreAPI api)
		{
		WrappedItem.OnUnloaded(api);
		}

		private void PopulatePlaceholderItemFields()
		{
		string trueClassName = string.Empty;
		if (!string.IsNullOrEmpty(this.Class)) {
		trueClassName = this.Class.Split('_').Last( );// 'Steel_ItemAxe' -> ItemAxe
		}
		else {
		api.World.Logger.Error("Class (name) for ItemID# {0} - null!", this.ItemId);
		if (WrappedItem == null) api.Logger.Error("Failed to resolve Wrapped Item Class! Code:[{0}]", this.Code);
		trueClassName = WrappedItem.GetType( ).Name;
		api.World.Logger.Error("Substituting class name from wrapped Item '{0}'", trueClassName);
		}

		WrappedItem = ( OrigItem )api.ClassRegistry.CreateItem(trueClassName);//( T )api.World.Items[this.ItemId];// Old Item class (name) should still exist.
				
		OverwriteFields(WrappedItem);
		}





		#region Specific_Behavior

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		if (inSlot == null || inSlot.Empty || inSlot.Inventory == null) {
		#if DEBUG
		api.World.Logger.Warning("GetHeldItemInfo -> Invetory / slot / stack: FUBAR!");
		#endif
		return;
		}

		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		SteelAspects.GetHeldItemInfo(api, inSlot, dsc, world, withDebugInfo);
		}







		/// <summary>
		/// Future use...
		/// </summary>
		/// <returns>The attack power.</returns>
		/// <param name="withItemStack">With item stack.</param>
		public override void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
		{
		WrappedItem.OnHeldDropped(world, byPlayer, slot, quantity, ref handling);
		}

		/// <summary>
		/// For; Quench-harden...
		/// </summary>
		/// <param name="entityItem">Entity item.(Itself)</param>
		public override void OnGroundIdle(EntityItem entityItem)
		{
		SteelAspects.QuenchHarden(this, entityItem, api);
		base.OnGroundIdle(entityItem);
		}

		#endregion


		#region Steel Affects

		public override float GetAttackPower(IItemStack withItemStack)
		{
		var defaultPower = base.GetAttackPower(withItemStack);

		if (this.Sharpenable) {
		var sharpness = Sharpness(withItemStack);
		float pctBoost = 0;//CONSIDER: Perhaps make this external?
		switch (sharpness) {
		case SharpnessState.Rough:
			pctBoost = -0.25f;
			break;

		case SharpnessState.Dull:
			pctBoost = -0.20f;
			break;

		case SharpnessState.Honed:
			pctBoost = 0.10f;
			break;
		case SharpnessState.Keen:
			pctBoost = 0.15f;
			break;
		case SharpnessState.Sharp:
			pctBoost = 0.20f;
			break;
		case SharpnessState.Razor:
			pctBoost = 0.25f;
			break;
		}

		return defaultPower + (pctBoost * defaultPower);
		}
		return defaultPower;
		}


		public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
		{
		bool edged = this.Edged;
		bool weapon = this.Weapon;
		float targetArmorFactor = 0.0f;
		

		/*DETERMINE: 
		 * Usage - Edged weapon attack Vs. creature Sc.#1 [What about armored players?]
		 * Non-edged weapon vs. creature Sc. #2 [What about armored players?]
		 * [Improvised-arms] Edged-Tool (non-weapon) vs. Creature Sc.#3
		 * [Improvised-arms] Blunt-Tool (non-weapon) vs. Creature Sc.#4
		 * Tool Against Envrionment (Pickaxe / Axe / Propick / Saw / Shovel) Sc. #5
		 * WEAPONS Vs. Envrionment (hiting dirt with a sword!) Sc. #6
		 * Tools - don't really benefit from edges vs. envrionment...?
		*/

		//Only called for attacks on ENTITIES. Scen# 1 - 4 here.
		api.World.Logger.VerboseDebug($"OnAttackingWith:: (Weap:{weapon},Edge:{edged}) {byEntity.Code} -> {attackedEntity.Code}");



		/*			 
		if (this.DamagedBy != null && this.DamagedBy.Contains (EnumItemDamageSource.Attacking) && attackedEntity != null && attackedEntity.Alive) {
		this.DamageItem (world, byEntity, itemslot, 1);
		}
		*/

		if (this.Hardness(itemslot.Itemstack) > HardnessState.Hard) {
		bool catasptrophicFailure = world.Rand.Next(1, 1000) >= 999;
		if (catasptrophicFailure) {
		world.Logger.VerboseDebug("Catastrophic brittle fracture of {0} !", this.Code);
		SteelAspects.SetHitpoints(itemslot.Itemstack, 0);
		this.DamageItem(world, byEntity, itemslot, 9999);
		return;
		}

		}

		//WrappedItem.OnAttackingWith(world, byEntity, attackedEntity, itemslot);


		}




		public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
		{
		if (api.Side.IsClient()) return true;

		bool edged = this.Edged;
		bool weapon = this.Weapon;
		var targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
		int targetTier = targetBlock.ToolTier;
		float targetResistance = targetBlock.Resistance;
		bool recomendedUsage = this.RecomendedUsage(targetBlock.BlockMaterial);
		var hardness = this.Hardness(itemslot.Itemstack);

		//ERROR: NullReferenceException !

		//Only called for attacks on BLOCKS / Envrionment. Scen# 5 - 6 here.	

		//Weapon & Tool applicability: Is rate > 1 ? then - its sorta OK...ish.

		//Harndess Vs. Block-Resistance...
		//Tool Specific special damage reduction rate: e.g. scythe, hoe, knife, here...
		//By MiningSpeed 

		#if DEBUG
		api.World.Logger.VerboseDebug($"OnBlockBrokenWith:: (Weap:{weapon},Edge:{edged},OK: {recomendedUsage},T.T#{targetTier}) {byEntity.Code} -> {targetBlock.Code}");
		#endif

		if (recomendedUsage == false &&  hardness > HardnessState.Hard) {
		bool catasptrophicFailure = world.Rand.Next(1, 1000) >= (999 - (targetTier * 5));
		
		if (catasptrophicFailure) {
		world.Logger.VerboseDebug("Catastrophic brittle fracture of {0} !", this.Code);
		SteelAspects.SetHitpoints(itemslot.Itemstack, 0);
		this.DamageItem(world, byEntity, itemslot, 9999);
		return true;
		}


		}

		return WrappedItem.OnBlockBrokenWith(world, byEntity, itemslot, blockSel);
		//Post Damage reduction? or Increase??
		
		}

		//This Method signature leaves _ALOT_ to be ascertained about the _ACUTAL_ Scenario / REASON of damage...
		//Tool/Weapon Vs. What?
		public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
		{
		//ItemAxe: just repeatedly calls DamageItem....instead of accrued count...lame.

		bool byPlayer = byEntity is EntityPlayer;
		bool edgeBlunting = false;

		float resistance = 0.0f;

		if (!itemslot.Empty) 
		{
		var hardness = this.Hardness(itemslot.Itemstack);
		switch (hardness) 
				{
				case HardnessState.Soft:
					resistance = 0.3f;
					edgeBlunting = world.Rand.Next(1, 100) >= 99;
					break;
				case HardnessState.Medium:
					resistance = -0.1f;
					edgeBlunting = world.Rand.Next(1, 200) >= 199;
					break;
				case HardnessState.Hard:
					resistance = -0.25f;
					edgeBlunting = world.Rand.Next(1, 300) >= 299;
					break;
				case HardnessState.Brittle:
					resistance = -0.3f;
					edgeBlunting = world.Rand.Next(1, 400) >= 399;

					break;
				}
		}

		amount = amount < 1 ? 1 : amount;//Zero or negative damange, possible but WHY?

		float amountFractional =  (resistance * amount);
		var dmgRandomizer = NatFloat.createUniform(amount, amountFractional);
		var actualAmount = Math.Abs( (int)dmgRandomizer.nextFloat( ));
		
		if (edgeBlunting) this.Dull(itemslot.Itemstack);
		

		base.DamageItem(world, byEntity, itemslot, actualAmount);
		}



		/// <summary>
		/// Handle Sharpening by Item + Craft-Grid use
		/// </summary>
		/// <returns>The consumed by crafting.</returns>
		/// <param name="allInputSlots">All input slots.</param>
		/// <param name="stackInSlot">Stack in slot.</param>
		/// <param name="gridRecipe">Grid recipe.</param>
		/// <param name="fromIngredient">From ingredient.</param>
		/// <param name="byPlayer">By player.</param>
		/// <param name="quantity">Quantity.</param>
		public override void OnConsumedByCrafting(ItemSlot[ ] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{

		if (fromIngredient.IsTool) {		

		//Edged tool vs. non-edged tool
		bool edgedTool = this.Edged;
		
		float hardnessMult =((int)HardnessState.Brittle+1) / ((int)this.Hardness(stackInSlot.Itemstack)+1) * 0.25f;
		float wearMax = 1;
		if (edgedTool) {
		wearMax = ( byte )SharpnessState.Razor - ( byte )this.Sharpness(stackInSlot.Itemstack);
		}

		int actualDmg = ( int )Math.Round(NatFloat.createTri(wearMax, hardnessMult).nextFloat( ), 1);

		#if DEBUG
		api.World.Logger.VerboseDebug($"tooluse [{this.Code}] --> Harndess effect: [ Hardness {hardnessMult} Vs. Rate: {wearMax} apply dmg: {actualDmg}, edged: {edgedTool} ]");
		#endif

		stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, actualDmg);
		return;
		}

		WrappedItem.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
		}




		//TODO: OnCreated ByCrafting - Copy properties from 'parent' to steel item/block!
		public override void OnCreatedByCrafting(ItemSlot[ ] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
		{
		//Failsafe[s]
		if (byRecipe == null || byRecipe.Ingredients == null || byRecipe.IngredientPattern == null || byRecipe.Output == null) {
		string name = "unset!";
		name = byRecipe?.Name.ToString( );
		api.World.Logger.Error("Invalid / Incomplete / Corrupt Recipe: {0}", name);
		return;
		}

		var steelItemSlot = (from inputSlot in allInputslots
							 where inputSlot.Empty == false
							 where inputSlot.Itemstack.Class == EnumItemClass.Item
							 where inputSlot.Itemstack.Collectible.IsSteelMetal( )
							 select inputSlot).SingleOrDefault( );

		var sharpenerItemSlot = (from inputSlot in allInputslots
							 where inputSlot.Empty == false
							 where inputSlot.Itemstack.Class == EnumItemClass.Item
		                     where inputSlot.Itemstack.Collectible.IsSharpener()
							 select inputSlot).SingleOrDefault( );

		if (steelItemSlot != null) 
		{
			if (steelItemSlot.Itemstack.Item is ISteelByStack) 
			{
			var steelItem = steelItemSlot.Itemstack.Item;

			#if DEBUG
			api.World.Logger.VerboseDebug("Input (ingredient) Item {0} supports; Steel Interface ", steelItem.Code);
			#endif
			if (!outputSlot.Empty && outputSlot.Itemstack.Class == EnumItemClass.Item
				&& outputSlot.Itemstack.Item is ISteelByStack) 
				{
				var outputItem = outputSlot.Itemstack.Item;
				var fullMetalInterface = outputSlot.Itemstack.Item as ISteelByStack;
				#if DEBUG
				api.World.Logger.VerboseDebug("Output Item {0} supports; Steel Interface ", steelItem.Code);
				#endif

				fullMetalInterface.CopyStackAttributes(steelItemSlot.Itemstack, outputSlot.Itemstack);

				if (sharpenerItemSlot != null) fullMetalInterface.Sharpen(outputSlot.Itemstack);
				#if DEBUG
				api.World.Logger.VerboseDebug("Attributes perpetuated from {0} to {1} ", steelItem.Code, outputItem.Code);
				#endif
				}
			}
		}

		}

		public override float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
		{
		var baseSpeed = 1f;
		//Boost for Edged tools / weapons
		if (MiningSpeed != null && MiningSpeed.ContainsKey(block.BlockMaterial) && this.Tool.EdgedImpliment()) {
				
		baseSpeed = MiningSpeed[block.BlockMaterial] * GlobalConstants.ToolMiningSpeedModifier;
		float pctBoost = 0f;		
		switch (Sharpness(itemstack)) {
		case SharpnessState.Rough:
			pctBoost = -0.35f;
			break;
		case SharpnessState.Dull:
			pctBoost = -0.20f;
			break;
		case SharpnessState.Honed:
			pctBoost = 0.10f;
			break;
		case SharpnessState.Keen:
			pctBoost = 0.20f;
			break;
		case SharpnessState.Sharp:
			pctBoost = 0.25f;
			break;
		case SharpnessState.Razor:
			pctBoost = 0.30f;
			break;
		}

		return baseSpeed + (pctBoost * baseSpeed);
		}

		return baseSpeed;
		}

		public override int GetItemDamageColor(ItemStack itemstack)
		{
		SharpnessState edge = Sharpness(itemstack);

		switch (edge) {
		case SharpnessState.Rough:
			return SteelAspects.color_Rough;

		case SharpnessState.Dull:
			return SteelAspects.color_Dull;

		case SharpnessState.Honed:
			return SteelAspects.color_Honed;

		case SharpnessState.Keen:
			return SteelAspects.color_Keen;

		case SharpnessState.Sharp:
			return SteelAspects.color_Sharp;

		case SharpnessState.Razor:
			return SteelAspects.color_Razor;
		}

		return SteelAspects.color_Default;
		}

		#endregion


		//Wire up all invokes >>> to NOT call Base - but (WrappedItem) T instead !
		#region Wrapped_Calls

		public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter) => WrappedItem.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) => WrappedItem.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) => WrappedItem.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) => WrappedItem.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

		public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection) => WrappedItem.GetToolMode(slot, byPlayer, blockSelection);

		public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode) => WrappedItem.SetToolMode(slot, byPlayer, blockSelection, toolMode);

		public override SkillItem[ ] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => WrappedItem.GetToolModes(slot, forPlayer, blockSel);

		public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling) => WrappedItem.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);

		public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel) => WrappedItem.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel);

		public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel) => WrappedItem.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel);

		public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) => WrappedItem.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);


		/*
		public override TransitionableProperties[ ] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
		{
		return null;//HACK: to stop missing variables from causing a fault
		}
		*/



		public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
		{
		return null;//HACK: to stop missing variables from causing a fault
		}

		public override TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
		{
		return null;//HACK: to stop missing variables from causing a fault
		}

		public override TransitionState[ ] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
		{
		return  new TransitionState[0];//HACK: to stop missing variables from causing a fault
		}

		public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
		{
		return 0f;//HACK: to stop missing variables from causing a fault
		}

		public override float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
		{
		return 0f;//HACK: to stop missing variables from causing a fault
		}


		#endregion






		protected void SetTimestamp(EntityItem entityItem)
		{
			
		if (!entityItem.Attributes.HasAttribute(SteelAspects._timestampKey)) {
			entityItem.Attributes.SetLong(SteelAspects._timestampKey, DateTime.Now.Ticks);
		}
		}

		protected TimeSpan GetTimestampElapsed(EntityItem entityItem)
		{
		if (entityItem.Attributes.HasAttribute(SteelAspects._timestampKey)) {
		var ts = TimeSpan.FromTicks(entityItem.Attributes.GetLong(SteelAspects._timestampKey));
		return ts.Subtract(TimeSpan.FromTicks(DateTime.Now.Ticks)).Negate();
		}
		return TimeSpan.Zero;
		}
	}
}

