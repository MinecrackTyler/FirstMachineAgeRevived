using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace AnvilMetalRecovery;

public class VariableMetalItem : Item
{
	private const string default_IngotCode = @"game:ingot-copper";
	private const string default_MetalbitCode = @"metalbit-";
	private const string metalQuantityKey = @"metalQuantity";
	private const string metalIngotCodeKey = @"metalIngotCode";

	protected MetalRecoverySystem AnvilMetalRecoveryMod => api.ModLoader.GetModSystem<MetalRecoverySystem>();

	/// <summary>
	///     Store Ingot Code (here)
	/// </summary>
	/// <returns>The code.</returns>
	/// <param name="itemStack">Item stack.</param>
	/// <remarks>If base metal still exists - can still smelt...</remarks>
	protected AssetLocation MetalIngotCode(ItemStack itemStack)
	{
		if (itemStack == null || itemStack.Attributes == null) return new AssetLocation(default_IngotCode);
		return new AssetLocation(itemStack.Attributes.GetString(metalIngotCodeKey, default_IngotCode));
	}

	protected string MetalCode(ItemStack itemStack)
	{
		if (itemStack == null || itemStack.Attributes == null) return string.Empty;
		return itemStack.Attributes.GetString(metalIngotCodeKey).Split('-').Last();
	}

	/// <summary>
	///     Store metal VOXEL quantity value.
	/// </summary>
	/// <returns>The quantity.</returns>
	/// <param name="itemStack">Item stack.</param>
	protected int MetalQuantity(ItemStack itemStack)
	{
		if (itemStack == null || itemStack.Attributes == null) return 0;
		return itemStack.Attributes.GetInt(metalQuantityKey);
	}

	protected string MetalName(ItemStack itemStack)
	{
		//TODO: generic 'material' Language entries...
		if (itemStack == null || itemStack.Attributes == null) return @"?";
		var sliced = Lang.GetUnformatted("item-" + MetalIngotCode(itemStack).Path).Split(' ');
		return string.Join(" ", sliced.Take(sliced.Length - 1));
	}


	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target,
		ref ItemRenderInfo renderinfo)
	{
		//Set correct material texture from ItemStack attributes
		var texturePlaceholder = new LoadedTexture(capi);
		var ingotCode = MetalIngotCode(itemstack);
		var textureDonatorItem = capi.World.GetItem(ingotCode);
		if (textureDonatorItem != null)
		{
			var newTexture = textureDonatorItem.FirstTexture.Base.WithPathAppendixOnce(".png");

			//#if DEBUG
			//capi.Logger.VerboseDebug("VariableMetalItem Txr: {0}", newTexture);
			//#endif			                  

			capi.Render.GetOrLoadTexture(newTexture, ref texturePlaceholder);

			renderinfo.TextureId = texturePlaceholder.TextureId;
		}
		//Cache TextId# on TempAttributes ?
	}


	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		var metalName = MetalName(inSlot.Itemstack);
		var metalQuantity = (int)Math.Floor(MetalQuantity(inSlot.Itemstack) *
		                                    AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue);

		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

		dsc.AppendLine(Lang.Get("fma:itemdesc-item-metal_fragments"));
		dsc.AppendLine(Lang.Get("fma:metal_worth", metalQuantity, metalName));
	}

	public void ApplyMetalProperties(RecoveryEntry recoveryData, ref ItemStack contStack, float percentAdjust = 1.0f)
	{
		contStack.Attributes.SetInt(metalQuantityKey, (int)(recoveryData.TotalQuantity * percentAdjust));
		contStack.Attributes.SetString(metalIngotCodeKey, recoveryData.PrimaryMaterial.ToString());
	}


	/// <summary>
	///     Bend Crafting output result - to Dynamic  item
	/// </summary>
	/// <returns>The for crafting.</returns>
	/// <param name="inputStack">Input stack.</param>
	/// <param name="recipeBase">Grid recipe.</param>
	/// <param name="ingredient">Ingredient.</param>
	public override bool MatchesForCrafting(ItemStack inputStack, IRecipeBase recipeBase, IRecipeIngredient ingredient)
	{
#if DEBUG
		api.Logger.VerboseDebug("MatchesForCrafting::'{0}', RecipieName: '{1}', Ing. '{2}'",
			inputStack.Collectible.Code, recipeBase.Name, ingredient.Code);
#endif

		if (recipeBase is not GridRecipe gridRecipe || ingredient is not CraftingRecipeIngredient craftingRecipeIngredient ) return false;
		
		if (inputStack != null
		    && gridRecipe.Name.Domain == Code.Domain
		    && inputStack.Class == EnumItemClass.Item
		    && inputStack.Collectible.Code == Code
		    && craftingRecipeIngredient.Code.Domain == Code.Domain
		    && gridRecipe.Output != null
		   )
		{
			if (api.Side.IsServer())
			{
				var metalCode = MetalCode(inputStack);
				var metalUnits = MetalQuantity(inputStack);

				//TODO: Smarter lookup here - allow for 3rd party metals
				var metalBits =
					api.World.GetItem(
						new AssetLocation(GlobalConstants.DefaultDomain, default_MetalbitCode + metalCode));
				//This whole scenario can't work for multi-material items...spawn remainder of items in grid, or on craft?
				if (metalBits != null)
				{
					gridRecipe.Output.Quantity =
						(int)(Math.Round(metalUnits * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue) /
						      5);
					gridRecipe.Output.Code = metalBits.Code;
					gridRecipe.Output.Resolve(api.World, "VariableMetalItem_crafting");
				}
			}

			return true;
		}

		return false;
	}
}