using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{

	/// <summary>
	/// Direct spray cooler collectable behavior.
	/// </summary>
	/// <remarks>*TSSSSS!*</remarks>
	public class DirectSprayCooler_Behavior : CollectibleBehavior
	{
		private const string coolRateKey = @"coolRate";
		private const float coolRateDefault = 0.5f;
		private BlockWateringCan WateringCan;
		protected ICoreAPI CoreAPI { get; private set;}
		protected ICoreServerAPI ServerAPI { get; private set;}

		public const string ClassName = @"directspraycooler";
		public float CoolRate { get; private set;}

		public DirectSprayCooler_Behavior(CollectibleObject collecta) : base(collecta)
		{
		}				

		public override void Initialize(JsonObject properties)
		{
		base.Initialize(properties);

		CoolRate = properties[coolRateKey].AsFloat(coolRateDefault);
		}

		public override void OnLoaded(ICoreAPI api)
		{
		base.OnLoaded(api);
		CoreAPI = api;

		#if DEBUG
		api.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnLoaded...");
		#endif
		
		WateringCan = this.collObj as BlockWateringCan;
		if (WateringCan == null) { throw new InvalidOperationException(string.Format("Block with code '{0}' does not inherit from BlockWateringCan, which is required", collObj.Code)); }

		}

		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		dsc.Append(Lang.Get("fma:spray_cooler_text"));
		}



		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
		#if DEBUG
		byEntity.World.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnHeldInteractStart...");
		#endif

		handHandling = EnumHandHandling.PreventDefault;
		handling = EnumHandling.PassThrough;    //EnumHandling.PreventDefault;		 				
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
		#if DEBUG
		byEntity.World.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnHeldInteractStop...");
		#endif
		handling = EnumHandling.PassThrough;
		
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
		#if DEBUG
		byEntity.World.Logger.VerboseDebug("DirectSprayCooler_Behavior::OnHeldInteractStep...");
		#endif
		CoreAPI = byEntity.World.Api;
					
		if (blockSel == null) return false;
		if (byEntity.Controls.Sneak) return false;

		if (CoreAPI.World.Side.IsServer( )) 
			{
			ServerAPI = byEntity.World.Api as ICoreServerAPI;;
			if (WateringCan.GetRemainingWateringSeconds(slot.Itemstack) >= 0.5f) {
			BlockPos targetPos = blockSel.Position;
			var someBlock = ServerAPI.World.BlockAccessor.GetBlock(targetPos);

				if (someBlock != null
					&& someBlock.BlockMaterial == EnumBlockMaterial.Ceramic
					&& (someBlock.Class == @"BlockIngotMold" || someBlock.Class == @"BlockToolMold")) 
				{
				BlockEntityIngotMold ingotBE = ServerAPI.World.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityIngotMold;

				if (ingotBE != null) 
					{
						if (ingotBE.fillSide == true && ingotBE.IsLiquidRight) 
							{ coolContents(ingotBE.contentsRight); }
						else if (ingotBE.IsLiquidLeft) 
							{ coolContents(ingotBE.contentsLeft); }
					handling = EnumHandling.PreventDefault;//?
					return true;
					}

				BlockEntityToolMold toolBE = ServerAPI.World.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityToolMold;
				if (toolBE != null) 
					{
					if (toolBE.IsLiquid) { coolContents(toolBE.metalContent); }

					return false;
					}
				}	
			}
		}
		
		handling = EnumHandling.PassThrough;
		return false;
		}


		internal void coolContents(ItemStack itemStack)
		{

		var temperature = itemStack.Collectible.GetTemperature(CoreAPI.World, itemStack);
		if (temperature > 25f)//TODO: USE local AMBIENT Temp
			itemStack.Collectible.SetTemperature(CoreAPI.World, itemStack, (temperature - CoolRate), false);
		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Reduced Molten metal temp: {0:F1}  ", temperature);
		#endif
		}


	}
}

