using System;
using System.Linq;


using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;


namespace AnvilMetalRecovery
{
	public partial class MetalRecoverySystem : ModSystem
	{
		private void MaterialDataGathering( )
		{
		//Count out Voxels in smthing recipes for all metal-ingot(?) derived items;
		var examineList = this.SmithingRecipies.Where(sr => sr.Enabled && sr.Ingredient.Type == EnumItemClass.Item && sr.Output.Type == EnumItemClass.Item);

		foreach (var recipie in examineList) {

		if (SmithingRecipieValidator(recipie) == false) {
		#if DEBUG
		Mod.Logger.Debug($"Probable invalid Smithing Recipie: {recipie.Name.ToString( )}, skipping.");
		#endif
		continue;
		}

		CollectibleObject metalObject = recipie.Ingredient.Type == EnumItemClass.Item ? ServerAPI.World.GetItem(recipie.Ingredient.Code) : ServerAPI.World.GetBlock(recipie.Ingredient.Code) as CollectibleObject;
		Item outputItem = ServerAPI.World.GetItem(recipie?.Output?.Code);

		if (outputItem == null) {
		#if DEBUG
		Mod.Logger.Debug($"Missing Output item, from: {recipie.Name.ToString( )}, skipping.");
		#endif
		continue;
		}

		if (metalObject != null && metalObject.CombustibleProps != null && metalObject.CombustibleProps.SmeltingType == EnumSmeltType.Smelt && metalObject.CombustibleProps.SmeltedRatio > 0) {
		//Item Input Has a metal Unit value...(Smeltable)	
		//Resolve?
		int setVoxels = 0;
		setVoxels = recipie.Voxels.OfType<bool>( ).Count(vox => vox);							

		#if DEBUG
		Mod.Logger.VerboseDebug($"Info: {recipie.Output.Quantity}* '{outputItem.Code}' -> {setVoxels}x '{metalObject.Code}' voxel = ~{setVoxels * CachedConfiguration.VoxelEquivalentValue:F1} metal Units");
		#endif
		//Direct output *IS* tool or tool-like Durability type item (chisel )
		if (outputItem.Tool.HasValue || outputItem.Durability > 1) {

		if (itemToVoxelLookup.ContainsKey(outputItem.Code)) { 
		Mod.Logger.Warning($"Duplicate recipie '{recipie.Name}' output item: '{outputItem.Code.ToString()}'");				
		}
		else {
		itemToVoxelLookup.Add(new RecoveryEntry(outputItem.Code, metalObject.Code,
							                                              ( uint )(setVoxels / recipie.Output.Quantity),
																			  metalObject.CombustibleProps.MeltingDuration,
																		  metalObject.CombustibleProps.MeltingPoint)
							 );
			#if DEBUG
			Mod.Logger.VerboseDebug($"Mapped: ('tool') '{outputItem.Code}' -> ('tool') '{outputItem.Code}'");
			#endif
			}
						
		}
		else {
		//Tool-head map to Tool item; decode
		var itemToolCode = ServerAPI.World.GridRecipes.FirstOrDefault(gr => gr.Ingredients.Any(crg => crg.Value.Code.Equals(outputItem.Code)) && gr.Enabled && gr.Output.Type == EnumItemClass.Item)?.Output.Code;
		if (itemToolCode != null) {
		var itemTool = ServerAPI.World.GetItem(itemToolCode);
		if (itemTool == null) {
		#if DEBUG
		Mod.Logger.Debug($"Missing Output Tool item, from: {recipie.Name.ToString( )}, skipping.");
		#endif
		continue;
		}

		if ( itemTool.Tool.HasValue) {
		if (itemToVoxelLookup.ContainsKey(itemToolCode)) {
		Mod.Logger.Warning($"Duplicate recipie '{recipie.Name}' output tool-item: '{itemToolCode.ToString( )}'");
		}
		else
		itemToVoxelLookup.Add( new RecoveryEntry(itemToolCode.Clone( ), metalObject.Code,
																	  ( uint )(setVoxels / recipie.Output.Quantity),
																		  metalObject.CombustibleProps.MeltingDuration,
																	  metalObject.CombustibleProps.MeltingPoint)
						 );
		#if DEBUG
		Mod.Logger.VerboseDebug($"Mapped: (head) '{outputItem.Code}' -> (tool) '{itemToolCode}'");
		#endif
		}
		}
		}
		}
		}

		Mod.Logger.Event("tallied {0} smithables totaling {1} metal units from {2} smithing recipies!", itemToVoxelLookup.Count, itemToVoxelLookup.Sum(ie => ie.Quantity), this.SmithingRecipies.Count);
		}

		private bool SmithingRecipieValidator(SmithingRecipe aRecipie )
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
		HotbarObserverData hotbarData = data as HotbarObserverData;

		#if DEBUG
		Mod.Logger.VerboseDebug("Item_Damage Rx: Item:{0} InventoryID '{1}' Slot#{2} PlayerUID:{3}", hotbarData.ItemCode.ToString( ), hotbarData.InventoryID, hotbarData.Inventory_SlotID, hotbarData.PlayerUID);
		#endif

