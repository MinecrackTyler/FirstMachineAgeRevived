using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	public class RecoveryEntry
	{
		public readonly AssetLocation CollectableCode;
		public Dictionary<AssetLocation, uint> MaterialComposition;//e.g. 50x Ingot-Copper, 10x Ingot-Zinc...

		public AssetLocation PrimaryMaterial 
		{
			get {
			return MaterialComposition.First().Key;
			}
		}

		public uint FirstQuantity {
			get
			{
			return MaterialComposition.First( ).Value;
			}
		}

		public uint TotalQuantity 
		{
			get
			{
			return ( uint )MaterialComposition.Sum(mt => mt.Value);
			}
		}

		public bool MultiComponent 
		{
			get
			{
			return MaterialComposition.Count > 1;
			}
		}

		/// <summary>
		/// Initializes a singluar entry recovery entry for 1:1 items
		/// </summary>
		/// <param name="coll">Coll.</param>
		/// <param name="ingot">Ingot.</param>
		/// <param name="qty">Qty.</param>
		public RecoveryEntry(AssetLocation coll, AssetLocation ingot, uint qty)
		{
		CollectableCode = coll.Clone();
		MaterialComposition = new Dictionary<AssetLocation, uint>( );
		MaterialComposition.Add(ingot, qty);
		}

		/// <summary>
		/// Initializes a singluar entry recovery entry for 1:2 items
		/// </summary>
		public RecoveryEntry(AssetLocation coll, AssetLocation ingotA, uint qtyA,AssetLocation ingotB, uint qtyB)
		{
		CollectableCode = coll.Clone( );
		MaterialComposition = new Dictionary<AssetLocation, uint>( );
		MaterialComposition.Add(ingotA, qtyA);
		MaterialComposition.Add(ingotB, qtyB);
		}
	}
}

