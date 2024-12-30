using System;
using System.Collections.Generic;

using Vintagestory.API;
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

		public const string BoltableDoorEntityNameKey = @"BoltableDoorEntity";
		public const string CollapsingBlockEntityNameKey = @"CollapsingBlockEntity";


        //public override bool AllowRuntimeReload => false;

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
		RegisterBehaviorClasses( );
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
		CoreAPI.RegisterBlockClass(@"BoltableDoor", typeof(BoltableDoor));
		CoreAPI.RegisterBlockClass(@"FalseWall", typeof(FalseWall));		
		CoreAPI.RegisterBlockClass(@"CollapsingBlock", typeof(CollapsingBlock));
		CoreAPI.RegisterBlockClass(@"RectangularBrazier", typeof(RectangularBrazier));


		CoreAPI.RegisterBlockEntityClass(BoltableDoorEntityNameKey, typeof(BoltableDoorBlockEntity));
		}

		private void RegisterBehaviorClasses( )
		{
		CoreAPI.RegisterBlockBehaviorClass(@"FreeReinforcement", typeof(BlockBehaviorFreeReinforcement));
		CoreAPI.RegisterBlockBehaviorClass(@"VerticalOrentiation",typeof(BlockBehaviorVerticalOrientation));
		CoreAPI.RegisterBlockBehaviorClass(@"NeedSides", typeof(BlockBehaviorNeedSides));
		}
	}




}

