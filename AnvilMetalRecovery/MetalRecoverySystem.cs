using System;
using System.Collections.Generic;

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
	}


}

