using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery.Patches
{

	/// <summary>
	/// Harmony patcher class to detect Item (CollectableObject) Hitpoint damage and generate Destruction events from 'Damage' method calls
	/// </summary>
	[HarmonyPatch(typeof(CollectibleObject))]
	public class GenericItemMortalityDetector
	{

		[HarmonyPrepare]
		private static bool DeduplicatePatching(MethodBase original, Harmony harmony)
		{
		if (original != null) 
		{
			foreach (var patched in harmony.GetPatchedMethods( )) 
			{
			if (patched.Name == original.Name) return false; //SKIPS PATCHING, its already there
			}
		}

		return true;//patch all other methods
		}



		[HarmonyPrefix]
		[HarmonyPatch(nameof(CollectibleObject.DamageItem))]
		private static void Prefix_DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance)//Object __state
		{
		if (world.Api.Side.IsClient( )) return;
		#if DEBUG
		world.Api.Logger.VerboseDebug("Prefix_DamageItem: {0} by {1}", __instance.Code, amount);
		#endif
		if (DamageFilterTool.Ignore(world, __instance)) return;
		#if DEBUG
		world.Api.Logger.VerboseDebug("InventoryID: {0}, Class: {1}", itemslot?.Inventory?.InventoryID, itemslot?.Inventory?.ClassName);//Class: hotbar
		world.Api.Logger.VerboseDebug("Thing has HP: {0}", itemslot.Itemstack.Hitpoints( ));
		#endif
		if (itemslot.Itemstack.Hitpoints( ) <= amount && itemslot.Inventory != null) 
			{
			#if DEBUG
			world.Api.Logger.VerboseDebug("Sending Item Expiry Event");
			#endif
			var playerEntity = byEntity as EntityPlayer;
			var hotbarEvent = new HotbarObserverData(itemslot.Inventory.InventoryID, itemslot.Inventory.GetSlotId(itemslot), __instance.Code, (playerEntity == null ? String.Empty : playerEntity.PlayerUID));
			world.Api.Event.PushEvent(MetalRecoverySystem.ItemDamageChannelName, hotbarEvent);
			}
		}

		/// <summary>
		/// Specialized Multitool for Setting / Getting, Checking Item-Filter list; and Ignore non-Items
		/// </summary>
		internal static class DamageFilterTool
		{

			public static bool Ignore(IWorldAccessor world, CollectibleObject that)
			{
			if (that.ItemClass != EnumItemClass.Item || that.Durability <= 1) {
			return true;
			}

			Dictionary<AssetLocation, RecoveryEntry> itemToVoxelLookup = ( Dictionary<AssetLocation, RecoveryEntry> )world.Api.ObjectCache[MetalRecoverySystem.itemFilterListCacheKey];

			if (itemToVoxelLookup.ContainsKey(that.Code)) return false;

			return true;
			}

		}
	}
}