		if (CachedConfiguration.ToolFragmentRecovery && ItemFilterList.Contains(hotbarData.ItemCode)) 	{
				
		RecoveryEntry rec = itemToVoxelLookup[hotbarData.ItemCode];
		#if DEBUG
		Mod.Logger.VerboseDebug("broken-item {0} WORTH: {1:F1}*{2} units", hotbarData.ItemCode.ToString( ), (rec.Quantity * CachedConfiguration.VoxelEquivalentValue), rec.IngotCode.ToShortString( ));
		#endif

		if (String.IsNullOrEmpty(hotbarData.PlayerUID) || String.IsNullOrEmpty(hotbarData.InventoryID)) return;

		bool probablyHotbar = hotbarData.InventoryID.StartsWith(GlobalConstants.hotBarInvClassName, StringComparison.Ordinal);
		var playerTarget = ServerAPI.World.PlayerByUid(hotbarData.PlayerUID);
		var spim = playerTarget.InventoryManager as ServerPlayerInventoryManager;												
		var hotbarInv = playerTarget.InventoryManager.GetHotbarInventory( );
		var hotSlot = hotbarInv[hotbarData.Inventory_SlotID];

			if (probablyHotbar && hotSlot.Empty) 
			{					
			#if DEBUG
			Mod.Logger.VerboseDebug("Directly inserting fragments into hotbar slot# {0}", hotbarData.Inventory_SlotID);
			#endif

			VariableMetalItem variableMetal = ServerAPI.World.GetItem(new AssetLocation(metalFragmentsCode)) as VariableMetalItem;
			ItemStack metalFragmentsStack = new ItemStack(variableMetal, 1);
			variableMetal.ApplyMetalProperties(rec, ref metalFragmentsStack, CachedConfiguration.ToolRecoveryRate);
			hotSlot.Itemstack = metalFragmentsStack;
			hotSlot.Itemstack.ResolveBlockOrItem(ServerAPI.World);
			hotSlot.MarkDirty( );
			spim.NotifySlot(playerTarget, hotSlot);
			}							
			else
			{
			#if DEBUG
			Mod.Logger.VerboseDebug("Hotbar-occupied (or crafting) slot#{0} so; shoving {1} in general direction of player...", hotbarData.Inventory_SlotID, hotbarData.ItemCode.ToShortString( ));
			#endif

			VariableMetalItem variableMetal = ServerAPI.World.GetItem(new AssetLocation(metalFragmentsCode)) as VariableMetalItem;
			ItemStack metalFragmentsStack = new ItemStack(variableMetal, 1);
			variableMetal.ApplyMetalProperties(rec, ref metalFragmentsStack, CachedConfiguration.ToolRecoveryRate);
				if (spim.TryGiveItemstack(metalFragmentsStack, true) == false) 
					{
					//Player with full Inv.
					ServerAPI.World.SpawnItemEntity(metalFragmentsStack, playerTarget.Entity.Pos.XYZ);
					}
			}		
		}

		}


		private void EditDurability(IServerPlayer player, int groupId, CmdArgs args)
		{
		if (!player.Entity.RightHandItemSlot.Empty &&
			player.Entity.RightHandItemSlot.Itemstack.Class == EnumItemClass.Item) {
		var number = args.PopInt( );

		player.Entity.RightHandItemSlot.Itemstack.Hitpoints(number ?? 10);
		player.Entity.RightHandItemSlot.MarkDirty( );
		}

		}

		/// <summary>
		/// Adds all smelting related 'combustibleProps' fields from known Ingot Codes; (matching variant keys)
		/// </summary>
		/// <returns>The combuastable properties by code variant.</returns>
		/// <param name="updatingCode">PARTIAL Item code.</param>
		private void ApplySmeltingPropertiesByCodeVariant(AssetLocation updatingCode, int ratioOverride = 1)
		{
		//ALL ????:'ingot-*' type items...
		var ingotItems = ServerAPI.World.Items.Where(itm => itm.ItemId > 0 && itm.Code != null && itm.Code.BeginingOnly(@"ingot"));

		#if DEBUG
		this.Mod.Logger.VerboseDebug("found {0} Ingot type items", ingotItems.Count( ));
		#endif

		foreach (var ingotEntry in ingotItems) {
		if (ingotEntry == null) continue;
		string metalName = ingotEntry?.Variant[@"metal"];
		var metalSmeltProps = ingotEntry?.CombustibleProps?.Clone( );

		if (metalSmeltProps == null) continue;//SHOULD BE NEVER !
		if (string.IsNullOrEmpty(metalName)) continue;

		metalSmeltProps.SmeltedRatio = ratioOverride;
		AssetLocation shavingCode = updatingCode.AppendPathVariant(metalName);
		var shavingEquivalentItem = ServerAPI.World.GetItem(shavingCode);
		if (shavingEquivalentItem != null) {
		shavingEquivalentItem.CombustibleProps = metalSmeltProps;
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Updated SmeltProps, for: {0}", shavingCode.ToString( ));
		#endif
		}
		else {
		#if DEBUG
		ServerAPI.Logger.VerboseDebug("Non-existant item: {0}", shavingCode.ToString( ));
		#endif
		}
		}

		}
	}
}

