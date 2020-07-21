using System;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

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



		/*
		public virtual float GetAttackPower (IItemStack withItemStack)
		public virtual float GetDurability (IItemStack itemstack) //Leave unchanged - it never increases...
		public virtual float GetMiningSpeed (IItemStack itemstack, Block block)
		public virtual void DamageItem (IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
		public virtual void OnConsumedByCrafting (ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		public virtual void OnHeldDropped (IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)

		public int MiningTier  // public int ToolTier;

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

		dsc.AppendFormat("Metal: '{0}', ",Name);

		if (Hardenable) {
		dsc.AppendFormat("Temper: {0}\n", Hardness(inSlot.Itemstack) );
		}

		if (Sharpenable) {
		dsc.AppendFormat("Wear: {0}\n", Sharpness(inSlot.Itemstack) );	
		}

		}

		public virtual SharpnessState Sharpness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(sharpnessKeyword)) {
		return someStack.Attributes.GetEnum<SharpnessState>(sharpnessKeyword, SharpnessState.Rough);
		}
		return SharpnessState.Dull;		
		}

		public virtual void Sharpness(IItemStack someStack, SharpnessState set)
		{
		someStack.Attributes.SetEnum<SharpnessState>(sharpnessKeyword, set);
		}

		public virtual HardnessState Hardness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(sharpnessKeyword)) {
		return someStack.Attributes.GetEnum<HardnessState>(sharpnessKeyword, HardnessState.Soft);
		}
		return HardnessState.Soft;
		}

		public virtual void Hardness(IItemStack someStack, HardnessState set)
		{
		someStack.Attributes.SetEnum<HardnessState>(hardnessKeyword, set);
		}

		public virtual SharpnessState Sharpen(IItemStack someStack)
		{			
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(sharpnessKeyword)) {
		var sharp = someStack.Attributes.GetEnum<SharpnessState>(sharpnessKeyword, SharpnessState.Rough);

		if (sharp < SharpnessState.Razor) { someStack.Attributes.SetEnum<SharpnessState>(sharpnessKeyword, sharp++); }

		#if DEBUG
		api.World.Logger.VerboseDebug("{1} :: Sharpness increased to: {0}", sharp, this.Code);
		#endif

		return sharp;		
		}

		return SharpnessState.Rough;
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

		//TODO: OnCrafting - Translate properties from 'parent' steel item/block!

		#endregion
	}
}

