using System;
using System.Collections.Generic;

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
		private readonly Cuboidf[ ] collapseZones = { new Cuboidf(0.1f, 0.1f, 0.0f, 0.95f, 0.95f, 0.25f) };


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
		var victems = localAcc.GetIntersectingEntities(here.UpCopy( ), collapseZones);//TOP 'Surface' - excluding edges
		//var victems = localAcc.GetEntitiesInsideCuboid(here.AddCopy(-1,-1,-1),here.AddCopy(1,1,1));//TOP 'Surface' - excluding edges

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
		localAcc.BlockAccessor.BreakBlock(here, null, 0);
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

		//TODO: Fall apart if player tries to 'Hoe' 'Dig' or mess with block in any way, or even place things on top of it...
		public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
		{
		if (api.Side.IsClient()) return;

		//Get Ready to CRUMBLE!
		#if DEBUG
		ServerAPI.Logger.VerboseDebug($"OnEntityInside ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos}");
		#endif
		}

		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		if (api.Side.IsClient( )) return;
		//Start to shake...with particles , dust, creaking sounds
		#if DEBUG
		ServerAPI.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");
		#endif

		//Tick Callback; in 200ms...
		ServerAPI.World.RegisterCallbackUnique(MabeyCollapse, pos ,100);		
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

		#endregion


	}
}