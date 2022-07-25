using System;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	public class BlockWateringCanPlus : BlockWateringCan
	{
		private const float coolRateDefault = 5.0f;

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
		this.api.Logger.VerboseDebug("BlockWateringCanPlus::OnHeldInteractStep");
		var result =  base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);

		PerformBlockCooling(secondsUsed, slot, byEntity, blockSel, entitySel);

		return result;
		}

		private void PerformBlockCooling(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
		if (blockSel == null) return;
		if (byEntity.Controls.Sneak) return;

		BlockPos targetPos = blockSel.Position;

			if (this.GetRemainingWateringSeconds(slot.Itemstack) >= 0.1f) 
			{
			var server = (CoreAPI.World.Side.IsServer());
						
			var someBlock = CoreAPI.World.BlockAccessor.GetBlock(targetPos);

			if (someBlock != null
				&& someBlock.BlockMaterial == EnumBlockMaterial.Ceramic
				&& (someBlock.Class == @"BlockIngotMold" || someBlock.Class == @"BlockToolMold")) 
				{
				var someBlockEntity = CoreAPI.World.BlockAccessor.GetBlockEntity(targetPos);

				if (someBlockEntity is BlockEntityIngotMold) {
					var ingotMold = someBlockEntity as BlockEntityIngotMold;
					if (ingotMold.fillSide && ingotMold.fillLevelRight > 0 && ingotMold.IsLiquidRight) {
					if (server) CoolContents(ingotMold.contentsRight); else GenerateSpecialEffects(blockSel.HitPosition);
					}
					else if (ingotMold.fillLevelLeft > 0 && ingotMold.IsLiquidLeft) {
					if (server) CoolContents(ingotMold.contentsLeft); else GenerateSpecialEffects(blockSel.HitPosition);
					}
					return;
				}
				
				if (someBlockEntity is BlockEntityToolMold) {
					var toolMold = someBlockEntity as BlockEntityToolMold;
					if (toolMold.fillLevel > 0 && toolMold.IsLiquid) {
					if (server) CoolContents(toolMold.metalContent); else GenerateSpecialEffects(blockSel.HitPosition);
						}
					return;
					}
				}
			}			 					
		}

		internal void GenerateSpecialEffects(Vec3d  here)
		{
			var steamParticles = new SimpleParticleProperties {
				MinPos = here,
				AddPos = here.AddCopy(0.1,0.1,0.1),
				MinQuantity = 8,
				AddQuantity = 24,
				Color = ColorUtil.ToRgba(100, 225, 225, 225),
				GravityEffect = -0.015f,
				WithTerrainCollision = true,
				ParticleModel = EnumParticleModel.Quad,
				LifeLength = 2.0f,
				MinVelocity = new Vec3f(-0.25f, 0.1f, -0.25f),
				AddVelocity = new Vec3f(0.25f, 0.1f, 0.25f),
				MinSize = 0.075f,
				MaxSize = 0.1f,
				//VertexFlags = 32
			};
			ClientAPI.World.SpawnParticles(steamParticles );
			ClientAPI.World.PlaySoundAt(CoolSoundEffect, here.X,here.Y,here.Z, null, false, 16 );		
		}

		internal void CoolContents(ItemStack itemStack)
		{
		var temperature = itemStack.Collectible.GetTemperature(CoreAPI.World, itemStack);
		if (temperature > 25f)//TODO: USE local AMBIENT Temp
			itemStack.Collectible.SetTemperature(CoreAPI.World, itemStack, (temperature - coolRateDefault), false);
		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Reduced Molten metal temp: {0:F1}  ", temperature);
		#endif
		}


	}
}

