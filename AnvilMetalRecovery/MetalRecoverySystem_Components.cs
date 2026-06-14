using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace AnvilMetalRecovery;

public partial class MetalRecoverySystem : ModSystem
{
	private void UnravelMetalProperties()
	{
		MetalProperties = new Dictionary<string, MetalInfo>();
		var metalAsset = ServerCore.Assets.TryGet("game:worldproperties/block/metal.json");
		var originalJson = JObject.Parse(metalAsset.ToText());
		var variants = originalJson.SelectToken(@"variants").Children().ToList();

		foreach (var variant in variants)
		{
			var metalI = variant.ToObject<MetalInfo>();

			if (!MetalProperties.ContainsKey(metalI.Code)) MetalProperties.Add(metalI.Code, metalI);

#if DEBUG
			Mod.Logger.Debug(
				$"parsed '{metalI.Code}' T:{metalI.Tier}, {(metalI.Elemental ? "Element" : "Alloy")} Melt:{metalI.MeltingPoint}℃");
#endif
		}
	}

	/// <summary>
	///     Materials the data gathering.
	/// </summary>
	/// <remarks>
	///     Sum, tally Voxels in smithing recipes for all 'metal'-ingot derived items
	/// </remarks>
	/// <returns>The data gathering.</returns>
	private void MaterialDataGathering()
	{
		//TODO: FIX 3rd-PARTY MOD COMPATIBILITY !!!		
		var examineList = SmithingRecipies.Where(sr =>
			sr.Enabled && sr.Ingredient.Type == EnumItemClass.Item && sr.Output.Type == EnumItemClass.Item);

		foreach (var recipe in examineList)
		{
			if (!SmithingRecipieValidator(recipe))
			{
#if DEBUG
				Mod.Logger.Debug($"Probable invalid Smithing Recipe: {recipe.Name}, skipping.");
#endif
				continue;
			}

			var metalObject = recipe.Ingredient.Type == EnumItemClass.Item
				? ServerCore.World.GetItem(recipe.Ingredient.Code)
				: ServerCore.World.GetBlock(recipe.Ingredient.Code) as CollectibleObject;
			var outputItem = ServerCore.World.GetItem(recipe?.Output?.Code);

			if (outputItem == null)
			{
#if DEBUG
				Mod.Logger.Debug($"Missing Output item, from: {recipe.Name}, skipping.");
#endif
				continue;
			}

			if (metalObject != null && metalObject.CombustibleProps != null &&
			    metalObject.CombustibleProps.SmeltingType == EnumSmeltType.Smelt &&
			    metalObject.CombustibleProps.SmeltedRatio > 0)
			{
				//Item Input Has a metal Unit value...(Smeltable)	
				//Resolve?
				int setVoxels = 0, meltPoint = 0;
				float meltDur = 0;
				setVoxels = recipe.Voxels.OfType<bool>().Count(vox => vox);

#if DEBUG
				Mod.Logger.VerboseDebug(
					$"Info: {recipe.Output.Quantity}* '{outputItem.Code}' -> {setVoxels}x '{metalObject.Code}' voxel = ~{setVoxels * CachedConfiguration.VoxelEquivalentValue:F1} metal Units");
#endif
				//Direct output *IS* tool or tool-like Durability type item (chisel )
				if (outputItem.Tool.HasValue || outputItem.Durability > 1)
				{
					if (itemToVoxelLookup.ContainsKey(outputItem.Code))
					{
						Mod.Logger.Warning($"Duplicate recipie '{recipe.Name}' output item: '{outputItem.Code}'");
					}
					else
					{
						LookupMetalCode(metalObject.Code, out meltDur, out meltPoint);

						itemToVoxelLookup.Add(new RecoveryEntry(outputItem.Code, metalObject.Code,
							(uint)(setVoxels / recipe.Output.Quantity)));
#if DEBUG
						Mod.Logger.VerboseDebug(
							$"Mapped: ('tool') '{outputItem.Code}' -> ('tool') '{outputItem.Code}'");
#endif
					}
				}
				else
				{
					//Tool-head map to Tool item; decode
					var itemToolCode = ServerCore.World.GridRecipes.FirstOrDefault(gr =>
						gr.Ingredients.Any(crg => crg.Value.Code.Equals(outputItem.Code)) && gr.Enabled &&
						gr.Output.Type == EnumItemClass.Item)?.Output.Code;
					if (itemToolCode != null)
					{
						var itemTool = ServerCore.World.GetItem(itemToolCode);
						if (itemTool == null)
						{
#if DEBUG
							Mod.Logger.Debug($"Missing Output Tool item, from: {recipe.Name}, skipping.");
#endif
							continue;
						}

						if (itemTool.Tool.HasValue)
						{
							if (itemToVoxelLookup.ContainsKey(itemToolCode))
							{
								Mod.Logger.Warning(
									$"Duplicate recipie '{recipe.Name}' output tool-item: '{itemToolCode}'");
							}
							else
							{
								LookupMetalCode(metalObject.Code, out meltDur, out meltPoint);
								itemToVoxelLookup.Add(new RecoveryEntry(itemToolCode.Clone(), metalObject.Code,
									(uint)(setVoxels / recipe.Output.Quantity)));
							}
#if DEBUG
							Mod.Logger.VerboseDebug($"Mapped: (head) '{outputItem.Code}' -> (tool) '{itemToolCode}'");
#endif
						}
					}
				}
			}
		}

		Mod.Logger.Event("tallied {0} smithables totaling {1} metal units from {2} smithing recipies!",
			itemToVoxelLookup.Count, itemToVoxelLookup.Sum(ie => ie.TotalQuantity), SmithingRecipies.Count);
	}

