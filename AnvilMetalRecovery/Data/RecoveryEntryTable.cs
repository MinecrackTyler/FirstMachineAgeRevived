using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vintagestory.API.Common;

namespace AnvilMetalRecovery;

public class RecoveryEntryTable : KeyedCollection<AssetLocation, RecoveryEntry>
{
	public ICollection<AssetLocation> Keys => Dictionary.Keys;

	protected override AssetLocation GetKeyForItem(RecoveryEntry item)
	{
		return item.CollectableCode;
	}

	public void AddReplace(RecoveryEntry entry)
	{
		if (Contains(entry.CollectableCode)) Remove(entry.CollectableCode);
		Add(entry);
	}

	public bool ContainsKey(AssetLocation code)
	{
		if (Dictionary == null || Dictionary.Keys == null) return false;
		return Dictionary.ContainsKey(code);
	}
}