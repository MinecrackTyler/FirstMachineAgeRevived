using System;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace ElementalTools
{
	public partial class ElementalToolsSystem : ModSystem
	{
		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ICoreClientAPI ClientAPI;

		private ServerCoreAPI ServerCore { get; set; }
		private ClientCoreAPI ClientCore { get; set; }

		private RecipeLoader LoaderOfRecipies { get; set;}

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override double ExecuteOrder( )
		{
		return 0.1999d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;
					
		RegisterItemClasses( );
		RegisterBlockClasses( );
		RegisterEntityClasses( );
		Mod.Logger.Notification("Registered classes for toolin' & steely stuff...");
				

		base.Start(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
		this.ServerAPI = api;
		LoaderOfRecipies = ServerAPI.ModLoader.GetModSystem<RecipeLoader>( );

		if (api is ServerCoreAPI) {
		ServerCore = api as ServerCoreAPI;
		}
		else {
		Mod.Logger.Error("Cannot access 'ServerCoreAPI' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}
		
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, PostLoadTweaks);				

		Mod.Logger.VerboseDebug("Elemental Tools - should be installed...");		
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
		this.ClientAPI = api;
		
		if (api is ClientCoreAPI) {
		ClientCore = api as ClientCoreAPI;
		}
		else {
		Mod.Logger.Error("Cannot access 'ClientCoreAPI' class:  API (implimentation) has changed, Contact Developer!");
		return;
		}

		//ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, PostLoadTweaks);

		ClientCore.Event.LevelFinalize += ClientSideTweaks;


		Mod.Logger.VerboseDebug("Elemental Tools - should be installed...");
		}

		private void PostLoadTweaks( )
		{
		Mod.Logger.Notification("Making a few changes to recipes...");
		
		#if DEBUG
		Mod.Logger.VerboseDebug($"Total GridRecipies: {CoreAPI.World.GridRecipes.Count}");
		#endif

		MalletInsertion( );
		GenerateSharpeningGridRecipies( );
		CloneEntityClasses( );
		}

		private void ClientSideTweaks( )
		{
		CloneEntityClasses( );
		}

	}


}