	private bool SmithingRecipieValidator(SmithingRecipe aRecipie)
	{
		if (Helpers.NothingNull(aRecipie,
			    aRecipie.Ingredient, aRecipie.Ingredient.Code,
			    aRecipie.Output, aRecipie.Output.Code, aRecipie.Output.Type))
			//2nd Stage - 
			if (aRecipie.Ingredients.Length >= 1 && aRecipie.Ingredient.Quantity >= 1)
				if (aRecipie.Output.Quantity > 0)
					return true;

		return false;
	}

	private void Item_DamageEventReciever(string eventName, ref EnumHandling handling, IAttribute data)
	{
		handling = EnumHandling.PassThrough;
		var hotbarData = data as HotbarObserverData;

#if DEBUG
		Mod.Logger.VerboseDebug("Item_Damage Rx: Item:{0} InventoryID '{1}' Slot#{2} PlayerUID:{3}",
			hotbarData.ItemCode, hotbarData.InventoryID, hotbarData.Inventory_SlotID, hotbarData.PlayerUID);
#endif

		if (CachedConfiguration.ToolFragmentRecovery && ItemFilterList.Contains(hotbarData.ItemCode))
		{
			var rec = itemToVoxelLookup[hotbarData.ItemCode];
#if DEBUG
			Mod.Logger.VerboseDebug("broken-item {0} abs. WORTH: {1:F1}*{2} units", hotbarData.ItemCode,
				rec.TotalQuantity * CachedConfiguration.VoxelEquivalentValue, rec.PrimaryMaterial.ToShortString());
#endif

			if (string.IsNullOrEmpty(hotbarData.PlayerUID) || string.IsNullOrEmpty(hotbarData.InventoryID)) return;

			var probablyHotbar =
				hotbarData.InventoryID.StartsWith(GlobalConstants.hotBarInvClassName, StringComparison.Ordinal);
			var playerTarget = ServerCore.World.PlayerByUid(hotbarData.PlayerUID);
			var spim = playerTarget.InventoryManager as ServerPlayerInventoryManager;
			var hotbarInv = playerTarget.InventoryManager.GetHotbarInventory();
			var hotSlot = hotbarInv[hotbarData.Inventory_SlotID];

			if (probablyHotbar && hotSlot.Empty)
			{
#if DEBUG
				Mod.Logger.VerboseDebug("Directly inserting fragments into hotbar slot# {0}",
					hotbarData.Inventory_SlotID);
#endif

				var variableMetal =
					ServerCore.World.GetItem(new AssetLocation(metalFragmentsCode)) as VariableMetalItem;
				var metalFragmentsStack = new ItemStack(variableMetal);
				variableMetal.ApplyMetalProperties(rec, ref metalFragmentsStack, CachedConfiguration.ToolRecoveryRate);
				hotSlot.Itemstack = metalFragmentsStack;
				hotSlot.Itemstack.ResolveBlockOrItem(ServerCore.World);
				hotSlot.MarkDirty();
				spim.NotifySlot(playerTarget, hotSlot);
			}
			else
			{
#if DEBUG
				Mod.Logger.VerboseDebug(
					"Hotbar-occupied (or crafting) slot#{0} so; shoving {1} in general direction of player...",
					hotbarData.Inventory_SlotID, hotbarData.ItemCode.ToShortString());
#endif

				var variableMetal =
					ServerCore.World.GetItem(new AssetLocation(metalFragmentsCode)) as VariableMetalItem;
				var metalFragmentsStack = new ItemStack(variableMetal);
				variableMetal.ApplyMetalProperties(rec, ref metalFragmentsStack, CachedConfiguration.ToolRecoveryRate);
				if (!spim.TryGiveItemstack(metalFragmentsStack, true))
					//Player with full Inv.
					ServerCore.World.SpawnItemEntity(metalFragmentsStack, playerTarget.Entity.Pos.XYZ);
			}
		}
	}


