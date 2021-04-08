using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace AnvilMetalRecovery
{
	public partial class MetalRecoverySystem : ModSystem
	{				
		internal const string anvilKey = @"Anvil";
		internal const float ingotVoxelEquivalent = 2.38f;

		private Dictionary<AssetLocation, uint> itemToVoxelLookup = new Dictionary<AssetLocation, uint>();

		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ServerCoreAPI ServerCore { get; set; }
		private ClientCoreAPI ClientCore { get; set; }
		//private RecipeLoader LoaderOfRecipies { get; set;}

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override double ExecuteOrder( )
		{
		return 0.1d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;

		
		

		base.Start(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
		this.ServerAPI = api;
		

		if (api is ServerCoreAPI) {
		ServerCore = api as ServerCoreAPI;
		}
		else {
		Mod.Logger.Error("Cannot access 'ServerCoreAPI' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}
					
		//ServerAPI.ClassRegistry.GetBlockEntityClass
		//ServerAPI.RegisterBlockEntityClass(anvilKey, typeof(MetalRecovery_BlockEntityAnvil));

		ServerCore.ClassRegistryNative.ReplaceBlockEntityType(anvilKey, typeof(MetalRecovery_BlockEntityAnvil));

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be installed...");
			ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, MaterialDataGathering);
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
		base.StartClientSide(api);

		if (api is ClientCoreAPI) {
		ClientCore = api as ClientCoreAPI;
		}
		else {
		Mod.Logger.Error("Cannot access 'ClientCoreAPI' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}

		ClientCore.ClassRegistryNative.ReplaceBlockEntityType(anvilKey, typeof(MetalRecovery_BlockEntityAnvil));

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be installed...");
		}

		/*
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
		*/

		private void MaterialDataGathering( )
		{
		//Count out Voxels in smthing recipes for all metal-ingot(?) derived items;
		var examineList = ServerAPI.World.SmithingRecipes.Where(sr => sr.Enabled && sr.Ingredient.Type == EnumItemClass.Item && sr.Output.Type == EnumItemClass.Item);

		foreach (var recipie in examineList) 
		{		
			CollectibleObject inputObject =  recipie.Ingredient.Type == EnumItemClass.Item ? ServerAPI.World.GetItem(recipie.Ingredient.Code) : ServerAPI.World.GetBlock(recipie.Ingredient.Code) as CollectibleObject;
			Item outputItem = ServerAPI.World.GetItem(recipie.Output.Code);

			if (inputObject.CombustibleProps != null && inputObject.CombustibleProps.SmeltingType == EnumSmeltType.Smelt && inputObject.CombustibleProps.SmeltedRatio > 0) {
			//Item Input Has a metal Unit value...(Smeltable)	
			//Resolve?
			int setVoxels = 0;
			setVoxels = recipie.Voxels.OfType<bool>( ).Count(vox => vox);

			#if DEBUG
			Mod.Logger.VerboseDebug($"{recipie.Output.Quantity}* '{outputItem.Code}' -> {setVoxels}x '{inputObject.Code}' voxel = ~{setVoxels * ingotVoxelEquivalent:F1} metal Units");
			#endif

			if (outputItem.Tool.HasValue) 
			{
				itemToVoxelLookup.Add(outputItem.Code.Clone( ), (( uint )(setVoxels / recipie.Output.Quantity)));
				#if DEBUG
				Mod.Logger.VerboseDebug($"Mapped: (tool) '{outputItem.Code}' -> (tool) '{outputItem.Code}'");
				#endif
			}
			else 
			{
			//Tool-head map to Tool item			
			var itemToolCode = ServerAPI.World.GridRecipes.FirstOrDefault(gr => gr.Ingredients.Any(crg => crg.Value.Code.Equals(outputItem.Code)) && gr.Enabled && gr.Output.Type == EnumItemClass.Item)?.Output.Code;
				if (itemToolCode != null) 
				{
					var itemTool = ServerAPI.World.GetItem(itemToolCode);
					if (itemTool.Tool.HasValue) 
					{
					itemToVoxelLookup.Add(itemToolCode.Clone( ), (( uint )(setVoxels / recipie.Output.Quantity)));
					#if DEBUG
					Mod.Logger.VerboseDebug($"Mapped: (head) '{outputItem.Code}' -> (tool) '{itemToolCode}'");
					#endif
					}
				}
			}
		}		
		}

		}
	}


}

