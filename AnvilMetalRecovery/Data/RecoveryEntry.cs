using System;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	public struct RecoveryEntry
	{
		public AssetLocation IngotCode;
		public uint Quantity;//IN: Voxels
		public float Melting_Duration;
		public int Melting_Point;

		public RecoveryEntry(AssetLocation ig, uint qty, float dur, int point)
		{
		IngotCode = ig.Clone();
		Quantity = qty;
		Melting_Duration = dur;
		Melting_Point = point;
		}
	}
}

