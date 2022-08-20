using System;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	public struct RecoveryEntry
	{
		public readonly AssetLocation CollectableCode;
		public AssetLocation IngotCode;
		/// <summary>
		/// Metal Quantity (VOXELS)
		/// </summary>
		public uint Quantity;
		public float Melting_Duration;//TODO: Replace with 1.17 MetalAlloy mappings
		public int Melting_Point;//TODO: Replace with 1.17 MetalAlloy mappings

		public RecoveryEntry(AssetLocation coll, AssetLocation ingot, uint qty, float dur, int point)
		{
		CollectableCode = coll.Clone();
		IngotCode = ingot.Clone();
		Quantity = qty;
		Melting_Duration = dur;
		Melting_Point = point;
		}
	}
}

