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

		public static bool Above(this BlockPos pos, BlockPos other)
		{
		if (pos.UpCopy( ) == other.Copy( )) return true;

		return false;
		}
	
	}
}

