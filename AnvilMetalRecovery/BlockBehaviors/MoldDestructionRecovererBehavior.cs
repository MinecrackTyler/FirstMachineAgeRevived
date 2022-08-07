using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	public class MoldDestructionRecovererBehavior : BlockBehavior
	{
		public static readonly string BehaviorClassName = @"MoldDestructionRecoverer";

		public MoldDestructionRecovererBehavior(Block block) : base(block)
		{
			
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
		{
		if (world.Api.Side.IsClient( )) return;
		ICoreAPI CoreAPI = world.Api;
		ICoreServerAPI ServerAPI = world.Api as ICoreServerAPI;


		#if DEBUG
		world.Api.Logger.VerboseDebug("MoldDestructionRecovererBehavior::OnBlockBroken");
		#endif


		var someBlock = ServerAPI.World.BlockAccessor.GetBlock(pos);
		var someBlockEntity = ServerAPI.World.BlockAccessor.GetBlockEntity(pos);


		if (someBlockEntity is BlockEntityIngotMold) {
		var ingotMold = someBlockEntity as BlockEntityIngotMold;
		#if DEBUG
		world.Api.Logger.VerboseDebug("{0} Ingot Mold(s) with L {1} Units, R {2} Units", ingotMold.quantityMolds, ingotMold.fillLevelLeft, ingotMold.fillLevelRight);
		#endif
		}


		if (someBlockEntity is BlockEntityToolMold) {
		var toolMold = someBlockEntity as BlockEntityToolMold;
		#if DEBUG
		world.Api.Logger.VerboseDebug("Tool Mold with {0} Units", toolMold.fillLevel);
		#endif
		}

		}
	}
}

