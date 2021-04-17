using System;

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
		if (itemStack.Attributes.HasAttribute(@"durability"))
			return itemStack.Attributes.GetInt(@"durability", itemStack.Item.Durability);
		else return null;
		}

	}
}

