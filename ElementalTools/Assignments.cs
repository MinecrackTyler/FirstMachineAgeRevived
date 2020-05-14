using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading;

using Vintagestory.API.Common;
using Vintagestory.API.Config;




namespace ElementalTools
{
	public partial class ElementalToolsSystem : ModSystem
	{
		internal const string anvilKey = @"Anvil";
		internal const string malletItemKey = @"ItemMallet";
		internal const string malletAssetKey = @"mallet";

		private void RegisterItemClasses()
		{
			CoreAPI.RegisterItemClass(malletItemKey, typeof(ItemMallet));
		}


		private void ManipulateGridRecipies( )
		{
		uint malletizedCount = 0;
		//Thread.Sleep(1000);
		Mod.Logger.VerboseDebug($"Total GridRecipies: {CoreAPI.World.GridRecipes.Count}");

		/*
	var alternateQuery = from gridRecipie in CoreAPI.World.GridRecipes
						where gridRecipie.Ingredients.Any(gi => gi.Value.IsTool && gi.Value.Code.BeginsWith(GlobalConstants.DefaultDomain, @"hammer"))
						where gridRecipie.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"nugget") == false
						where gridRecipie.Output.Code.BeginsWith(GlobalConstants.DefaultDomain, @"lime") == false                            
						select gridRecipie;

	Mod.Logger.VerboseDebug($"Found {alternateQuery.Count()} Recipies using Hammer, (non ore)");

	if (alternateQuery.Any()) {
		foreach (var recipieToClone in alternateQuery.ToArray()) {
		var cloneRecipie = recipieToClone.Clone( );
		var hammerIngredient = cloneRecipie.Ingredients.First(gi => gi.Value.IsTool && gi.Value.Code.BeginsWith(GlobalConstants.DefaultDomain, @"hammer"));

		CraftingRecipeIngredient malletIngredient = new CraftingRecipeIngredient {
		Type = EnumItemClass.Item,
		IsTool = true,
		Code = new AssetLocation(@"fma", malletAssetKey),
		IsWildCard = false,
		//Name = "mallet",
		};
		cloneRecipie.Ingredients[hammerIngredient.Key] = malletIngredient;
		ServerCore.RegisterCraftingRecipe(cloneRecipie);
		malletizedCount++;
		}

	Mod.Logger.VerboseDebug($"Added {malletizedCount} Mallet recipies");
	}
	*/

		GridRecipe testRecipie = new GridRecipe {
			IngredientPattern = "M\tF",
			Name = new AssetLocation("fma","LogToSticks"),
			Height = 2,
			Width = 1,
			Ingredients =
				new Dictionary<string, CraftingRecipeIngredient>{
					{"M", new CraftingRecipeIngredient{ 
						Name = "mallet",
						Type = EnumItemClass.Item,
						Code = new AssetLocation("fma",malletAssetKey),
						IsTool = true,						
						Quantity = 1,} 
					},
					{"F", new CraftingRecipeIngredient{ 
						Name = "wood",
						Type = EnumItemClass.Item,
						Code = new AssetLocation(GlobalConstants.DefaultDomain,"firewood"),												
						Quantity = 1,}
					},
				},
				Output = new CraftingRecipeIngredient{ 
					Type = EnumItemClass.Item,
					Quantity = 3,
					Code = new AssetLocation(GlobalConstants.DefaultDomain,"stick"),
				},
		};//Needs: ResolvedItemstack <- for Non-wildcard !!!!
		testRecipie.ResolveIngredients(ServerCore.World);

		ServerCore.RegisterCraftingRecipe(testRecipie);
		}

		//TODO: Recycling assignment of Smeltable properties from all smith/grid/recipe forms...


		internal void GenerateMetalShavingsItems( )
		{
		//TODO: Automatic Generation of Item 'metal_shaving' by metal & alloy list at RUNTIME
		var genericShaving = ServerAPI.World.ClassRegistry.CreateItem("metal_shaving");
		//genericShaving.CombustibleProps.

		var metalProperties = new Dictionary<AssetLocation, MetalProperty>( );

		foreach (var entry in ServerAPI.Assets.GetMany<MetalProperty>(Mod.Logger, "worldproperties/")) {
		AssetLocation loc = entry.Key.Clone( );
		loc.Path = loc.Path.Replace("worldproperties/", "");
		loc.RemoveEnding( );

		entry.Value.Code.Domain = entry.Key.Domain;

		metalProperties.Add(loc, entry.Value);

		}
		}


	}
}

