using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace ElementalTools
{
	public static class Helpers
	{

		public static bool KeyValueMatch(this IDictionary<string, string> stringyDict, string key, string value) 
		{
		if (stringyDict.ContainsKey(key)) {
		return stringyDict.Any(kvp => String.CompareOrdinal(kvp.Key, key) == 0
							   && String.CompareOrdinal(kvp.Value, value) == 0);
		}

		return false;
		}

		/// <summary>
		/// Match against:Variant(s){   metal,	material  } == 'iron'
		/// </summary>
		/// <returns>The ferric metal.</returns>
		/// <param name="something">Something collectable.</param>
		public static bool IsFerricMetal(this CollectibleObject something)
		{
		return something.Variant.KeyValueMatch(ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.IronNameKey) ||
				 something.Variant.KeyValueMatch(ElementalToolsSystem.MaterialNameKey, ElementalToolsSystem.IronNameKey);
		}

		public static AssetLocation AppendPaths(this AssetLocation assetLoc, params string[ ] morePaths)
		{
		StringBuilder pathPile = new StringBuilder(assetLoc.Path);
		foreach (var addon in morePaths) {
		pathPile.AppendFormat("-{0}", addon);		
		}
		assetLoc.Path = pathPile.ToString( );
		return assetLoc.Clone();
		}

		/// <summary>
		/// Transmutes the by variant's keyword.
		/// </summary>
		/// <returns>Altered Code.</returns>
		/// <param name="originalAsset">Original asset.</param>
		/// <param name="keywords">Keys in Variant(s).</param>
		/// <param name="replacement">Replacement values for 1st matched key.</param>
		public static AssetLocation TransmuteByVariants(this RegistryObject originalAsset, string[ ] keywords, string replacement)
		{
		foreach (var key in keywords) 
		{
		if (originalAsset.Variant.ContainsKey(key)) 
		{
		return originalAsset.CodeWithVariant(key, replacement);		
		}	  	
		}
		return originalAsset.Code;
		}
	}
}

