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
		return @this.MatterState == EnumMatterState.Gas || @this.BlockMaterial == EnumBlockMaterial.Air;
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

		public static string DiagonalInitial(this BlockPos pos, BlockPos other)
		{
		//Subtract, to Normal; then 'Cardinals'...
		var normal = pos.SubCopy(other);
		var diagonal = Cardinal.FromNormali(normal.ToVec3i( ));
		return diagonal?.Initial ?? "?";
		}
	}
}

