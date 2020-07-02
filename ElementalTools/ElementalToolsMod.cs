using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace ElementalTools
{
	public partial class ElementalToolsSystem : ModSystem
	{
		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		internal static readonly string PackCarburizationEntityNameKey = @"PackCarburizationEntity";
		internal static readonly string IronNameKey = @"iron";
		internal static readonly string MaterialNameKey = @"metal";
		internal static readonly string MetalNameKey = @"material";

		private ServerCoreAPI ServerCore { get; set; }
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

		RegisterItemClasses( );


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

		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.LoadGame, OnServerLoadGame);
		

		

		Mod.Logger.VerboseDebug("Elemental Tools - should be installed...");		
		}

		private void OnServerLoadGame( )
		{
		ManipulateGridRecipies( );
		}
	}


}

