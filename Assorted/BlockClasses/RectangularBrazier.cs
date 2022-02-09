using System;
using System.Collections;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class RectangularBrazier : Block
	{
		private const string stateKey = @"state";
		private const string fuledValue = @"fueled";
		private const string extinguishedValue = @"extinguished";
		private const string litValue = @"lit";
		private const float ignitionTime = 2f;
		private const int fuelReqt = 5;
		private const float fireDmg = 0.5f;
		private const int fuel_Temp = 999;
		private const int fuel_Duration = 30;

		private WorldInteraction[ ] interactMustFuel, interactMustIgnight;
		private string flameoutPercentKey = @"flameoutPercent";
		private WeatherSystemBase weatherSys;

		public override void OnLoaded(ICoreAPI api)
		{
		base.OnLoaded(api);

		weatherSys = api.ModLoader.GetModSystem<WeatherSystemBase>( );

		if (api.Side.IsClient( )) 
			{
			var clientAPI = api as ICoreClientAPI;
							
			List<ItemStack> solidFuels = new List<ItemStack>();
			List<ItemStack> ignitionSources = new List<ItemStack>();

			foreach (CollectibleObject obj in api.World.Collectibles) {
			if (obj.CombustibleProps?.BurnTemperature >= fuel_Temp && obj.CombustibleProps?.BurnDuration >= fuel_Duration) 
			{
				List<ItemStack> stacks = obj.GetHandBookStacks(clientAPI);
				if (stacks != null) solidFuels.AddRange(stacks);
			}

			if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>( )) {
				List<ItemStack> stacks = obj.GetHandBookStacks(clientAPI);
				if (stacks != null) ignitionSources.AddRange(stacks);
				}
			}


			interactMustFuel = new WorldInteraction[ ] {
					new WorldInteraction {
						ActionLangCode = "blockhelp-bloomery-fuel",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = solidFuels.ToArray(),
					}
			};

			interactMustIgnight= new WorldInteraction[ ] {
					new WorldInteraction {
						ActionLangCode = "blockhelp-bloomery-ignite",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = ignitionSources.ToArray(),
					}
			};
			}
		}

		public bool Fueled 
		{
			get
			{
			return this.Variant[stateKey].Equals(fuledValue, StringComparison.Ordinal);
			}
		}

		public bool Lit {
			get
			{
			return this.Variant[stateKey].Equals(litValue, StringComparison.Ordinal);
			}
		}

		public float FlameoutPercent {
			get
			{
			return this.Attributes[flameoutPercentKey].AsFloat(0.01f);
			}
		}

		public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
		{
			if (Fueled) 
			{	
				return secondsIgniting > ignitionTime ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
			}

			return EnumIgniteState.NotIgnitablePreventDefault;
		}

		public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
		{
			if (secondsIgniting < ignitionTime) return;

			handling = EnumHandling.PreventDefault;

			if (api.Side.IsServer()) {
				var litBlock = api.World.GetBlock(CodeWithVariant(stateKey, litValue));
				if (litBlock != null) {
				api.World.BlockAccessor.ExchangeBlock(litBlock.BlockId, pos);
				api.World.BlockAccessor.MarkBlockDirty(pos);
				}
			}
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
		ItemStack hbItemStack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

			if ((!Fueled && !Lit) 
			    && hbItemStack != null 
			    && hbItemStack.Class == EnumItemClass.Item 
			    && hbItemStack.Item?.CombustibleProps.BurnTemperature >= fuel_Temp && hbItemStack.Item?.CombustibleProps.BurnDuration >= fuel_Duration) 
			{		
			if (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) {
				if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize >= fuelReqt) {
					if (api.Side.IsServer( )) {
					byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(fuelReqt);
					byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty( );

					Block fuledBlock = world.GetBlock(CodeWithVariant(stateKey, fuledValue));
					world.BlockAccessor.ExchangeBlock(fuledBlock.BlockId, blockSel.Position);
					world.BlockAccessor.MarkBlockDirty(blockSel.Position);
					}
				return true;
				}
				else {
				//Not enough fuel		
				(api as ICoreClientAPI)?.TriggerIngameError(this, @"lackfuel", Lang.Get("defensive:ingameerror-lackfuel"));
				}
			}							
			}
		return false;
		}

		public override bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
		{
		return false;
		}

		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		#if DEBUG
		api.Logger.VerboseDebug("Brazier Collision; Face:{0} Impact: {1} |{2}, OG:{3}", facing, isImpact, entity.Class, entity.OnGround);
		#endif

		if (world.Side.IsServer() && Lit && facing == BlockFacing.UP) 
			{
				if (entity is EntityAgent && entity.Alive) 
				{					
				entity.ReceiveDamage(new DamageSource( ) { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.Fire, SourcePos = pos.ToVec3d( ), DamageTier = 5, KnockbackStrength = 0.25f }, fireDmg);
				if (Sounds?.Inside != null)	world.PlaySoundAt(Sounds.Inside, entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
				}
			}
		}

		public override WorldInteraction[ ] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
		if (!Fueled && !Lit) {
		//Add Fuel
		return interactMustFuel;
		}
		else if (Fueled && !Lit) {
		//Add Ignition
		return interactMustIgnight;
		}

		return null;//Must be burning
		}


		public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
		{
		extra = null;

		if (Lit && DateTime.Now.Second == 0) { 			
			var rainLevel = world.BlockAccessor.GetRainMapHeightAt(pos);
			if (pos.Y >= rainLevel) { //Brr, its Wet out here!
			var rainPos = new BlockPos(pos.X, rainLevel, pos.Z);
			var precip = weatherSys.GetPrecipitation(pos.ToVec3d());

			if (precip >= 0.3) { return true; }
			if (offThreadRandom.NextDouble( ) <= (FlameoutPercent * 10) ) { return true; }			
			}
			else if (offThreadRandom.NextDouble( ) <= FlameoutPercent){ return true; }
			}

		return false;
		}

		public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
		{
			if (Lit) {			
				#if DEBUG
				api.Logger.VerboseDebug("Got server-game tick for flameout! @ {0}", pos);
				#endif

				Block extinctBlock = world.GetBlock(CodeWithVariant(stateKey, extinguishedValue));
				world.BlockAccessor.ExchangeBlock(extinctBlock.BlockId, pos);
				world.BlockAccessor.MarkBlockDirty(pos);
				}			
		}
	}
}

