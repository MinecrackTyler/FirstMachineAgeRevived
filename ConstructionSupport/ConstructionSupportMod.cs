using System;

using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace ConstructionSupport
{
	public partial class ConstructionSupportSystem : ModSystem
	{
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
		return 0.1d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;

		RegisterBlockClasses( );


		base.Start(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
		this.ServerAPI = api;
		//LoaderOfRecipies = ServerAPI.ModLoader.GetModSystem<RecipeLoader>( );

		if (api is ServerCoreAPI) {
		ServerCore = api as ServerCoreAPI;
		}
		else {
		Mod.Logger.Error("Cannot access 'ServerCoreAPI' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}






		Mod.Logger.VerboseDebug("Construction Supports - should be installed...");
		}


	}

}