using System;
using System.Text;
using System.Linq;


using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;

namespace ElementalTools
{
	/// <summary>
	/// GENERIC Steel item. (Tool / Weapon / Armor...anything) [Possibly: Temperable and/or Hardenable ]
	/// </summary>
	public class SteelWrap<T>: Item, IAmSteel where T : Item
	{
		internal const string hardenableKeyword = @"hardenable";
		internal const string sharpenableKeyword = @"sharpenable";
		internal const string metalNameKeyword = @"metalName";

		internal const string hardnessKeyword = @"hardness";
		internal const string sharpnessKeyword = @"sharpness";

		internal const string durabilityKeyword = @"durability";

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
		 * */

		#region Static Properties
		public virtual bool Hardenable {
			get
			{
			return this.Attributes[hardenableKeyword].AsBool(false);
			}
		}
			

		public virtual string Name {
			get
			{
			return this.Attributes[metalNameKeyword].AsString("?");
			}
		}

		public virtual bool Sharpenable {
			get
			{
			return this.Attributes[sharpenableKeyword].AsBool(false);
			}
		}
		#endregion

		#region Specific_Behavior

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

		dsc.AppendFormat("\nMetal: '{0}', ",Name);
					
		dsc.AppendFormat("Temper: {0}\n", Hardness(inSlot.Itemstack) );
		
		dsc.AppendFormat("Edge: {0}\n", Sharpness(inSlot.Itemstack) );	//Or surface?		

		}

