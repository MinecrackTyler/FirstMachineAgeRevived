using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using HarmonyLib;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;

/* IDEAS / ISSUES
 * # DONE: Watering Can (molten-metal state) Ingot Cooling *Tssss*
 * # WIP : Ingot breaks -> Metal fragments / bits (or a blob?)
 * # DONE: Tool-break configurable ratio
*/
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

		public event Action AMR_DataReady;

		private RecoveryEntryTable itemToVoxelLookup = new RecoveryEntryTable();//Item Asset Code to: Ammount & Material

		private ICoreAPI CoreAPI;

		private ServerCoreAPI ServerCore { get; set; }
		private ClientCoreAPI ClientCore { get; set; }

		public AMRConfig CachedConfiguration {
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

		RegisterItemClassMappings( );		
		//RegisterBlockClassMappings( );
		RegisterBlockBehaviors( );

		if (api.Side.IsServer()) 
			{
			if (api is ServerCoreAPI) {
				ServerCore = api as ServerCoreAPI;
				}
			}
		else 
			{
			ClientCore = api as ClientCoreAPI;			
			}

		#if DEBUG
		//Harmony.DEBUG = true;
		#endif
		var harmony = new Harmony(this.Mod.Info.ModID);
		harmony.PatchAll( );

		base.Start(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{		
		PrepareServersideConfig( );
		PrepareDownlinkChannel( );
		ServerCore.Event.PlayerJoin += SendClientConfigMessage;
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, PersistServersideConfig);
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.GameReady, MaterialDataGathering);				
		ServerCore.Event.ServerRunPhase(EnumServerRunPhase.RunGame, CacheRecoveryDataTable);		

		SetupGeneralObservers( );		

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be running...");

		#if DEBUG
		ServerCore.RegisterCommand("durability", "edit durability of item", " (Held tool) and #", EditDurability, Privilege.give);
		#endif
					
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
		base.StartClientSide(api);					
		
		ListenForServerConfigMessage( );						

		Mod.Logger.VerboseDebug("Anvil Metal Recovery - should be running...");
		#if DEBUG
		//ClientCore.Event.LevelFinalize += DebugStuffs;
		#endif
		}

		public override void AssetsLoaded(ICoreAPI api)
		{
		Mod.Logger.VerboseDebug("AssetsLoaded");		
		}


		public override void AssetsFinalize(ICoreAPI api)
		{
		Mod.Logger.VerboseDebug("AssetsFinalize");
					
		//PerformBlockClassSwaps( );
		AttachExtraBlockBehaviors( ); 
		}

		private void RegisterItemClassMappings( )
		{
		this.CoreAPI.RegisterItemClass(@"VariableMetalItem", typeof(VariableMetalItem));
		this.CoreAPI.RegisterItemClass(@"SmartSmeltableItem", typeof(SmartSmeltableItem));
		}

		private void RegisterBlockBehaviors()
		{
		#if DEBUG
		Mod.Logger.Debug("RegisterBlockBehaviors");
		#endif
		this.CoreAPI.RegisterBlockBehaviorClass(MoldDestructionRecovererBehavior.BehaviorClassName, typeof(MoldDestructionRecovererBehavior));
		this.CoreAPI.RegisterCollectibleBehaviorClass(MoldDestructionRecovererBehavior.BehaviorClassName, typeof(MoldDestructionRecovererBehavior));
		}

		/*
		private void PerformBlockClassSwaps()
		{
		#if DEBUG
		Mod.Logger.Debug("PerformBlockClassSwaps");
		#endif
		
		if (ServerCore.World is ServerMain) {
		var serverMain = ServerCore.World as ServerMain;
		var wateringCanBlock = serverMain.GetBlock(BlockWateringCanPlus.TargetCode);
		#if DEBUG
		Mod.Logger.VerboseDebug("Change class from {0} to {1}", wateringCanBlock.Class, BlockWateringCanPlus.BlockClassName);
		#endif
		wateringCanBlock.Class = BlockWateringCanPlus.BlockClassName;
		ServerCore.World.Blocks[wateringCanBlock.BlockId] = wateringCanBlock;//Mabey redundant?
		}

		}*/

		private void AttachExtraBlockBehaviors()
		{
		Collection<AssetLocation> mold_behaviorsAppendList = new Collection<AssetLocation>( ) {
				new AssetLocation(@"game",@"ingotmold-burned"),
				new AssetLocation(@"game",@"toolmold-burned-*"),
		};		

		var moldRecoverBehaviorType = ServerCore.ClassRegistry.GetBlockBehaviorClass(MoldDestructionRecovererBehavior.BehaviorClassName);
		foreach (var assetName in mold_behaviorsAppendList) {
		if (!assetName.IsWildCard) 
				{ 
				this.CoreAPI.AddBlockBehavior(assetName, MoldDestructionRecovererBehavior.BehaviorClassName, moldRecoverBehaviorType);
				#if DEBUG
				Mod.Logger.VerboseDebug("Attached Block-Behavior {0} to '{1}' ", MoldDestructionRecovererBehavior.BehaviorClassName, assetName);
				#endif
				}
			else {
					var searchResults = ServerCore.World.SearchBlocks(assetName);
					if (searchResults != null && searchResults.Length > 0) {
					#if DEBUG
					Mod.Logger.VerboseDebug("Attaching Block-Behaviors, wildcard matches from '{0}'", assetName);
					#endif
						for (int index = 0; index < searchResults.Length; index++) 
						{
						var matchBlock = searchResults[index];
						this.CoreAPI.AddBlockBehavior(matchBlock.Code, MoldDestructionRecovererBehavior.BehaviorClassName, moldRecoverBehaviorType);
						#if DEBUG
						Mod.Logger.VerboseDebug("Attached Block-Behavior {0} to '{1}' ", MoldDestructionRecovererBehavior.BehaviorClassName, matchBlock.Code);
						#endif
						}
					}
				}
		}
		
		}

		private void SetupGeneralObservers( ){
		ServerCore.Event.RegisterEventBusListener(Item_DamageEventReciever, 1.0f, ItemDamageChannelName);		
		}

		private void PrepareServersideConfig( )
		{
		AMRConfig config = ServerCore.LoadModConfig<AMRConfig>(_configFilename);

		if (config == null) 
			{
			//Regen default
			Mod.Logger.Warning("Regenerating default config as it was missing / unparsable...");
			ServerCore.StoreModConfig<AMRConfig>(new AMRConfig(true), _configFilename);
			config = ServerCore.LoadModConfig<AMRConfig>(_configFilename);
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
		ServerCore.StoreModConfig<AMRConfig>(this.CachedConfiguration, _configFilename);
		}
		}

		private void PrepareDownlinkChannel( )
		{
		_ConfigDownlink = ServerCore.Network.RegisterChannel(_configFilename);
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
		Mod.Logger.Debug("Message value; Recover Broken Tools:{0}, VoxelEquiv:{1:F2}, Tool Recovery {3:P0}, Blacklisted:{2}", networkMessage.ToolFragmentRecovery, networkMessage.VoxelEquivalentValue, networkMessage.BlackList.Count, networkMessage.ToolRecoveryRate);
		this.CachedConfiguration = networkMessage;
		}
		}

		private void CacheRecoveryDataTable( )
		{
		this.AMR_DataReady?.Invoke();
		// Cache list too
		#if DEBUG
		Mod.Logger.VerboseDebug("Adding Recovery entries table to Cache...");
		#endif
		ServerCore.ObjectCache.Add(itemFilterListCacheKey, itemToVoxelLookup);
		}
	}
}