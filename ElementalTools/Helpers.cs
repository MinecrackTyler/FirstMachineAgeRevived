using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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

		public static bool KeyValueEndingMatch(this IDictionary<string, string> stringyDict, string key, string value)
		{
		if (stringyDict.ContainsKey(key)) {
		return stringyDict.Any(kvp => String.CompareOrdinal(kvp.Key, key) == 0
							   && kvp.Value.EndsWith(value, StringComparison.Ordinal) );
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

		/// <summary>
		/// Match against:Variant(s){   metal,	material  } == 'steel'
		/// </summary>
		/// <returns>The Steel metal. </returns>
		/// <param name="something">Something collectable.</param>
		public static bool IsSteelMetal(this CollectibleObject something)
		{
		return something.Variant.KeyValueEndingMatch(ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.SteelNameKey) ||
				 something.Variant.KeyValueEndingMatch(ElementalToolsSystem.MaterialNameKey, ElementalToolsSystem.SteelNameKey);
		}

		/// <summary>
		/// Using ItemSharpener class....
		/// </summary>
		/// <returns>If a sharpener.</returns>
		/// <param name="something">Something.</param>
		public static bool IsSharpener(this CollectibleObject something)
		{
			return String.Equals(something.Class, ElementalToolsSystem.sharpeningStoneItemKey, StringComparison.Ordinal);
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

		/// <summary>
		/// Has Edge that can wear down...
		/// </summary>
		/// <returns>The impliment.</returns>
		/// <param name="what">What.</param>
		public static bool EdgedImpliment(this EnumTool? what)
		{			
		if (what.HasValue && (
				what == EnumTool.Axe ||
				what == EnumTool.Chisel ||
				what == EnumTool.Hoe ||
				what == EnumTool.Knife ||
				what == EnumTool.Pickaxe ||
				what == EnumTool.Saw ||
				what == EnumTool.Scythe ||
				what == EnumTool.Shears ||
				what == EnumTool.Sickle ||
				what == EnumTool.Spear ||
				what == EnumTool.Sword)
			) {
		return true;
		}
		return false;
		}

		/// <summary>
		/// Consider this as Weaspon Vs. Tool..?
		/// </summary>
		/// <returns>The impliment.</returns>
		/// <param name="what">What.</param>
		public static bool Weapons(this EnumTool? what)
		{
		if (what.HasValue && (
				what == EnumTool.Axe || //Arguable
				what == EnumTool.Bow ||
				what == EnumTool.Knife ||
				what == EnumTool.Spear ||
				what == EnumTool.Sword)
			) {
		return true;
		}
		return false;
		}


		public static void NamePrefixed_RegisterItemClass(this ICoreAPICommon commonApi, Type itemType, string prefix)
		{
		//CoreAPI.RegisterItemClass(genericSteelItemKey, typeof(SteelWrap<Item>));
		string className = string.Format(prefix + "_" + itemType.GetGenericArguments( ).First( ).Name);
		
		commonApi.RegisterItemClass(className, itemType);
		}

		#if DEFUNCT
		//Why C# 7.0 ? WHY?!?!
		public static T GetEnum<T>(this ITreeAttribute treeAttr, string keyword, T defaultValue = default(T)) where T : struct// enum
		{
		var enumType = typeof(T);

		if (enumType.IsEnum) 
		{				
			var eSize = Marshal.SizeOf(Enum.GetUnderlyingType(enumType));

			byte[ ] buf = new byte[eSize];

			treeAttr.GetBytes(keyword, buf);

			switch(eSize)
			{
				case 1://byte
				return ( T )Enum.ToObject(enumType, buf[0]);

				case 2://short
				var temp = BitConverter.ToInt16(buf, 0);
				return ( T )Enum.ToObject(enumType, temp);

				case 4://int - word
				var temp2 = BitConverter.ToInt32(buf, 0);
				return ( T )Enum.ToObject(enumType, temp2);
									
				case 8://long - d.word
				var temp3 = BitConverter.ToInt64(buf, 0);
				return ( T )Enum.ToObject(enumType, temp3);

				default:
					throw new NotSupportedException("Not a supported size of Enumerator!");
			}
						
		}

			    
		return defaultValue;
		}


		public static void SetEnum<T>(this ITreeAttribute treeAttr, string key, T setValue) where T : struct// enum
		{
		var enumType = typeof(T);

		if (enumType.IsEnum) {
		var eSize = Marshal.SizeOf(Enum.GetUnderlyingType(enumType));
					
		byte[ ] buf = new byte[eSize];

		switch (eSize) {
			case 1://byte
			byte temp = ( byte )Convert.ChangeType(setValue, TypeCode.Byte);
			buf[0] = temp;
			break;

			case 2://short
			short temp2 = ( short )Convert.ChangeType(setValue, TypeCode.Int16);
			buf = BitConverter.GetBytes(temp2);
			break;

			case 4://int - word
			int temp3 = ( int )Convert.ChangeType(setValue, TypeCode.Int32);
			buf = BitConverter.GetBytes(temp3);
			break;

			case 8://long - d.word
			long temp4 = ( long )Convert.ChangeType(setValue, TypeCode.Int64);
			buf = BitConverter.GetBytes(temp4);			
			break;

		default:
			throw new NotSupportedException("Not a supported size of Enumerator!");
		}

		treeAttr.SetBytes(key, buf);
		}

		}
#endif
	}
}

