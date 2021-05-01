using System;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace AnvilMetalRecovery
{
	internal static class Helpers
	{
		internal static void ReplaceBlockEntityType(this ClassRegistry registry, string className, Type blockentity)
		{
		if (registry.blockEntityClassnameToTypeMapping.ContainsKey(className)) {
		//replace it
		registry.blockEntityClassnameToTypeMapping[className] = blockentity;
		registry.blockEntityTypeToClassnameMapping[blockentity] = className;
		}
		}

		internal static void ReplaceItemClassType(this ClassRegistry registry, string className, Type replacer)
		{
		if (registry.ItemClassToTypeMapping.ContainsKey(className)) {
		//replace it
		registry.ItemClassToTypeMapping[className] = replacer;
		}
		}

		internal static int? Hitpoints(this ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return null;
		if (itemStack.Attributes.HasAttribute(@"durability"))
			return itemStack.Attributes.GetInt(@"durability", itemStack.Item.Durability);
		else return null;
		}

		internal static void Hitpoints(this ItemStack itemStack, int number)
		{
		if (itemStack.Attributes.HasAttribute(@"durability"))
			 itemStack.Attributes.SetInt(@"durability", number);		
		}

		internal static bool BeginingOnly(this AssetLocation checkCode, string term)
		{			
			return checkCode.Valid && checkCode.Path.Split('-').First().Equals(term, StringComparison.OrdinalIgnoreCase);
		}

		internal static AssetLocation AppendPathVariant(this AssetLocation toAppend, string append)
		{
		var appendedCode = toAppend.Clone( );
		appendedCode.Path += ("-" + append);
		return appendedCode;
		}
	}
}

