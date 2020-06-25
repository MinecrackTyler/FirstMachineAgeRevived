using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ElementalTools
{
	public class ItemMallet : Item
	{
		public ItemMallet( )
		{
		}

		/// <summary>
		/// Ons the consumed by crafting.
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
		int effectiveTier = 1;


		foreach (var itemSlot in allInputSlots) {
		if (itemSlot.Empty) continue;
		if ( itemSlot.Itemstack.Class == EnumItemClass.Block) {
		Block ingBlock = itemSlot.Itemstack.Block;
		effectiveTier = Math.Max(ingBlock.RequiredMiningTier, effectiveTier);
		}
		else {
		Item ingItem = itemSlot.Itemstack.Item;
		if (ingItem.Tool.HasValue) continue;
		effectiveTier = Math.Max(ingItem.ToolTier, effectiveTier);
		}
		}

		float burnRate = (effectiveTier / this.ToolTier);
						
		int actualDmg = ( int )Math.Round(NatFloat.createTri(effectiveTier, burnRate).nextFloat( ), 1);

		#if DEBUG
		api.World.Logger.VerboseDebug("Variable wear rate [ ToolTier:{0} VS {1}, BurnRate: {2} - apply dmg: {3} ]", this.ToolTier, effectiveTier, burnRate, actualDmg);
		#endif

		stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, actualDmg);
		return;
		}

		base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
		}


	}
}

