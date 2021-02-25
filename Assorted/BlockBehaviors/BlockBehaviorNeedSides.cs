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
		//Visit all cardinals
		Stack<BlockPos> checkList = new Stack<BlockPos>( );

		checkList.Push(checkPos.NorthCopy());
		checkList.Push(checkPos.EastCopy());
		checkList.Push(checkPos.WestCopy());
		checkList.Push(checkPos.SouthCopy());
					
		while (checkList.Count > 0) {
		Block toCheck = world.GetBlock(checkList.Pop( ));
		if (toCheck.BlockMaterial != EnumBlockMaterial.Air 
				|| ApplicableMaterials.Any( am => am == toCheck.BlockMaterial ) ) return true;
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
		//handling = EnumHandling.PreventDefault;
		//Got sides?
		if (CheckCardinalsOk(world.BlockAccessor, blockSel.Position.Copy( ))) 
		{ return true; }
		else
		{
		failureCode = @"requirehorizontalattachable";
		}

		return false;
		}

		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
		{

		}

		#endregion
	}
}

