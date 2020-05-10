using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{
	public class MetalRecoverySystem : ModSystem
	{
		internal const string anvilKey = @"Anvil";

		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ServerCoreAPI ServerCore { get; set; }

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override double ExecuteOrder( )
		{
		return 0.10d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;
		CoreAPI.RegisterItemClass(@"ItemMallet", typeof(ItemMallet));

		base.Start(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
		base.StartServerSide(api);

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

		internal void GenerateMetalShavingsItems( )
		{
		//TODO: Automatic Generation of Item 'metal_shaving' by metal & alloy list at RUNTIME
		var genericShaving = ServerAPI.World.ClassRegistry.CreateItem("metal_shaving");
		//genericShaving.CombustibleProps.

		var metalProperties = new Dictionary<AssetLocation, MetalProperty>( );

		foreach (var entry in ServerAPI.Assets.GetMany<MetalProperty>(Mod.Logger, "worldproperties/")) 
			{
			AssetLocation loc = entry.Key.Clone( );
			loc.Path = loc.Path.Replace("worldproperties/", "");
			loc.RemoveEnding( );

			entry.Value.Code.Domain = entry.Key.Domain;

			metalProperties.Add(loc, entry.Value);

			}
		}	
	}

	internal static class Helpers
	{
		internal static void ReplaceBlockEntityType(this ClassRegistry registry, string className, Type blockentity)
		{
		if (registry.blockEntityClassnameToTypeMapping.ContainsKey(className)) {
		//replace it
		registry.blockEntityClassnameToTypeMapping[className] = blockentity;
		registry.blockEntityTypeToClassnameMapping[blockentity] = className;
		}
		}

		internal static void ReplaceItemClassType(this ClassRegistry registry, string className, Type replacer)
		{
		if (registry.ItemClassToTypeMapping.ContainsKey(className)) {
		//replace it
		registry.ItemClassToTypeMapping[className] = replacer;		
		}
		}
	}
}

