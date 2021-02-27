using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class CollapsingBlock : Block
	{
		private readonly Cuboidf[ ] collapseZones = { new Cuboidf(0.06f, 0.1f, 0.1f, 0.937f, 0.25f, 1.0f) };


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
		var victems = localAcc.GetIntersectingEntities(here.UpCopy( ), collapseZones, (e) => { return true;});//TOP 'Surface' - excluding edges

		foreach (var entity in victems) 
		{
			if (entity.IsInteractable &&
			entity.Properties.HitBoxSize.X >= 0.5 &&
			entity.Properties.HitBoxSize.Y >= 0.5) {
			#if DEBUG
			ServerAPI.Logger.VerboseDebug($"Collision-check:  ({entity.Code}) is large enough to trigger COLLAPSE!");
			#endif
			enough = true;
			break;
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
		//Start to shake...with particles , dust, creaking sounds
		#if DEBUG
		ServerAPI.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");
		#endif

		//Tick Callback; in 200ms...
		ServerAPI.World.RegisterCallbackUnique(MabeyCollapse, pos.Copy() ,50);		
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
		//Dust
		/*
		byPlayer.Entity.World.SpawnParticles(new SimpleParticleProperties( ) {
			MinQuantity = 0,
			AddQuantity = 10,
			Color = ColorUtil.ToRgba(128, 128, 128, 64),
			MinPos = new Vec3d(posx + faceVec.X * 0.01f, posy + faceVec.Y * 0.01f, posz + faceVec.Z * 0.01f),
			AddPos = new Vec3d(0, 0, 0),
			MinVelocity = new Vec3f(
				   4 * faceVec.X,
				   4 * faceVec.Y,
				   4 * faceVec.Z
			   ),
			AddVelocity = new Vec3f(
				   8 * (( float )rnd.NextDouble( ) - 0.5f),
				   8 * (( float )rnd.NextDouble( ) - 0.5f),
				   8 * (( float )rnd.NextDouble( ) - 0.5f)
			   ),
			LifeLength = 0.025f,
			GravityEffect = 0f,
			MinSize = 0.03f,
			MaxSize = 0.4f,
			ParticleModel = EnumParticleModel.Quad,
			VertexFlags = 200,
			SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
		}, byPlayer);
		*/
		}

		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}

		#endregion


	}
}