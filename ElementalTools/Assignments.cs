using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.ServerMods;

namespace ElementalTools
{
	public partial class ElementalToolsSystem : ModSystem
	{
		internal const string malletItemKey = @"ItemMallet";
		internal const string pack_carburizationBlockKey = @"pack_carburization";
		internal const string pack_stateFired = @"fired";
		internal const string malletAssetKey = @"mallet";
		internal const string hammerAssetKey = @"hammer";
		internal const string fmaKey = @"fma";

		internal const string pack_carburizationClassKey = @"PackCarburization";
		internal const string PackCarburizationEntityNameKey = @"PackCarburizationEntity";

		internal const string IronNameKey = @"iron";
		internal const string SteelNameKey = @"steel";//Generic 'steel' of Unknown province...
		internal const string BlisterSteelNameKey = @"blister_steel";//a Crude 'Steel' (layer) made by Carburization - mixed material props
		internal const string ShearSteelNameKey = @"shear_steel";//forge-welded blister steel 
		internal const string MaterialNameKey = @"material";
		internal const string MetalNameKey = @"metal";
		internal const string RecipieWildcard = @"X";


		internal static readonly AssetLocation fired_carburizationPackCode = new AssetLocation(fmaKey, pack_carburizationBlockKey).AppendPaths(pack_stateFired);

		private void RegisterItemClasses( )
		{
		CoreAPI.RegisterItemClass(malletItemKey, typeof(ItemMallet));
		}

		private void RegisterBlockClasses( )
		{
		CoreAPI.RegisterBlockClass(pack_carburizationClassKey, typeof(PackCarburization));
		CoreAPI.RegisterBlockEntityClass(PackCarburizationEntityNameKey, typeof(PackCarburizationEntity));
		}

		private void ManipulateGridRecipies( )
		{
		Mod.Logger.VerboseDebug($"Total GridRecipies: {CoreAPI.World.GridRecipes.Count}");


		var nonCrushingHammerRecipies = from gridRecipie in CoreAPI.World.GridRecipes
							 where gridRecipie.Ingredients.Any(gi => gi.Value.IsTool && gi.Value.Code.BeginsWith(GlobalConstants.DefaultDomain, hammerAssetKey))
							 where gridRecipie.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"nugget") == false
							 where gridRecipie.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"lime") == false
							 select gridRecipie;

		CraftingRecipeIngredient hammerIngredient = new CraftingRecipeIngredient {
			Type = EnumItemClass.Item,
			//Name = "hammer",
			IsTool = true,
			Code = new AssetLocation(GlobalConstants.DefaultDomain, hammerAssetKey),
			Quantity = 1,
			//IsWildCard = false,			
		};

		CraftingRecipeIngredient malletIngredient = new CraftingRecipeIngredient {
			Type = EnumItemClass.Item,
			Name = "mallet",
			IsTool = true,
			Code = new AssetLocation(fmaKey, malletAssetKey),
			Quantity = 1,
			//IsWildCard = false,			
		};

		var results = SingleSwapinReplicas(nonCrushingHammerRecipies, hammerIngredient, malletIngredient);


