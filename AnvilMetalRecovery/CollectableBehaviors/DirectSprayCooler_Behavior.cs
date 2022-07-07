using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{

	/// <summary>
	/// Direct spray cooler collectable behavior.
	/// </summary>
	/// <remarks>*TSSSSS!*</remarks>
	public class DirectSprayCooler_Behavior : BlockBehavior
	{
		public const string ClassName = @"directspraycooler_behavior";
		private const float coolRate = 0.5f;
		private BlockWateringCan WateringCan;
		protected ICoreAPI CoreAPI { get; set; }

		public DirectSprayCooler_Behavior(Block block) : base(block)
		{ 
			
		}


		internal void coolContents(ItemStack itemStack)
		{
		var temperature = itemStack.Collectible.GetTemperature(CoreAPI.World, itemStack);
		if (temperature > 25f)//TODO: USE local AMBIENT Temp
		itemStack.Collectible.SetTemperature(CoreAPI.World, itemStack, (temperature - coolRate), false);
		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Reduced Molten metal temp: {0:F1}  ", temperature);
		#endif
		}

		public override void OnLoaded(ICoreAPI api) 
		{
		#if DEBUG
		api.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnLoaded...");
		#endif
		base.OnLoaded(api);
		CoreAPI = api;
			/*
		WateringCan = block as BlockWateringCan;
		if (WateringCan == null) 
			{ throw new InvalidOperationException(string.Format("Block with code '{0}' does not inherit from BlockWateringCan, which is required", block.Code)); }
		*/
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
		#if DEBUG
		CoreAPI.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnHeldInteractStep...");
		#endif

		handling = EnumHandling.PassThrough;
		if (blockSel == null) return false;

		if (CoreAPI.World.Side.IsServer( )) 
			{				
			if (WateringCan.GetRemainingWateringSeconds(slot.Itemstack) >= 0.5f) {
			BlockPos targetPos = blockSel.Position;
			var someBlock = CoreAPI.World.BlockAccessor.GetBlock(targetPos);

				if (someBlock != null
					&& someBlock.BlockMaterial == EnumBlockMaterial.Ceramic
					&& (someBlock.Class == @"BlockIngotMold" || someBlock.Class == @"BlockToolMold")) 
				{
				BlockEntityIngotMold ingotBE = CoreAPI.World.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityIngotMold;

				if (ingotBE != null) 
					{
						if (ingotBE.fillSide == true && ingotBE.IsLiquidRight) 
							{ coolContents(ingotBE.contentsRight); }
						else if (ingotBE.IsLiquidLeft) 
							{ coolContents(ingotBE.contentsLeft); }

					return false;
					}

				BlockEntityToolMold toolBE = CoreAPI.World.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityToolMold;
				if (toolBE != null) 
					{
					if (toolBE.IsLiquid) { coolContents(toolBE.metalContent); }

					return false;
					}
				}	
			}

		}

		return false;
		}



	}
}

