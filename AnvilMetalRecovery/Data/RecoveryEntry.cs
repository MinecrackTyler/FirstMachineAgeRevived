using System;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	public struct RecoveryEntry
	{
		public AssetLocation IngotCode;
		public uint Quantity;//Voxels

		public RecoveryEntry(AssetLocation ig, uint qty)
		{
		IngotCode = ig.Clone();
		Quantity = qty;
		}
	}
}

