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
		


		/*
		EntityPlayer : EntityHumanoid
		EntityHumanoid : EntityAgent
		EntityPlayerBot : EntityAnimalBot
		EntityAnimalBot : EntityAgent
		EntityAgent
		 */
		public CollapsingBlock( )
		{

		}

		public ICoreServerAPI ServerAPI {
			get { return this.api as ICoreServerAPI; }
		}


		//TODO: Fall apart if player tries to 'Hoe' 'Dig' or mess with block in any way, or even place things on top of it...

		public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
		{
		//Get Ready to CRUMBLE!
		ServerAPI.Logger.VerboseDebug($"OnEntityInside ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos}");
		}

		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		//Start to shake...with particles , dust, creaking sounds
		ServerAPI.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");

		//Tick Callback; in 200ms...
		world.RegisterCallbackUnique(MabeyCollapse, pos ,200);		
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{

		if (!String.IsNullOrEmpty(this.Cover) && !String.IsNullOrEmpty(this.Material)) {
		return Lang.Get(GlobalConstants.DefaultDomain + $":block-{this.Cover}-{this.Material}" );
		}

		return @"Error?";
		}

		private void MabeyCollapse(IWorldAccessor localAcc, BlockPos here, float delay)
		{
		//Check 'Volumne' of Bounding box; any 'large' entity still standing here is going to be surprised!
		//hitboxSize: { x: 0.6, y: 1.85 } > Seraph
		//hitboxSize: { x: 0.85, y: 0.5 }> Pup
		//hitboxSize: { x: 0.4, y: 0.3 } > lil'Bunny

		bool enough = false;
		var victems = localAcc.GetEntitiesInsideCuboid(here, here.UpCopy( ));

			foreach (var entity in victems) {
			if (entity is EntityAgent &&
				entity.Properties.HitBoxSize.X >= 0.5 &&
				entity.Properties.HitBoxSize.Y >= 0.5  )
				{
				ServerAPI.Logger.VerboseDebug($"MabeyCollapse:  ({entity.Code}) is large enough to trigger COLLAPSE!");
				enough = true;
				break;
				}
		}
		
		if (enough) {		
		localAcc.BlockAccessor.BreakBlock(here, null, 0);
		}

		}


		private string Material
		{
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


	}
}