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



		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		var metalName = MetalName(inSlot.Itemstack);
		var metalQuantity = ( int )Math.Floor(MetalQuantity(inSlot.Itemstack) * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue);
		var props = RegenerateCombustablePropsFromStack(inSlot.Itemstack);

		if (props  != null && props.MeltingPoint > 0) {		
		dsc.AppendLine(Lang.Get("game:smeltpoint-smelt", props.MeltingPoint));
		}
		dsc.AppendLine(Lang.Get("fma:metal_worth", metalQuantity, metalName));
		}


		//TODO: Merge - to the New metal V.S. stock metal bits...?
		//TryMergeStacks ???
		//virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot) //?
		//virtual void OnModifiedInInventorySlot //Only for new-Inserts (?)

		public void ApplyMetalProperties(RecoveryEntry recoveryData, ref ItemStack contStack)
		{
		contStack.Attributes.SetInt(metalQuantityKey, ( int )recoveryData.Quantity);
		contStack.Attributes.SetString(metalIngotCodeKey, recoveryData.IngotCode.ToString( ));

		RegenerateCombustablePropsFromStack(contStack);
		}

		protected CombustibleProperties RegenerateCombustablePropsFromStack(ItemStack contStack)
		{
		if (contStack == null ) return null;
		//if (contStack.Class == EnumItemClass.Item && contStack.Item.CombustibleProps != null) return contStack.Item.CombustibleProps;

		var metalCode = MetalIngotCode(contStack);
		var metalUnits = MetalQuantity(contStack);

		if (metalCode != null || metalUnits > 0)
		{
		var sourceMetalItem = api.World.GetItem(metalCode);

		if (sourceMetalItem == null || sourceMetalItem.IsMissing || sourceMetalItem.CombustibleProps == null) return null;
		
		var aCombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = sourceMetalItem.CombustibleProps.MeltingPoint,
			MeltingDuration = sourceMetalItem.CombustibleProps.MeltingDuration,
			HeatResistance = sourceMetalItem.CombustibleProps.HeatResistance,
			MaxTemperature = sourceMetalItem.CombustibleProps.MaxTemperature,
			SmokeLevel = sourceMetalItem.CombustibleProps.SmokeLevel,
			SmeltedRatio = 100,
			SmeltedStack = new JsonItemStack( ) { Type = EnumItemClass.Item, Code = sourceMetalItem.Code.Clone( ), Quantity = (int)Math.Floor(metalUnits * AnvilMetalRecoveryMod.CachedConfiguration.VoxelEquivalentValue) }
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

			Item metalBits = api.World.GetItem(new AssetLocation(GlobalConstants.DefaultDomain, @"metalbit-" + metalCode));
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

