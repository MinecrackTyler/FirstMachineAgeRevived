using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ConstructionSupport
{
	
	public static class BlockHelpers
	{

		public static bool IsSolid(this Block @this )
		{
		return @this.MatterState == EnumMatterState.Solid;
		}

		public static bool IsGaseous(this Block @this)
		{
		return @this.BlockMaterial == EnumBlockMaterial.Air || @this.MatterState == EnumMatterState.Gas;
		}

		public static bool Above(this BlockPos pos, BlockPos other)
		{
		if (pos.UpCopy( ) == other.Copy( )) return true;

		return false;
		}

		public static bool OnSide(this BlockPos pos, BlockFacing side, BlockPos other)
		{
		if (pos.AddCopy(side) == other.Copy( )) return true;

		return false;
		}
	}
}