		Mod.Logger.VerboseDebug($"Added {results} Mallet recipies");


		}

		private void GenerateSteelEquivalentObjects( )
		{
		//For  "blister_steel" Tier 4 steel tools & tool-heads ...ect...made from iron templates
		var toolsEquivalentList = new string[]{
		"axehead",
		"hammerhead",
		"arrowhead",
		"swordblade",
		"scythehead",
		"sawblade",
		"prospectingpickhead",
		"shovelhead",
		"pickaxehead",
		"knifeblade",
		"hoehead",		
		};

		var ironThingsList = from collectee in CoreAPI.World.Collectibles	                                          
							 			where toolsEquivalentList.Any(toolEqv => collectee.Code.BeginsWith(GlobalConstants.DefaultDomain, toolEqv))
                                      	where collectee.IsFerricMetal()						                                        
										select collectee.Code;

		#if DEBUG
		Mod.Logger.VerboseDebug("Found {0} Iron things to clone into '{1}' equivalents", ironThingsList.Count( ), BlisterSteelNameKey);
		#endif

		var result = MetalSwapCloning(ironThingsList, BlisterSteelNameKey);
					
		Mod.Logger.Event("Made {0} patterns of '{1}' (placeholders)", result, BlisterSteelNameKey);		

		}

		private uint SingleSwapinReplicas(IEnumerable<GridRecipe> sourceRecipies, CraftingRecipeIngredient target, CraftingRecipeIngredient replacement)
		{
		uint replicaCount = 0;

		if (sourceRecipies.Any( )) 
		{
		foreach (var recipieToClone in sourceRecipies.ToArray( )) {
		var cloneRecipie = recipieToClone.Clone( );

		cloneRecipie.Name = new AssetLocation(fmaKey, $"clone_{replacement.Code.Path}_{replicaCount}");

		var targetTag = cloneRecipie.Ingredients.FirstOrDefault(gi => gi.Value.Type == target.Type &&
															gi.Value.IsTool == target.IsTool &&
															target.Code.IsChild(gi.Value.Code) &&
															gi.Value.Quantity == target.Quantity
															 );
		if (targetTag.Key != null && targetTag.Value != null) {
		cloneRecipie.Ingredients[targetTag.Key] = replacement;

		cloneRecipie.ResolveIngredients(ServerCore.World);
		ServerCore.RegisterCraftingRecipe(cloneRecipie);
		replicaCount++;
		}
		else Mod.Logger.Warning("Recipe replacement - fails to locate target| {0}", target.Code);		

		}				
		}		
		return replicaCount;
		}

		//before server runphase LoadGame
		private uint MetalSwapCloning(IEnumerable<AssetLocation> sourceAssets, string materialReplacement)
		{
		uint counter = 0;

		var metalTexture = new CompositeTexture(new AssetLocation(fmaKey, MetalNameKey+"/" +materialReplacement));

		foreach (var asset in sourceAssets) {
		var collectable = CoreAPI.World.Collectibles.Find(ci => ci.Code.Equals(asset));
		if (collectable != null) {
		if (collectable.ItemClass == EnumItemClass.Item) {
		var Clonee = CoreAPI.World.GetItem(asset);

		var transmutedAssetLocation = Clonee.TransmuteByVariants(new string[ ] { MaterialNameKey, MetalNameKey }, materialReplacement);
		Clonee.Code = transmutedAssetLocation;
		Clonee.Code.Domain = fmaKey;
		Clonee.ToolTier = 4;
		Clonee.ItemId = 0;
		//SET: Textures, shape, other properties....		
		Clonee.Textures[MaterialNameKey] = metalTexture;
		Clonee.Textures[MetalNameKey] = metalTexture;
							      
		ServerCore.RegisterItem(Clonee.Clone());
		counter++;
		}
		else {
		var Clonee = CoreAPI.World.GetBlock(asset);
		var transmutedAssetLocation = Clonee.TransmuteByVariants(new string[ ] { MaterialNameKey, MetalNameKey }, materialReplacement);
		Clonee.Code = transmutedAssetLocation;
		Clonee.Code.Domain = fmaKey;
		Clonee.ToolTier = 4;		
		Clonee.BlockId = 0;
		//SET: Textures, shape, other properties....
		Clonee.Textures[MaterialNameKey] = metalTexture;
		Clonee.Textures[MetalNameKey] = metalTexture;

		ServerCore.RegisterBlock(Clonee.Clone());
		counter++;
		}
		}
		else Mod.Logger.Warning("Cannot locate asset for cloning: {0}", asset);
					
		}

		return counter;
		}

		//TODO: Recycling assignment of Smeltable properties from all smith/grid/recipe forms...


		private void ReloadGridRecipes( )
		{
		var RecipieLoader = CoreAPI.ModLoader.GetModSystem<RecipeLoader>( );

		Dictionary<AssetLocation, JToken> fmaGridRecipieFiles = CoreAPI.Assets.GetMany<JToken>(Mod.Logger, "recipes/grid",fmaKey);
		uint recipeQuantity = 0;

		foreach (var val in fmaGridRecipieFiles) {
		Mod.Logger.VerboseDebug("G.R: {0} Processing...", val.Key.ToString());
		if (val.Value is JObject) {
		var gridRecipe = val.Value.ToObject<GridRecipe>(val.Key.Domain);
		gridRecipe.Enabled = true;
		RecipieLoader.LoadRecipe(val.Key, gridRecipe );
		recipeQuantity++;
		}
		if (val.Value is JArray) {
		foreach (var token in (val.Value as JArray)) {
		var gridRecipe = token.ToObject<GridRecipe>(val.Key.Domain);

		RecipieLoader.LoadRecipe(val.Key, gridRecipe);
		gridRecipe.Enabled = true;
		recipeQuantity++;
		}
		}
		}

		Mod.Logger.Event("{0} grid recipes re-loaded from {1} files", recipeQuantity, fmaGridRecipieFiles.Count);		
		}
	}
}

/**** Terminology *************
 * Wrought Iron  -> Blister Steel [Pack carburization / Cementation ]
 * Blister Steel -> Shear Steel [Smithing (Welding) ]
 * Shear Steel -> Cast Steel [ Bessemer process / Open-hearth /.... ] 
 * Pig Iron -> Cast Iron [ Blast furnace / .... ]
 * Cast Iron -> Steel-clad Cast Iron [ "fining" furnace; Decarburization, re-heat in air @900C]
 * https://www.tf.uni-kiel.de/matwis/amat/iss/kap_a/backbone/ra_2_3.html
 * Benjamin Huntsman's invention of the crucible steel process
 * 
 "blister_steel" Tier 4 steel:
TODO: Code to allow anvil to handle welding split blister rods/cards back into ONE consolidated ingot of 'steel'


attributes: {
	outputOverride: "fma:zzzzzzz"
},	




 ******************************/
