using System;
using System.Text;

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
	public class BlockWateringCanPlus : BlockWateringCan
	{
		private const float coolRateDefault = 0.0075f;
		private const float flashPointTemp = 100f;

		private SimpleParticleProperties steamParticles = new SimpleParticleProperties {
			MinPos = new Vec3d(),
			AddPos = new Vec3d( ),
			MinQuantity = 6,
			AddQuantity = 12,
			Color = ColorUtil.ToRgba(100, 225, 225, 225),
			OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 1.0f),
			GravityEffect = -0.015f,
			WithTerrainCollision = false,
			ShouldDieInLiquid = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 2.0f,
			MinVelocity = new Vec3f(-0.25f, 0.1f, -0.25f),
			AddVelocity = new Vec3f(0.25f, 0.1f, 0.25f),
			MinSize = 0.075f,
			MaxSize = 0.1f,
			WindAffected = true,
			WindAffectednes = 0.4f,
		};


		protected ICoreAPI CoreAPI { get { return this.api;  }  }
		protected ICoreServerAPI ServerAPI { get { return this.api as ICoreServerAPI; } }
		protected ICoreClientAPI ClientAPI { get { return this.api as ICoreClientAPI; } }

		public readonly AssetLocation CoolSoundEffect = new AssetLocation(@"game", @"sounds/sizzle");
		public static readonly string BlockClassName = @"BlockWateringCan";
		public static readonly AssetLocation TargetCode = new AssetLocation(@"game", @"wateringcan-burned");

		/// <summary>
		/// DO OnHeldInteractStep
		/// </summary>
		/// <returns>The held interact step.</returns>
		/// <param name="secondsUsed">Seconds used.</param>
		/// <param name="slot">Slot.</param>
		/// <param name="byEntity">By entity.</param>
		/// <param name="blockSel">Block sel.</param>
		/// <param name="entitySel">Entity sel.</param>
		/// <remarks>
		/// Had to do it this way the base class _NEVER_ invokes the block-behavior virtual...
		/// </remarks>
		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
		#if DEBUG
		this.api.Logger.VerboseDebug("BlockWateringCanPlus::OnHeldInteractStep");
		#endif

		PerformBlockCooling(secondsUsed, slot, byEntity, blockSel, entitySel);
		return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) 
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get(@"fma:spray_cooler_text"));
		}

		private void PerformBlockCooling(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
		if (blockSel == null) return;
		if (byEntity.Controls.Sneak) return;

		if ((DateTime.Now.Millisecond / 100) % 2 == 1) return;

		BlockPos targetPos = blockSel.Position;

			if (!slot.Empty && this.GetRemainingWateringSeconds(slot.Itemstack) >= 0.1f) 
			{
			var server = (CoreAPI.World.Side.IsServer());						
			var someBlock = CoreAPI.World.BlockAccessor.GetBlock(targetPos);
		 	
			if (someBlock != null
				&& someBlock.BlockMaterial == EnumBlockMaterial.Ceramic
				&& (someBlock.Class == @"BlockIngotMold" || someBlock.Class == @"BlockToolMold")) 
				{
				var someBlockEntity = server ? ServerAPI.World.BlockAccessor.GetBlockEntity(targetPos) : ClientAPI.World.BlockAccessor.GetBlockEntity(targetPos);
				
				#if DEBUG
				this.api.Logger.VerboseDebug("Ok, its an Tool/Ingot mold.: {0}",someBlockEntity);
				#endif

				if (someBlockEntity is BlockEntityIngotMold) {
					var rightSide = AimAtRight(blockSel.HitPosition);
					var ingotMold = someBlockEntity as BlockEntityIngotMold;
					
					if (rightSide && (ingotMold.fillLevelRight > 0 && ingotMold.TemperatureRight > flashPointTemp)) {							
					if (server) CoolContents(ingotMold.contentsRight); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer);
					ingotMold.MarkDirty( );
					}
					else if (ingotMold.fillLevelLeft > 0 && ingotMold.TemperatureLeft > flashPointTemp) {
					if (server) CoolContents(ingotMold.contentsLeft); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer);
					ingotMold.MarkDirty( );
					}
					return;
				}
				
				if (someBlockEntity is BlockEntityToolMold) {
					var toolMold = someBlockEntity as BlockEntityToolMold;
					if (toolMold.fillLevel > 0 && toolMold.Temperature > flashPointTemp) {
					if (server) CoolContents(toolMold.metalContent); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer );
						toolMold.MarkDirty( );
						}
					return;
					}
				}
			}			 					
		}

		internal void GenerateSpecialEffects(BlockPos blockLoc, Vec3d aimPoint, EntityPlayer playerEntity)
		{
		if ((DateTime.Now.Millisecond / 333) % 2 == 1) return;
		
		steamParticles.MinPos = blockLoc.ToVec3d().AddCopy(aimPoint);		
		steamParticles.AddPos = new Vec3d(0.05f, 0f, 0.05f);

		#if DEBUG
		api.Logger.VerboseDebug("Generate steam particles");
		#endif

		ClientAPI.World.SpawnParticles(steamParticles, playerEntity.Player );
		ClientAPI.World.PlaySoundAt(CoolSoundEffect, playerEntity, playerEntity.Player, randomizePitch: false, volume: 0.5f);
		}

		internal void CoolContents(ItemStack itemStack)
		{
		var temperature = itemStack.Collectible.GetTemperature(CoreAPI.World, itemStack);
		if (temperature > 20f)//TODO: USE local AMBIENT Temp
			itemStack.Collectible.SetTemperature(CoreAPI.World, itemStack, temperature - (temperature * coolRateDefault), false);
			(itemStack.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 400);

		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Cooled Molten metal, temp: {0:F1}  ", temperature);
		#endif
		}

		internal bool AimAtRight(Vec3d hitPosition)
		{
		return hitPosition.X >= 0.5f;
		}
	}
}

