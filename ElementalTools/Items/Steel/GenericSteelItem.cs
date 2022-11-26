using System;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ElementalTools
{
	public class GenericSteelItem : Item, ISteelBase  //SteelAssist
	{
		#region ISteelBase

		public string BaseMetalName {
			get
			{
			return this.Attributes[SteelAspects.metalNameKeyword].AsString("?");
			}
		}

		public bool Sharpenable {
			get
			{
			return this.Attributes[SteelAspects.sharpenableKeyword].AsBool(false);
			}
		}

		public bool Hardenable {
			get
			{
			return this.Attributes[SteelAspects.hardenableKeyword].AsBool(false);
			}
		}
		#endregion

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
		/// For; Quench-hardening...
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
		return SteelAspects.AttackPower(this, withItemStack, this.api);
		}

		public override float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
		{
		return SteelAspects.MiningSpeed(itemstack, blockSel, block, forPlayer, this.api);		
		}

		public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
		{
		SteelAspects.WhenUsedInAttack(world, byEntity,attackedEntity,itemslot, api );
		}




		public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
		{
		if (api.Side.IsClient( )) return true;

		SteelAspects.WhenUsedForBlockBreak(world, byEntity, itemslot, blockSel, this.api);
		
		return true;//Blocks Behavior overrides?
		}





		public override bool ConsumeCraftingIngredients(ItemSlot[ ] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
		{				
		SteelAspects.ToolInRecipeUse(this, slots, matchingRecipe, api );
		
		return true;//Always as its a tool?
		}




		//OnCreated By Crafting:  Copy properties from 'parent' to steel item/block, for Sharpening effect
		public override void OnCreatedByCrafting(ItemSlot[ ] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
		{
		SteelAspects.SharpenOneSteelItem(allInputslots, outputSlot, byRecipe, api);

		

		}



		public override int GetItemDamageColor(ItemStack itemstack)
		{
		var steelThing = itemstack.AsSteelThing( );		

		switch (steelThing.Sharpness) {
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



	}
}

