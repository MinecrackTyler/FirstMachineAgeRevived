using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{
	public partial class MetalRecoverySystem : ModSystem
	{
		internal const string _configFilename = @"amr_config.json";
		internal const string anvilKey = @"Anvil";
		internal const string metalFragmentsCode = @"fma:metal_fragments";
		internal const string metalShavingsCode = @"metal_shaving";
		internal const string itemFilterListCacheKey = @"AMR_ItemFilters";
		public const float IngotVoxelDefault = 2.38f;
		public const string ItemDamageChannelName = @"ItemDamageEvents";

		internal IServerNetworkChannel _ConfigDownlink;
		internal IClientNetworkChannel _ConfigUplink;

		private RecoveryEntryTable itemToVoxelLookup = new RecoveryEntryTable();//Item Asset Code to: Ammount & Material

		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ServerCoreAPI ServerCore { get; set; }
		private ClientCoreAPI ClientCore { get; set; }

		internal AMRConfig CachedConfiguration {
			get
			{
			return ( AMRConfig )CoreAPI.ObjectCache[_configFilename];
			}
			set
			{
			CoreAPI.ObjectCache.Add(_configFilename, value);
			}
		}

		internal List<SmithingRecipe> SmithingRecipies
		{
			get { return CoreAPI.ModLoader.GetModSystem<RecipeRegistrySystem>( ).SmithingRecipes; }
        }

		public static RecoveryEntryTable GetCachedLookupTable(IWorldAccessor world )
		{
			return ( RecoveryEntryTable )world.Api.ObjectCache[MetalRecoverySystem.itemFilterListCacheKey];
		}

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
		/// ALL Items that have were derivable from smithing recipies (and are 'tool' / have durability property)
		/// </summary>/
		/// <value>The item filter list.</value>
		public RecoveryEntryTable ItemRecoveryTable {
			get
			{
			return itemToVoxelLookup;
			}
			set
			{
			itemToVoxelLookup = value;
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

		#if DEBUG
		//Harmony.DEBUG = true;
		#endif
		var harmony = new Harmony(this.Mod.Info.ModID);
		harmony.PatchAll( );

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
		PrepareServersideConfig( );
		PrepareDownlinkChannel( );
		ServerAPI.Event.PlayerJoin += SendClientConfigMessage;
		ServerAPI.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, PersistServersideConfig);
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, MaterialDataGathering);		

		SetupGeneralObservers( );

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be installed...");

		#if DEBUG
		ServerAPI.RegisterCommand("durability", "edit durability of item", " (Held tool) and #", EditDurability, Privilege.give);
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

		ListenForServerConfigMessage( );
		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be installed...");
		}


		private void RegisterItemMappings( )
		{
		this.CoreAPI.RegisterItemClass(@"VariableMetalItem", typeof(VariableMetalItem));
		this.CoreAPI.RegisterItemClass(@"SmartSmeltableItem", typeof(SmartSmeltableItem));
		}



		private void SetupGeneralObservers( ){
		ServerCore.Event.RegisterEventBusListener(Item_DamageEventReciever, 1.0f, ItemDamageChannelName);		
		}

		private void PrepareServersideConfig( )
		{
		AMRConfig config = ServerAPI.LoadModConfig<AMRConfig>(_configFilename);

		if (config == null) 
			{
			//Regen default
			Mod.Logger.Warning("Regenerating default config as it was missing / unparsable...");
			ServerAPI.StoreModConfig<AMRConfig>(new AMRConfig(true), _configFilename);
			config = ServerAPI.LoadModConfig<AMRConfig>(_configFilename);
			}
			else if( config.BlackList == null || config.BlackList.Count == 0)
			{
			AMRConfig defaults = new AMRConfig(true);
			config.BlackList = defaults.BlackList;
			}

		this.CachedConfiguration = config;					
		}

		private void PersistServersideConfig( )
		{
		if (this.CachedConfiguration != null) {
		Mod.Logger.Notification("Persisting configuration.");
		ServerAPI.StoreModConfig<AMRConfig>(this.CachedConfiguration, _configFilename);
		}
		}

		private void PrepareDownlinkChannel( )
		{
		_ConfigDownlink = ServerAPI.Network.RegisterChannel(_configFilename);
		_ConfigDownlink.RegisterMessageType<AMRConfig>( );
		}

		private void SendClientConfigMessage(IServerPlayer byPlayer)
		{
		#if DEBUG
		Mod.Logger.VerboseDebug("Sending joiner: {0} a copy of config data.", byPlayer.PlayerName);
		#endif

		_ConfigDownlink.SendPacket<AMRConfig>(this.CachedConfiguration, byPlayer);
		}

		private void ListenForServerConfigMessage( )
		{
		_ConfigUplink = ClientCore.Network.RegisterChannel(_configFilename);
		_ConfigUplink = _ConfigUplink.RegisterMessageType<AMRConfig>( );

		#if DEBUG
		Mod.Logger.VerboseDebug("Registered RX channel: '{0}'", _ConfigUplink.ChannelName);
		#endif

		_ConfigUplink.SetMessageHandler<AMRConfig>(RecievedConfigMessage);
		}

		private void RecievedConfigMessage(AMRConfig networkMessage)
		{
		#if DEBUG
		Mod.Logger.Debug("Got Config message!");
		#endif

		if (networkMessage != null) {
			Mod.Logger.Debug("Message value; Recover Broken Tools:{0}, VoxelEquiv#{1:F2}, Blacklist #{2}", networkMessage.ToolFragmentRecovery, networkMessage.VoxelEquivalentValue, networkMessage.BlackList.Count);
		this.CachedConfiguration = networkMessage;
		}
		}



	}


}

