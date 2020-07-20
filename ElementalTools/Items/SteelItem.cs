using System;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ElementalTools
{
	/// <summary>
	/// GENERIC Steel item. (Tool / Weapon / Armor...anything) [Possibly: Temperable and/or Hardenable ]
	/// </summary>
	public class SteelItem<T>: Item, IAmSteel where T : Item
	{
		internal const string hardenableKeyword = @"hardenable";
		internal const string sharpenableKeyword = @"sharpenable";
		internal const string metalNameKeyword = @"metalName";

		internal const string hardnessKeyword = @"hardness";
		internal const string sharpnessKeyword = @"sharpness";



		/*
		public virtual float GetAttackPower (IItemStack withItemStack)
		public virtual float GetDurability (IItemStack itemstack)
		public virtual float GetMiningSpeed (IItemStack itemstack, Block block)
		public virtual void DamageItem (IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
		public virtual void OnConsumedByCrafting (ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		public virtual void OnHeldDropped (IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)

		public int MiningTier  // public int ToolTier;

		public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		 * */

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

		#region OVER_RIDES

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		dsc.AppendFormat("Metal: '{0}', ",Name);

		if (Hardenable) {
		dsc.AppendFormat("Temper: {0}\n", Hardness(inSlot.Itemstack) );
		}

		if (Sharpenable) {
		dsc.AppendFormat("Wear: {0}\n", Sharpness(inSlot.Itemstack) );	
		}

		}

		public virtual SharpnessState Sharpness(ItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(sharpnessKeyword)) {
		return someStack.Attributes.GetEnum<SharpnessState>(sharpnessKeyword, SharpnessState.Dull);
		}
		return SharpnessState.Dull;		
		}

		public virtual void Sharpness(ItemStack someStack, SharpnessState set)
		{

		}

		public virtual HardnessState Hardness(ItemStack someStack)
		{

		}

		public virtual void Hardness(ItemStack someStack, HardnessState set)
		{

		}

		#endregion

	}
}

