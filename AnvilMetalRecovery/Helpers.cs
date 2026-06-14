using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace AnvilMetalRecovery;

internal static class Helpers
{
	internal static void AddBlockBehavior(this ICoreAPI coreAPI, AssetLocation assetName, string behaviorCode,
		Type blockBehaviorType)
	{
		if (assetName.Valid && !assetName.IsWildCard)
		{
			var targetBlock = coreAPI.World.GetBlock(assetName);
			var newBlockBehavior = coreAPI.ClassRegistry.CreateBlockBehavior(targetBlock, behaviorCode);

			if (targetBlock != null && newBlockBehavior != null)
			{
				targetBlock.BlockBehaviors = targetBlock.BlockBehaviors.Append(newBlockBehavior);
				targetBlock.CollectibleBehaviors = targetBlock.CollectibleBehaviors.Append(newBlockBehavior);
			}
			else
			{
				coreAPI.Logger.Warning(
					$"Could not append new BLOCK BEHAVIOR ({blockBehaviorType.Name}): '{behaviorCode}' to block [{assetName}]!");
			}
		}
	}

	internal static void AddCollectableBehavior(this ICoreAPI coreAPI, AssetLocation assetName, string behaviorCode,
		Type blockBehaviorType)
	{
		if (assetName.Valid && !assetName.IsWildCard)
		{
			var targetBlock = coreAPI.World.GetBlock(assetName);
			var newCollectableBehavior = coreAPI.ClassRegistry.CreateCollectibleBehavior(targetBlock, behaviorCode);

			if (targetBlock != null && newCollectableBehavior != null)
				targetBlock.CollectibleBehaviors = targetBlock.CollectibleBehaviors.Append(newCollectableBehavior);
			else
				coreAPI.Logger.Warning(
					$"Could not append new COLLECTABLE BEHAVIOR ({blockBehaviorType.Name}): '{behaviorCode}' to something [{assetName}]!");
		}
	}

	internal static void ReplaceBlockEntityType(this ClassRegistry registry, string className, Type blockentity)
	{
		if (registry.blockEntityClassnameToTypeMapping.ContainsKey(className))
		{
			//replace it
			registry.blockEntityClassnameToTypeMapping[className] = blockentity;
			registry.blockEntityTypeToClassnameMapping[blockentity] = className;
		}
	}

	internal static void ReplaceItemClassType(this ClassRegistry registry, string className, Type replacer)
	{
		if (registry.ItemClassToTypeMapping.ContainsKey(className))
			//replace it
			registry.ItemClassToTypeMapping[className] = replacer;
	}

	internal static void ReplaceBlockClassType(this ClassRegistry registry, string className, Type replacer)
	{
		if (registry.BlockClassToTypeMapping.ContainsKey(className))
			//replace it
			registry.BlockClassToTypeMapping[className] = replacer;
	}

	internal static int? Hitpoints(this ItemStack itemStack)
	{
		if (itemStack == null || itemStack.Attributes == null) return null;
		if (itemStack.Attributes.HasAttribute(@"durability"))
			return itemStack.Attributes.GetInt(@"durability", itemStack.Item.Durability);
		return null;
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

	internal static string PathEnding(this AssetLocation inputCode)
	{
		return inputCode.Path.Split('-').Last();
	}

	internal static AssetLocation AppendPathVariant(this AssetLocation toAppend, string append)
	{
		var appendedCode = toAppend.Clone();
		appendedCode.Path += "-" + append;
		return appendedCode;
	}

	internal static bool NothingNull(params object[] parameters)
	{
		return parameters.All(parm => parm != null);
	}

	internal static string AnyKeys(this RelaxedReadOnlyDictionary<string, string> source, params string[] keys)
	{
		foreach (var key in keys)
			if (source.ContainsKey(key))
				return source[key];
		return string.Empty;
	}
}