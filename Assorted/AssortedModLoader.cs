using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace FirstMachineAge
{
	public class AssortedModSystems : ModSystem
	{		
		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ServerCoreAPI ServerCore { get; set; }

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
			return forSide.IsClient() || forSide.IsServer();
		}

		public override double ExecuteOrder( )
		{
		return 0.1d;
		}

		public override void Start(ICoreAPI api)
		{
		base.Start(api);
		this.CoreAPI = api;

		RegisterBlockClasses( );
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

		}

		private void RegisterBlockClasses( )
		{
		CoreAPI.RegisterBlockClass("BoltableDoor", typeof(BoltableDoor));
		CoreAPI.RegisterBlockEntityClass("BoltableDoorEntity", typeof(BoltableDoorBlockEntity));
		}
	}

	/*
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
	*/
}

