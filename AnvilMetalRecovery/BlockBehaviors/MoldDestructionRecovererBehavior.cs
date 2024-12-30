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
		private readonly AssetLocation MetalBits_partial = new AssetLocation(GlobalConstants.DefaultDomain, @"metalbit");
		private const int shavingValue = 5;



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
		world.Api.Logger.VerboseDebug("{0} Ingot Mold(s) with L {1} Units, R {2} Units", ingotMold.QuantityMolds, ingotMold.FillLevelLeft, ingotMold.FillLevelRight);
		#endif

		if ( ingotMold.FillLevelLeft >= shavingValue && ingotMold.ContentsLeft != null) 
			{
			var ingotMetal = ingotMold.ContentsLeft.Collectible.Variant[@"metal"];
			SpawnMetalBits(world, pos, ingotMold.FillLevelLeft, ingotMetal);
			}
		
		if ( ingotMold.FillLevelRight >= shavingValue && ingotMold.ContentsRight != null) 
			{
			var ingotMetal = ingotMold.ContentsLeft.Collectible.Variant[@"metal"];
			SpawnMetalBits(world, pos, ingotMold.FillLevelRight, ingotMetal);
			}

		return;
		}		

		if (someBlockEntity is BlockEntityToolMold) {
		var toolMold = someBlockEntity as BlockEntityToolMold;
		#if DEBUG
		world.Api.Logger.VerboseDebug("Tool Mold with {0} Units", toolMold.FillLevel);
		#endif
		if ( toolMold.FillLevel >= shavingValue && toolMold.MetalContent != null) 
			{
				var metalCode = toolMold.MetalContent.Collectible.Variant.AnyKeys(@"metal", @"material"); 
				SpawnMetalBits(world, pos, toolMold.FillLevel, metalCode);
			}		
		}

		}

		internal void SpawnMetalBits(IWorldAccessor world, BlockPos pos, int unitQuantity, string baseMetalCode)
		{	
			if (unitQuantity > 0 && pos != null && !string.IsNullOrEmpty(baseMetalCode)) 
			{
			int shavingQty = unitQuantity / shavingValue;
			Item metalShavingsItem = world.Api.World.GetItem(MetalBits_partial.AppendPathVariant(baseMetalCode));

			if (shavingQty >= 1 && metalShavingsItem != null) 
				{
				var metalShavingsStack = new ItemStack(metalShavingsItem, shavingQty);				
				#if DEBUG
				world.Api.Logger.VerboseDebug("Creating '{0}' @{1} *{2} Units",metalShavingsItem, pos, shavingQty);
				#endif
				world.Api.World.SpawnItemEntity(metalShavingsStack, pos.ToVec3d( ).Add(0.1d, 0, 0)); 			
				}
			}		
		}
	}
}

