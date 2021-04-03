using System;
using System.Collections.Generic;
using System.Linq;


using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public class BlockBehaviorNeedSides : BlockBehavior
	{
		public EnumBlockMaterial[ ] ApplicableMaterials { get; private set; }

		public BlockBehaviorNeedSides(Block block) : base(block)
		{}	


		protected bool CheckCardinalsOk(IBlockAccessor world, BlockPos checkPos)
		{
		//Visit all cardinals, mabey diagonal support?
		Stack<Cardinal> directions = new Stack<Cardinal>( );

		directions.Push(Cardinal.North);
		directions.Push(Cardinal.East);
		directions.Push(Cardinal.West);
		directions.Push(Cardinal.South);
					
		while (directions.Count > 0) {
		var direction = directions.Pop( );
		BlockPos probePos = checkPos.AddCopy(direction.Normali);
		Block toCheck = world.GetBlock(probePos);
			if (toCheck.BlockMaterial != EnumBlockMaterial.Air && ApplicableMaterials.Any(am => am == toCheck.BlockMaterial)) 
			{
			var counterFace = BlockFacing.FromCode(direction.Opposite.Code);
				if (toCheck.SideSolid[counterFace.Index]) return true;
			}
		}

		return false;
		}

		#region Overrides

		public override void Initialize(JsonObject properties)
		{
		base.Initialize(properties);
		
		this.ApplicableMaterials = properties[@"applicableMaterials"].FromEnumStrings<EnumBlockMaterial>();
		
		}

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
		{
		handling = EnumHandling.PreventDefault;
		//Got sides?
		if (CheckCardinalsOk(world.BlockAccessor, blockSel.Position.Copy( ))) 
		{
			handling = EnumHandling.PassThrough;
			return true;
		}
		else
		{
		failureCode = @"requirehorizontalside";
		}

		return true;
		}

		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
		{
		handling = EnumHandling.PassThrough;

			if (!CheckCardinalsOk(world.BlockAccessor, pos.Copy( ))) 
			{		
			world.BlockAccessor.BreakBlock(pos, null,0);
			}

		base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
		}

		#endregion
	}
}