	private void EditDurability(IServerPlayer player, int groupId, CmdArgs args)
	{
		if (!player.Entity.RightHandItemSlot.Empty &&
		    player.Entity.RightHandItemSlot.Itemstack.Class == EnumItemClass.Item)
		{
			var number = args.PopInt();

			player.Entity.RightHandItemSlot.Itemstack.Hitpoints(number ?? 10);
			player.Entity.RightHandItemSlot.MarkDirty();
		}
	}

	/// <summary>
	///     Adds all smelting related 'combustibleProps' fields from known Ingot Codes; (matching variant keys)
	/// </summary>
	/// <returns>The combustible properties by code variant.</returns>
	/// <param name="updatingCode">PARTIAL Item code.</param>
	private void ApplySmeltingPropertiesByCodeVariant(AssetLocation updatingCode, int ratioOverride = 1)
	{
		//ALL ????:'ingot-*' type items...
		var ingotItems =
			ServerCore.World.Items.Where(itm => itm.ItemId > 0 && itm.Code != null && itm.Code.BeginingOnly(@"ingot"));

#if DEBUG
		Mod.Logger.VerboseDebug("found {0} Ingot type items", ingotItems.Count());
#endif

		foreach (var ingotEntry in ingotItems)
		{
			if (ingotEntry == null) continue;
			var metalName = ingotEntry?.Variant[@"metal"];
			var metalSmeltProps = ingotEntry?.CombustibleProps?.Clone();

			if (metalSmeltProps == null) continue; //SHOULD BE NEVER !
			if (string.IsNullOrEmpty(metalName)) continue;

			metalSmeltProps.SmeltedRatio = ratioOverride;
			var shavingCode = updatingCode.AppendPathVariant(metalName);
			var shavingEquivalentItem = ServerCore.World.GetItem(shavingCode);
			if (shavingEquivalentItem != null)
			{
				shavingEquivalentItem.CombustibleProps = metalSmeltProps;
#if DEBUG
				ServerCore.Logger.VerboseDebug("Updated SmeltProps, for: {0}", shavingCode);
#endif
			}
			else
			{
#if DEBUG
				ServerCore.Logger.VerboseDebug("Non-existant item: {0}", shavingCode);
#endif
			}
		}
	}

	private bool LookupMetalCode(AssetLocation metalAssetCode, out float melting_duration, out int melting_point)
	{
		melting_duration = 30f; //same for ALL metals?
		melting_point = 0;
		var metalName = metalAssetCode.PathEnding(); //Better be an Ingot!
		if (MetalProperties.ContainsKey(metalName))
		{
			melting_point = (int)MetalProperties[metalName].MeltingPoint;
			return true;
		}

		return false;
	}
}