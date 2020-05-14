using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElementalTools
{
	public class ItemMallet : Item
	{
		public ItemMallet( )
		{
		}

		/// <summary>
		/// Mallet is a Hammer compatible replacement for many of the same uses, most Crafstmen can't tell the difference.
		/// Useless for smith work...
		/// </summary>
		/// <param name="inputStack"></param>
		/// <param name="gridRecipe"></param>
		/// <param name="ingredient"></param>
		/// <returns></returns>
		public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
		{
		api.World.Logger.VerboseDebug($"{gridRecipe.Name} : {ingredient.Code} ({ingredient.Name})");
		if (gridRecipe.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"nugget")||
		gridRecipe.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"lime")) {
		//It don't *DO* rock crushing.
		return false;
		}

		if (ingredient.IsTool && 
			(ingredient.Code.BeginsWith(GlobalConstants.DefaultDomain, @"hammer") ||
			 ingredient.Code.BeginsWith("fma",ElementalToolsSystem.malletAssetKey))) 
		{
		return true;
		}

		return false;
		}

		/// <summary>
		/// Should return true if thisStack is a satisfactory replacement of otherStack. 
		/// It's bascially an Equals() test, but it ignores any additional attributes that exist in otherStack
		/// </summary>
		/// <param name="thisStack"></param>
		/// <param name="otherStack"></param>
		/// <returns></returns>
		//public override bool Satisfies(ItemStack thisStack, ItemStack otherStack)
		//{
		//return base.Satisfies(thisStack, otherStack);
		//}

		/// <summary>
		/// Damages the item.
		/// </summary>
		/// <returns>The item.</returns>
		/// <param name="world">World.</param>
		/// <param name="byEntity">By entity.</param>
		/// <param name="itemslot">Itemslot.</param>
		/// <param name="amount">Amount.</param>
		public override void DamageItem(IWorldAccessor world, Vintagestory.API.Common.Entities.Entity byEntity, ItemSlot itemslot, int amount = 1)
		{
		//Tweak Numbers...when chiseling
		base.DamageItem(world, byEntity, itemslot, amount);

		}

	}
}

