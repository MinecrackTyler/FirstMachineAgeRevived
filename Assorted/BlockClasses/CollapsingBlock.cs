using System;
using System.Collections.Generic;
using System.Threading;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;


namespace FirstMachineAge
{
	public class CollapsingBlock : Block
	{
		private readonly Cuboidf[ ] collapseZones = { new Cuboidf(0.1f, 0.1f, 0.1f, 0.9f, 0.25f, 0.9f) };


		private void MabeyCollapse(IWorldAccessor localAcc, BlockPos here, float delay)
		{
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("MabeyCollapse?");
		#endif
		//Check 'Volumne' of Bounding box; any 'large' entity still standing here is going to be surprised!
		//hitboxSize: { x: 0.6, y: 1.85 } > Seraph
		//hitboxSize: { x: 0.85, y: 0.5 }> Pup
		//hitboxSize: { x: 0.4, y: 0.3 } > lil'Bunny

		bool enough = false;
		var victems = localAcc.GetIntersectingEntities(here.UpCopy( ), collapseZones, (e) => { return (e is EntityAgent); } );//TOP 'Surface' - excluding edges
		if (victems.Length > 2) { enough = true; }
		else {
				foreach (var entity in victems) 
				{
					if (entity.IsInteractable &&
					entity.Properties.HitBoxSize.X >= 0.51 &&
					entity.Properties.HitBoxSize.Y >= 0.51) 
					{
					#if DEBUG
					ServerAPI.Logger.VerboseDebug($"Collision box ( W{collapseZones[0].Width} H{collapseZones[0].Height} L{collapseZones[0].Length})");
					ServerAPI.Logger.VerboseDebug($"Collision-check:  ({entity.Code}) is large enough to trigger COLLAPSE!");
					#endif
					enough = true;
					break;
					}
				}
			}

		if (enough) {
		localAcc.BlockAccessor.BreakBlock(here.Copy(), null);
		//TODO: Sound & Dust
		
		}

		}


		private string Material {
			/*
			{ code: "cover",  states: [ "sand","gravel" ] },
			{ code: "material", loadFromProperties: "block/rock", combine: "SelectiveMultiply", onVariant: "cover" },
			{ code: "alt_cover", states: [ "soil" ], combine: "Add" },
			*/
			get
			{
			return this.Variant[@"material"];
			}
		}

		private string Cover {
			/*
			{ code: "cover",  states: [ "sand","gravel" ] },
			{ code: "material", loadFromProperties: "block/rock", combine: "SelectiveMultiply", onVariant: "cover" },
			{ code: "alt_cover", states: [ "soil" ], combine: "Add" },
			*/
			get
			{
			return this.Variant[@"cover"];
			}
		}

		public ICoreServerAPI ServerAPI {
			get { return this.api as ICoreServerAPI; }
		}

		#region Overrides



		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		if (api.Side.IsClient( )) return;
		if (!(entity is EntityAgent)) return;
		//Start to shake...with particles , dust, creaking sounds
		#if DEBUG
		ServerAPI.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");
		#endif

		//Tick Callback; in 200ms...
		ServerAPI.World.RegisterCallbackUnique(MabeyCollapse, pos.Copy() ,50);		
		}

		public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
		{
		if (api.Side.IsClient( )) return;
		if (!(entity is EntityAgent)) return;
		//Start to shake...with particles , dust, creaking sounds
		#if DEBUG
		ServerAPI.Logger.VerboseDebug($"OnEntityInside ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} ");
		#endif

		//Tick Callback; in 200ms...
		ServerAPI.World.RegisterCallbackUnique(MabeyCollapse, pos.Copy( ), 50);
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{

		if (!String.IsNullOrEmpty(this.Cover) && !String.IsNullOrEmpty(this.Material)) {
		return Lang.Get(GlobalConstants.DefaultDomain + $":block-{this.Cover}-{this.Material}" );
		}

		return @"Error?";
		}

		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{
		if (api.Side.IsClient( )) return;

		if (neibpos.Above(pos)) {		
			 world.BlockAccessor.BreakBlock(pos, null,0);
		}

		base.OnNeighbourBlockChange(world, pos, neibpos);
		}

		public override bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
		{
		return false;
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
		if (api.Side.IsClient( )) {
		//Bits of broken block

		var particleProps = new SimpleParticleProperties(1, 3, this.GetRandomColor(api as ICoreClientAPI, pos.Copy( ), BlockFacing.UP), new Vec3d( ), new Vec3d( ), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1, 1, 0.1f, 0.3f, EnumParticleModel.Quad);
		particleProps.AddPos.Set(1.4, 1.4, 1.4);
		particleProps.AddQuantity = 20;
		particleProps.MinVelocity.Set(-0.25f, 0, -0.25f);
		particleProps.AddVelocity.Set(0.5f, 1, 0.5f);
		particleProps.WithTerrainCollision = true;
		particleProps.ParticleModel = EnumParticleModel.Cube;
		particleProps.LifeLength = 1.5f;
		particleProps.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f);
		particleProps.GravityEffect = 2.5f;
		particleProps.MinSize = 0.5f;
		particleProps.MaxSize = 1.5f;

		byPlayer.Entity.World.SpawnParticles(particleProps, byPlayer);
		
		}

		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}

		#endregion


	}
}