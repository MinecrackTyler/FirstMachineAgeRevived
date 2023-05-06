using System;
using System.Text;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	public class VariableMetalItem : Item
	{
		private const string default_IngotCode = @"game:ingot-copper";
		private const string default_MetalbitCode = @"metalbit-";
		private const string metalQuantityKey = @"metalQuantity";
		private const string metalIngotCodeKey = @"metalIngotCode";

		/// <summary>
		/// Store Ingot Code (here)
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
		/// Store metal VOXEL quantity value.
		/// </summary>
		/// <returns>The quantity.</returns>
		/// <param name="itemStack">Item stack.</param>
		protected int MetalQuantity(ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return 0;
		return itemStack.Attributes.GetInt(metalQuantityKey, 0);
		}

		protected string MetalName(ItemStack itemStack)
		{
		//TODO: generic 'material' Language entries...
		if (itemStack == null || itemStack.Attributes == null) return @"?";
		var sliced = Lang.GetUnformatted("item-"+MetalIngotCode(itemStack).Path).Split(' ');
		return String.Join(" ", sliced.Take(sliced.Length - 1));
		}

		protected MetalRecoverySystem AnvilMetalRecoveryMod 
		{
			get
			{
				return this.api.ModLoader.GetModSystem<MetalRecoverySystem>( );
			}
		}


		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
		//Set correct material texture from ItemStack attributes
		LoadedTexture texturePlaceholder = new LoadedTexture(capi);
		var ingotCode = MetalIngotCode(itemstack);
		var textureDonatorItem = capi.World.GetItem(ingotCode);
		if (textureDonatorItem != null) {
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
		var metalQuantity = ( int )Math.Floor(MetalQuantity(inSlot.Itemstack) * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue);
		var props = RegenerateCombustablePropsFromStack(inSlot.Itemstack);

		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

		dsc.AppendLine(Lang.Get("fma:itemdesc-item-metal_fragments"));
		dsc.AppendLine(Lang.Get("fma:metal_worth", metalQuantity, metalName));
		}

		public void ApplyMetalProperties(RecoveryEntry recoveryData, ref ItemStack contStack, float percentAdjust = 1.0f)
		{
		contStack.Attributes.SetInt(metalQuantityKey, ( int )(recoveryData.TotalQuantity * percentAdjust));
		contStack.Attributes.SetString(metalIngotCodeKey, recoveryData.PrimaryMaterial.ToString( ));

		RegenerateCombustablePropsFromStack(contStack);
		}

		//Why is this actually done...? whole item isn't smeltable now...
		protected CombustibleProperties RegenerateCombustablePropsFromStack(ItemStack contStack)
		{
		if (contStack == null ) return null;
		//if (contStack.Class == EnumItemClass.Item && contStack.Item.CombustibleProps != null) return contStack.Item.CombustibleProps;

		var metalAssetCode = MetalIngotCode(contStack);
		var metalUnits = MetalQuantity(contStack);

		if (metalAssetCode != null && metalUnits > 0)				
		if (MetalRecoverySystem.MetalProperties.ContainsKey(metalAssetCode.PathEnding()))
		{
		var sourceInfo = MetalRecoverySystem.MetalProperties[metalAssetCode.PathEnding( )];//Mabey more...rustic lookup?

		var aCombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = ( int )sourceInfo.MeltingPoint,
			MeltingDuration = sourceInfo.MeltingDuration,
			//HeatResistance = 500, //sourceInfo.SpecificHeatCapacity & Formula...?
			MaxTemperature = ( int )sourceInfo.BoilingPoint,
			//SmokeLevel = sourceInfo.SmokeLevel,
			SmeltedRatio = 100,
			SmeltedStack = new JsonItemStack( ) { Type = EnumItemClass.Item, Code = metalAssetCode.Clone( ), Quantity = (int)Math.Floor(metalUnits * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue) }
		};
		aCombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_regen", true);
		
		
		
		return aCombustibleProps;
		}
		return null;
		}

		/// <summary>
		/// Bend Crafting output result - to Dynamic  item
		/// </summary>
		/// <returns>The for crafting.</returns>
		/// <param name="inputStack">Input stack.</param>
		/// <param name="gridRecipe">Grid recipe.</param>
		/// <param name="ingredient">Ingredient.</param>
		public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
		{
		#if DEBUG
		api.Logger.VerboseDebug("MatchesForCrafting::'{0}', RecipieName: '{1}', Ing. '{2}'", inputStack.Collectible.Code, gridRecipe.Name, ingredient.Code);
		#endif

		if (inputStack != null
			&& gridRecipe.Name.Domain == this.Code.Domain
			&& inputStack.Class == EnumItemClass.Item
			&& inputStack.Collectible.Code == this.Code
			&& ingredient.Code.Domain == this.Code.Domain
			) 
		{

		if (api.Side.IsServer( )) 
			{
			var metalCode = MetalCode(inputStack);
			var metalUnits = MetalQuantity(inputStack);

			//TODO: Smarter lookup here - allow for 3rd party metals
			Item metalBits = api.World.GetItem(new AssetLocation(GlobalConstants.DefaultDomain, default_MetalbitCode + metalCode));
			//This whole scenario can't work for multi-material items...spawn remainder of items in grid, or on craft?
			if (metalBits != null) 
				{
				gridRecipe.Output.Quantity = ( int )(Math.Round(metalUnits * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue) / 5);
				gridRecipe.Output.Code = metalBits.Code;
				gridRecipe.Output.Resolve(api.World, "VariableMetalItem_crafting");													
				}			
			}
		return true;
		}
		return false;
		}




	}
}

