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
		return 0.5d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;
					
		RegisterItemClasses( );
		RegisterBlockClasses( );
		Mod.Logger.Event("Registered classes for toolin' & steely stuff...");
		/* WORKAROUND: Due to over-eager loading of GridRecipies - and block/item defs,  
		 * Load Alternate Recipes, AND placeholder items Later than "normal"
		 * as classes get registered "too" late...
		 * Consider; OVERWRITE of all 'ignored' recipies/items/blocks - and force reload after registering...
		*/
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
		//api.Event.SaveGameLoaded += OnSaveGameLoaded;
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.LoadGame, OnServerLoadGame);
		

		

		Mod.Logger.VerboseDebug("Elemental Tools - should be installed...");		
		}

		private void OnServerLoadGame( )
		{
		GenerateSteelEquivalentObjects( );
		ManipulateGridRecipies( );
		//Re-activate crafting recipes for blister_steel stuff
		ReloadGridRecipes( );
		}
	}


}

