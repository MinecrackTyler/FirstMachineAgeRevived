using System;
using System.Text;
using System.Linq;


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
		dsc.AppendFormat("Edge: {0}\n", Sharpness(inSlot.Itemstack) );	
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

		public void CopyAttributes(ItemStack donor, ItemStack recipient)
		{
		if (donor.Class == recipient.Class) {
		var hI = (donor.Item as IAmSteel).Hardness(donor);
		var sI = (donor.Item as IAmSteel).Sharpness(donor);

		(recipient.Item as IAmSteel).Hardness(recipient, hI);
		(recipient.Item as IAmSteel).Sharpness(recipient, sI);
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

		public override void DamageItem(IWorldAccessor world, Vintagestory.API.Common.Entities.Entity byEntity, ItemSlot itemslot, int amount = 1)
		{
		//TODO: Effects from Tempering and edge Wear...also too sharp edges...

		base.DamageItem(world, byEntity, itemslot, amount);
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


		if (steelItemSlot != null) {

		if (steelItemSlot.Itemstack.Item is IAmSteel) {
		var steelItem = steelItemSlot.Itemstack.Item;

		api.World.Logger.VerboseDebug("Input (ingredient) Item {0} supports; Steel Interface ", steelItem.Code);

		if (!outputSlot.Empty && outputSlot.Itemstack.Class == EnumItemClass.Item
				&& outputSlot.Itemstack.Item is IAmSteel) {
		var outputItem = outputSlot.Itemstack.Item;
		var OutputSteelInterface = outputSlot.Itemstack.Item as IAmSteel;
		api.World.Logger.VerboseDebug("Output Item {0} supports; Steel Interface ", steelItem.Code);

		OutputSteelInterface.CopyAttributes(steelItemSlot.Itemstack, outputSlot.Itemstack);

		api.World.Logger.VerboseDebug("Attributes perpetuated from {0} to {1} ", steelItem.Code, outputItem.Code);
		}

		}

		}
		}

		#endregion
	}
}

