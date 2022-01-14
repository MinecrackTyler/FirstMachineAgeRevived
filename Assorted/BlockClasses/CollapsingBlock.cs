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
					entity.Properties.CollisionBoxSize.X >= 0.51 &&
					entity.Properties.CollisionBoxSize.Y >= 0.51) 
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
		ServerAPI.World.RegisterCallback(MabeyCollapse, pos.Copy() ,50);		
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
		ServerAPI.World.RegisterCallback(MabeyCollapse, pos.Copy( ), 50);
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

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
		{
		if (api.Side.IsClient( )) {
		var capi = api as ICoreClientAPI;

		//int color = capi.BlockTextureAtlas.GetRandomColor(this.TextureSubIdForBlockColor);

		//Bits of broken block
		var particleProps = new SimpleParticleProperties
		(7, 12, 
		0x808080, 
		pos.ToVec3d().Add(0.1, 0.75, 0.1),
		pos.ToVec3d().Add(0.7, 0.0, 0.7),
		Vec3f.Zero,
		Vec3f.Zero,
		5, //life length
		0.8f, //gravity effect 
		0.25f, 0.7f, //min size, max size
		EnumParticleModel.Cube); // quad or cube

		
		particleProps.ShouldSwimOnLiquid = true;		
		particleProps.WithTerrainCollision = true;
		particleProps.WindAffected = false;
		

		capi.World.SpawnParticles(particleProps);
		//TODO: Sound ?

		}

		base.OnBlockRemoved(world, pos);
		}

		#endregion


	}
}