		public virtual SharpnessState Sharpness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(sharpnessKeyword)) {
		byte[ ] bytes = new byte[1];
		bytes = someStack.Attributes.GetBytes(sharpnessKeyword, bytes);
	 	return bytes == null ? SharpnessState.Rough : ( SharpnessState )bytes[0];
		}

		return SharpnessState.Rough;		
		}

		public virtual void Sharpness(IItemStack someStack, SharpnessState set)
		{
		byte[ ] bytes = new byte[1];
		bytes[0] = (byte)set;
		someStack.Attributes.SetBytes(sharpnessKeyword, bytes);
		}

		public virtual HardnessState Hardness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(hardnessKeyword)) {
		byte[ ] bytes = new byte[1];
		bytes = someStack.Attributes.GetBytes(hardnessKeyword, bytes);
		return bytes == null ? HardnessState.Soft : ( HardnessState )bytes[0];
		}

		return HardnessState.Soft;
		}

		public virtual void Hardness(IItemStack someStack, HardnessState set)
		{
		byte[ ] bytes = new byte[1];
		bytes[0] = ( byte )set;
		someStack.Attributes.SetBytes(hardnessKeyword, bytes);
		}

		public virtual SharpnessState Sharpen(IItemStack someStack)
		{
		if (this.Sharpenable == false) {
		api.World.Logger.VerboseDebug("Can't sharpen! {0}", this.Code);
		return this.Sharpness(someStack);;
		}

		SharpnessState sharp = Sharpness(someStack);

		if (sharp < SharpnessState.Razor) { Sharpness(someStack, ++sharp); }
		//TODO: Play sound effect
		#if DEBUG
		api.World.Logger.VerboseDebug("Sharpness of '{1}' increased to: {0}", sharp, this.Code);
		#endif

		//TODO: If durability exists - decriment based on Hardnes Vs. Wear...
		if (this.Durability > 1) {

		var currentDur = GetDurability(someStack);
		SetDurability(someStack,--currentDur);
		}

		return sharp;				
		}

		public virtual void CopyAttributes(ItemStack donor, ItemStack recipient)
		{
		if (donor.Class == recipient.Class) {
		var hI = (donor.Item as IAmSteel).Hardness(donor);
		var sI = (donor.Item as IAmSteel).Sharpness(donor);

		(recipient.Item as IAmSteel).Hardness(recipient, hI);
		(recipient.Item as IAmSteel).Sharpness(recipient, sI);

		var wear = GetDurability(donor);
		SetDurability(recipient, wear);
		}

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

		return defaultPower + (pctBoost * defaultPower);
		}
		return defaultPower;
		}


		public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
		{
		bool edged = this.Tool.EdgedImpliment( );
		bool weapon = this.Tool.Weapons( ); 		
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

		base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
		}



		public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
		{
		bool edged = this.Tool.EdgedImpliment( );
		bool weapon = this.Tool.Weapons( );
		var targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
		int targetTier = targetBlock.ToolTier;

		//Only called for attacks on BLOCKS / Envrionment. Scen# 5 - 6 here.	
		
		//Weapon & Tool applicability: Is rate > 1 ? then - its sorta OK...ish.

		api.World.Logger.VerboseDebug($"OnAttackingWith:: (Weap:{weapon},Edge:{edged}) {byEntity.Code} -> {targetBlock.Code}");



		return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel);
		}


		public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
		{
		//TODO: too sharp edges...wear down EDGE randomly (extra for 'abuse' )

		//Tool Specific special damage reduction rate: e.g. scythe, hoe, knife, here?

		if (!itemslot.Empty) 
		{
		var hardness = this.Hardness(itemslot.Itemstack);
		switch (hardness) 
				{
				case HardnessState.Soft:

					break;
				case HardnessState.Medium:

					break;
				case HardnessState.Hard:

					break;
				case HardnessState.Brittle:

					break;
				default:
					break;
				}
		}

		base.DamageItem(world, byEntity, itemslot, amount);
		}



		/// <summary>
		/// Advanced formula to calculate wear based on 'sharpness' and 'durability' inherint
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
		bool edgedTool = this.Tool.EdgedImpliment( );
		

		float hardnessMult =((int)HardnessState.Brittle+1) / ((int)this.Hardness(stackInSlot.Itemstack)+1) * 0.25f;
		float wearMax = 1;
		if (edgedTool) {
		wearMax = ( byte )SharpnessState.Razor / ( byte )this.Sharpness(stackInSlot.Itemstack);//5..1
		}

		int actualDmg = ( int )Math.Round(NatFloat.createTri(wearMax, hardnessMult).nextFloat( ), 1);

		#if DEBUG
		api.World.Logger.VerboseDebug($"[{this.Code}] --> Harndess effect: [ Hardness {hardnessMult} Vs. Rate: {wearMax} apply dmg: {actualDmg}, edged: {edgedTool} ]");
		#endif

		stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, actualDmg);
		return;
		}

		base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
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

		if (steelItemSlot != null) {

		if (steelItemSlot.Itemstack.Item is IAmSteel) {
		var steelItem = steelItemSlot.Itemstack.Item;

		api.World.Logger.VerboseDebug("Input (ingredient) Item {0} supports; Steel Interface ", steelItem.Code);

		if (!outputSlot.Empty && outputSlot.Itemstack.Class == EnumItemClass.Item
				&& outputSlot.Itemstack.Item is IAmSteel) {
		var outputItem = outputSlot.Itemstack.Item;
		var fullMetalInterface = outputSlot.Itemstack.Item as IAmSteel;
		api.World.Logger.VerboseDebug("Output Item {0} supports; Steel Interface ", steelItem.Code);

		fullMetalInterface.CopyAttributes(steelItemSlot.Itemstack, outputSlot.Itemstack);

		api.World.Logger.VerboseDebug("Attributes perpetuated from {0} to {1} ", steelItem.Code, outputItem.Code);

		if(sharpenerItemSlot != null) fullMetalInterface.Sharpen(outputSlot.Itemstack);

		//outputSlot.MarkDirty( );
		}
		}

		}

		}




		#endregion

		internal void SetDurability(IItemStack recipient, int wearLevel)
		{
		recipient.Attributes.SetInt(durabilityKeyword, wearLevel);
		}

		internal int GetDurability(IItemStack recipient)
		{
		return recipient.Attributes.GetInt(durabilityKeyword, recipient.Item.Durability);
		}
	}
}

