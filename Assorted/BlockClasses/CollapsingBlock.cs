using System;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
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

		public ICoreClientAPI ClientAPI 
		{
			get { return this.api as ICoreClientAPI; }
		}




		//TODO: Fall apart if player tries to 'Hoe' 'Dig' or mess with block in any way, or even place things on top of it...

		public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
		{
		//Get Ready to CRUMBLE!
		api.World.Logger.VerboseDebug($"OnEntityInside ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos}");
		}

		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		//Start to shake...with particles , dust, creaking sounds
		api.World.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");

		//Tick Callback; in 200ms...
		api.Event.RegisterCallback(CheckOwnVolume, pos ,200);		
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{

		if (!String.IsNullOrEmpty(this.Cover) && !String.IsNullOrEmpty(this.Material)) {
		return Lang.Get(GlobalConstants.DefaultDomain + $":block-{this.Cover}-{this.Material}" );
		}

		return @"Error?";
		}

		private void CheckOwnVolume(IWorldAccessor localAcc, BlockPos here, float delay)
		{
		//Check 'Volumne' of Bounding box; any 'large' entity still standing here is going to be surprised!
		

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