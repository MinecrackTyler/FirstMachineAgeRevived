using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery.Patches
{
	/// <summary>
	/// Harmony patcher class to wrap Watering-can class 
	///</summary>
	[HarmonyPatch(typeof(BlockWateringCan))]
	public class WateringCanDaptor
	{
		[HarmonyPrepare]
		private static bool DeduplicatePatching(MethodBase original, Harmony harmony)
		{

		if (original != null) {
		foreach (var patched in harmony.GetPatchedMethods( )) {
		if (patched.Name == original.Name) return false; //SKIPS PATCHING, its already there
		}
		}

		return true;//patch all other methods
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(BlockWateringCan.OnHeldInteractStep))]
		public static void OnHeldInteractStep(ref bool __result, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, BlockWateringCan __instance)
		{
		var coreAPI = byEntity.Api;
		#if DEBUG
		coreAPI.Logger.VerboseDebug("BlockWateringCanPlus::OnHeldInteractStep");
		#endif

		var wc = new WateringCanAssist(__instance, byEntity.Api);

		wc.PerformBlockCooling(secondsUsed, slot, byEntity, blockSel, entitySel);

		}


	}

	public class WateringCanAssist
	{
		private ICoreAPI CoreAPI { get; set; }
		private ICoreServerAPI ServerAPI { get { return CoreAPI as ICoreServerAPI; } }
		private ICoreClientAPI ClientAPI { get { return CoreAPI as ICoreClientAPI; } }
		private BlockWateringCan Original { get; set; }

		private const float coolRateDefault = 0.0075f;
		private const float flashPointTemp = 100f;

		private SimpleParticleProperties steamParticles = new SimpleParticleProperties {
			MinPos = new Vec3d( ),
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

		public readonly AssetLocation CoolSoundEffect = new AssetLocation(@"game", @"sounds/sizzle");

		public WateringCanAssist(BlockWateringCan original, ICoreAPI api)
		{
		this.CoreAPI = api;
		this.Original = original;
		}

		public void PerformBlockCooling(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{

		if (blockSel == null) return;
		if (byEntity.Controls.Sneak) return;

		if ((DateTime.Now.Millisecond / 100) % 2 == 1) return;

		BlockPos targetPos = blockSel.Position;

		if (!slot.Empty && Original.GetRemainingWateringSeconds(slot.Itemstack) >= 0.1f) {
		var server = (CoreAPI.World.Side.IsServer( ));
		var someBlock = CoreAPI.World.BlockAccessor.GetBlock(targetPos);

		if (someBlock != null
			&& someBlock.BlockMaterial == EnumBlockMaterial.Ceramic
			&& (someBlock.Class == @"BlockIngotMold" || someBlock.Class == @"BlockToolMold")) {
		var someBlockEntity = server ? ServerAPI.World.BlockAccessor.GetBlockEntity(targetPos) : ClientAPI.World.BlockAccessor.GetBlockEntity(targetPos);

		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Ok, its an Tool/Ingot mold.: {0}", someBlockEntity);
		#endif

		if (someBlockEntity is BlockEntityIngotMold) {
		var rightSide = AimAtRight(blockSel.HitPosition);
		var ingotMold = someBlockEntity as BlockEntityIngotMold;

		if (rightSide && (ingotMold.FillLevelRight > 0 && ingotMold.TemperatureRight > flashPointTemp)) {
		if (server) CoolContents(ingotMold.ContentsRight); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer);
		ingotMold.MarkDirty( );
		}
		else if (ingotMold.FillLevelLeft > 0 && ingotMold.TemperatureLeft > flashPointTemp) {
		if (server) CoolContents(ingotMold.ContentsLeft); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer);
		ingotMold.MarkDirty( );
		}
		return;
		}

		if (someBlockEntity is BlockEntityToolMold) {
		var toolMold = someBlockEntity as BlockEntityToolMold;
		if (toolMold.FillLevel > 0 && toolMold.Temperature > flashPointTemp) {
		if (server) CoolContents(toolMold.MetalContent); else GenerateSpecialEffects(blockSel.Position, blockSel.HitPosition, byEntity as EntityPlayer);
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

		steamParticles.MinPos = blockLoc.ToVec3d( ).AddCopy(aimPoint);
		steamParticles.AddPos = new Vec3d(0.05f, 0f, 0.05f);

		#if DEBUG
		CoreAPI.Logger.VerboseDebug("Generate steam particles");
		#endif

		ClientAPI.World.SpawnParticles(steamParticles, playerEntity.Player);
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