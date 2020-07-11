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
		internal const string malletItemKey = @"ItemMallet";
		internal const string pack_carburizationBlockKey = @"pack_carburization";
		internal const string pack_stateFired = @"fired";
		internal const string malletAssetKey = @"mallet";
		internal const string hammerAssetKey = @"hammer";
		internal const string fmaKey = @"fma";

		internal const string pack_carburizationClassKey = @"PackCarburization";
		internal const string pack_carburizationBEKey = @"PackCarburizationEntity";
		internal const string PackCarburizationEntityNameKey = @"PackCarburizationEntity";

		internal const string IronNameKey = @"iron";
		internal const string MaterialNameKey = @"metal";
		internal const string MetalNameKey = @"material";
		internal static readonly AssetLocation fired_carburizationPackCode = new AssetLocation(fmaKey, pack_carburizationBlockKey).AppendPaths(pack_stateFired);

		private void RegisterItemClasses( )
		{
		CoreAPI.RegisterItemClass(malletItemKey, typeof(ItemMallet));
		}

		private void RegisterBlockClasses( )
		{
		CoreAPI.RegisterBlockClass(pack_carburizationClassKey, typeof(PackCarburization));
		CoreAPI.RegisterBlockEntityClass(pack_carburizationBEKey, typeof(PackCarburizationEntity));
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
		//TODO: Recycling assignment of Smeltable properties from all smith/grid/recipe forms...
	}
}
