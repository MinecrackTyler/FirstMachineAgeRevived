using System;

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
		return forSide.IsServer( );
		}

		public override double ExecuteOrder( )
		{
		return 0.11d;
		}

		public override void Start(ICoreAPI api)
		{
		base.Start(api);
		this.CoreAPI = api;
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
	}
}

