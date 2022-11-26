using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading;

using Newtonsoft.Json.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace ElementalTools
{
	public partial class ElementalToolsSystem : ModSystem
	{
		internal const string malletItemKey = @"ItemMallet";
		internal const string sharpeningStoneItemKey = @"ItemSharpening_stone";
		internal const string SteelPrefix = @"Steel";//Generic 'steel' of Unknown province...
		internal const string consolidatableCardsKey = @"ConsolidatableCards";

		internal const string pack_carburizationBlockKey = @"pack_carburization";
		internal const string pack_stateFired = @"fired";
		internal const string malletAssetKey = @"mallet";
		internal const string hammerAssetKey = @"hammer";
		internal const string fmaKey = @"fma";
		internal const string sharpeningStoneAssetKey = @"sharpening_stone";
		internal const string sharpeningRecipiePrefix = @"steel_sharpening_";

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
		CoreAPI.RegisterItemClass(sharpeningStoneItemKey, typeof(ItemSharpeningStone));

		CoreAPI.RegisterItemClass(consolidatableCardsKey, typeof(ItemConsolidatableCards));
		
		//stock VS steel'ified classes
		CoreAPI.RegisterItemClass(@"Steel_Item", typeof(GenericSteelItem));
		
		//Steel Wrapped ItemCores.
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<Item>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemSword>), SteelPrefix );
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemChisel>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemAxe>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemSpear>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemCleaver>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemHammer>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemHoe>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemKnife>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass( typeof(SteelWrapItem<ItemProspectingPick>), SteelPrefix);		
		//CoreAPI.NamePrefixed_RegisterItemClass(typeof(SteelWrapItem<ItemScythe>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass(typeof(SteelWrapItem<ItemShears>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass(typeof(SteelWrap<ItemSaw>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass(typeof(SteelWrap<ItemShovel>), SteelPrefix);
		//CoreAPI.NamePrefixed_RegisterItemClass(typeof(SteelWrap<ItemArrow>), SteelPrefix);//Merging of Steel ammo - TODO!

		}



		private void RegisterBlockClasses( )
		{
		CoreAPI.RegisterBlockClass(pack_carburizationClassKey, typeof(PackCarburization));
		CoreAPI.RegisterBlockEntityClass(PackCarburizationEntityNameKey, typeof(PackCarburizationEntity));

		}



		private void MalletInsertion( )
		{
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
		Mod.Logger.Event($"Added {results} Mallet recipies");
		}


		private void GenerateSharpeningGridRecipies( )
		{
		var sharpenableThings = new string[ ]{
		//Tool-heads
		"axehead-*",				
		"arrowhead-*",
		"swordblade-*",		
		"scythehead-*",
		"sawblade-*",
		"prospectingpickhead-*",		
		"pickaxehead-*",
		"knifeblade-*",
		"hoehead-*",
		
		//Finished Tools
		"axe-*",
		"saw-*",
		"pickaxe-*",
		"prospectingpick-*",
		"sword-*",
		"knife-*",
		"spear-*",
		"scythe-*",
		"hoe-*",
		"chisel-*",
		};

		var variants = new string[ ]
		{
		BlisterSteelNameKey,
		ShearSteelNameKey,
		};

		GridRecipe sharpeningPattern = new GridRecipe( ) {
			Enabled = true,
			Height = 3,
			Width = 1,
			Shapeless = false,
			Name = new AssetLocation(fmaKey, sharpeningRecipiePrefix),//Automatic ## appended...
			IngredientPattern = "H\tL\tS",
				Ingredients = new Dictionary<string, CraftingRecipeIngredient>( )
				{
					{"H", new CraftingRecipeIngredient()
						{
							Type = EnumItemClass.Item,
							Code = new AssetLocation(fmaKey,"#"),
							IsWildCard = true,
							AllowedVariants = variants,
							Name = MetalNameKey,
							Quantity = 1,
						}
					},
					{
					"L",  new CraftingRecipeIngredient()
						{
							Type = EnumItemClass.Item,
							Code = new AssetLocation(GlobalConstants.DefaultDomain,"fat"),//Consider; Alternate-Lubricants: Grease, Veg-Oils
							Quantity = 1,
						}
					},
					{
					"S", new CraftingRecipeIngredient()
						{
							Type = EnumItemClass.Item,
							IsTool = true,
							Code = new AssetLocation(fmaKey,sharpeningStoneAssetKey),
							Quantity = 1,
						}
					}
				},
				Output = new CraftingRecipeIngredient( ) 
				{
					Type = EnumItemClass.Item,
					Quantity = 1,
					Code = new AssetLocation(fmaKey, @"#"),//Code//Cloned from 'H' - with Wildcard ending {metal}
					//IsWildCard = true,

				},

		};

		var results = SingleVariableToolRecipies(sharpenableThings, sharpeningPattern,'H', MetalNameKey);
		Mod.Logger.Event($"Added {results} Sharpening recipes, for {sharpenableThings.Count()} items");

		}

		private void GenerateSteelToolEquivalentGridRecipies( )//Revisit!
		{
		uint results = 0;
		var ironTools = new string[ ]{
		//Finished Tools
		"axe",
		"saw",
		"knife",
		"chisel",
		"hammer",
		};

		var variants = new string[ ]
		{
		BlisterSteelNameKey,
		ShearSteelNameKey,
		};

		foreach (var toolName in ironTools) {
		var ironToolRecipieSet = from gridRecipie in CoreAPI.World.GridRecipes
                                where gridRecipie.Ingredients.Any(gi => gi.Value.IsTool && gi.Value.Code.BeginsWith(GlobalConstants.DefaultDomain, toolName))								 
								 where gridRecipie.Output.IsTool == false
								 select gridRecipie;

		CraftingRecipeIngredient ironToolIngredient = new CraftingRecipeIngredient {
			Type = EnumItemClass.Item,
			//Name = "hammer",
			IsTool = true,
			Code = new AssetLocation(GlobalConstants.DefaultDomain, toolName).WithPathAppendix("-"+IronNameKey),// game:knife-iron
			Quantity = 1,
			//IsWildCard = false,			
		};

		CraftingRecipeIngredient steelToolIngredient = new CraftingRecipeIngredient {
			Type = EnumItemClass.Item,
			Name = toolName,
			IsTool = true,
			Code = new AssetLocation(fmaKey, toolName+"-*"),
			Quantity = 1,
			AllowedVariants = variants,
			IsWildCard = true, //?
		};

		results += SingleSwapinReplicas(ironToolRecipieSet, ironToolIngredient, steelToolIngredient);
		}

		
		Mod.Logger.Event($"Added {results} (steely) grid recipes, from {ironTools.Count( )} Steel tools");
		}

		/// <summary>
		/// Permutate the variant tool recipies.
		/// </summary>
		/// <returns>Count created </returns>
		/// <param name="targetsNames">Targets names. (variants preset!)</param>
		/// <param name="generalPattern">General pattern (tool predefined!).</param>
		/// <param name="outputCloneKey">Clone key (ouput)</param>
		internal uint SingleVariableToolRecipies(IEnumerable<string> targetsNames, GridRecipe generalPattern, char outputCloneKey, string remapName = "" )
		{
		uint recipieCount = 0;
					
		foreach (var name in targetsNames) 
		{
		var editRecipe = generalPattern.Clone( );
		editRecipe.Name = editRecipe.Name.WithPathAppendix(recipieCount.ToString("D"));
		CraftingRecipeIngredient thingToClone;
		if (editRecipe.Ingredients.TryGetValue(outputCloneKey.ToString(), out thingToClone))
		{
		string remapedOutput = name.Replace("*", "{"+remapName+"}");// "-{remapName}" instead of "*"
		thingToClone.Code = new AssetLocation(fmaKey, name);
		editRecipe.Output = new CraftingRecipeIngredient( ) {
			Type = thingToClone.Type,
			Quantity = thingToClone.Quantity,
			Code = new AssetLocation(fmaKey, remapedOutput),
			//IsWildCard = true,	//No...
		};

		//editRecipe.ResolveIngredients(ServerCore.World);
		LoaderOfRecipies.LoadRecipe(new AssetLocation(fmaKey, "singletoolvar_"+recipieCount.ToString("D")), editRecipe);
		recipieCount++;
		}

		}


		return recipieCount;
		}

		internal uint SingleSwapinReplicas(IEnumerable<GridRecipe> sourceRecipies, CraftingRecipeIngredient target, CraftingRecipeIngredient replacement)
		{
		uint replicaCount = 0;

		if (sourceRecipies.Any( )) 
		{
		foreach (var recipieToClone in sourceRecipies.ToArray( )) {
		var cloneRecipie = recipieToClone.Clone( );

		cloneRecipie.Name = new AssetLocation(fmaKey, $"clone_{replacement.Code.Path}_{replicaCount}");

		var targetTag = cloneRecipie.Ingredients.FirstOrDefault(gi => gi.Value.Type == target.Type &&
															gi.Value.IsTool == target.IsTool &&
															target.Code.IsDerivedFrom(gi.Value.Code) &&
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

		internal uint CloneEntityClasses( )
		{
		uint counter = 0;
		//BUG DODGER: Duplicate FMA:spear-blah projectiles into domain: 'game'...
		var fmaThings = CoreAPI.World.EntityTypes.Where(thg => thg.Code.Domain.Equals(fmaKey, StringComparison.Ordinal));
		foreach (var thing in fmaThings) {
		Mod.Logger.VerboseDebug("found EntityType; Code[{0}] ", thing.Code);
		var clone = thing.Clone( );
		clone.Code.Domain = GlobalConstants.DefaultDomain;
		CoreAPI.World.EntityTypes.Add(clone);
		Mod.Logger.VerboseDebug("Registering clone of EntityProperties; Code[{0}]:[{1}]", clone.Code, clone.Class);
		//RegisterEntityClass
		CoreAPI.RegisterEntityClass(clone.Class, clone);
		counter++;
		}
		return counter;
		}

	}
}

/**** Terminology *************
 * Wrought Iron  -> Blister Steel [Pack carburization / Cementation ]
 * Blister Steel -> Shear Steel [Smithing (Welding/Consolidation) ]
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

* Item: Sickle - 2x2 hand-held harvest tool (works on reeds, crops, grass, any plants)
* Item: Mattock - Half Pickaxe, Half Shovel; All-Tool.
* Item: Seax - larger stabbier Knife, thats still a knife.
* Sort out how Arrow's work with item merging...

 ******************************/
