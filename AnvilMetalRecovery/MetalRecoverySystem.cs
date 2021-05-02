using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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
		internal const string metalFragmentsCode = @"fma:metal_fragments";
		internal const string metalShavingsCode = @"metal_shaving";
		public const float IngotVoxelEquivalent = 2.38f;

		private Dictionary<AssetLocation, RecoveryEntry> itemToVoxelLookup = new Dictionary<AssetLocation, RecoveryEntry>();//Ammount & Material?

		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ServerCoreAPI ServerCore { get; set; }
		private ClientCoreAPI ClientCore { get; set; }


		/// <summary>
		/// Valid Items that are 'recoverable' (Asset Codes) only
		/// </summary>
		/// <value>The item filter list.</value>
		public List<AssetLocation> ItemFilterList {
			get
			{
			return itemToVoxelLookup.Keys.ToList( );
			}
		}

		/// <summary>
		/// ALL Items that have were derivable from smithing recipies (and are tool / durable)
		/// </summary>
		/// <value>The item filter list.</value>
		public Dictionary<AssetLocation, RecoveryEntry> ItemRecoveryTable {
			get
			{
			return itemToVoxelLookup;
			}
		}

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override double ExecuteOrder( )
		{
		return 0.11d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;

		RegisterItemMappings( );

		//TODO: Setup HARMONY Patches

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

		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, MaterialDataGathering);		

		SetupHotbarObserver( );

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be installed...");

		#if DEBUG
			ServerAPI.RegisterCommand("durability", "edit durability of item", " (Held tool) and #", EditDurability);
		#endif

		
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


		private void RegisterItemMappings( )
		{
		this.CoreAPI.RegisterItemClass(@"VariableMetalItem", typeof(VariableMetalItem));
		this.CoreAPI.RegisterItemClass(@"SmartSmeltableItem", typeof(SmartSmeltableItem));
		}



		private void SetupHotbarObserver( ){
		ServerCore.RegisterEntityBehaviorClass(@"HotbarObserver", typeof(HotbarObserverBehavior));
		ServerCore.Event.RegisterEventBusListener(HotbarEventReciever, 1.0f, HotbarObserverBehavior.HotbarChannelName);
		}


	}


}

