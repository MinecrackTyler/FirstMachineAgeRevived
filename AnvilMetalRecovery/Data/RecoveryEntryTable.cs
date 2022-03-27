using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	public class RecoveryEntryTable : KeyedCollection<AssetLocation, RecoveryEntry>
	{
		protected override AssetLocation GetKeyForItem(RecoveryEntry item) => item.CollectableCode;

		public void AddReplace(RecoveryEntry entry)
		{
		if (Contains(entry.CollectableCode)) { Remove(entry.CollectableCode); }
		Add(entry);
		}

		public ICollection<AssetLocation> Keys 
		{
			get
			{
			return this.Dictionary.Keys;
			}
		}

		public bool ContainsKey(AssetLocation code)
		{
		if (this.Dictionary == null || this.Dictionary.Keys == null ) return false;
		return this.Dictionary.ContainsKey(code);
		}
	}
}

