using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery.Patches
{
	/// <summary>
	/// Harmony patcher class to wrap B.E. Anvil class
	/// </summary>
	[HarmonyPatch(typeof(BlockEntityAnvil))]
	public class AnvilDaptor 
	{

	[HarmonyPrepare]
	private static bool DeduplicatePatching(MethodBase original, Harmony harmony)
	{

	if (original != null ) {
		foreach(var patched in harmony.GetPatchedMethods()) 
		{							
		if (patched.Name == original.Name)return false; //SKIPS PATCHING, its already there
		}
	}

	return true;//patch all other methods
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(BlockEntityAnvil.OnSplit))]	
	private static void Prefix_OnSplit(Vec3i voxelPos, BlockEntityAnvil __instance)
	{
	var anvil = new SmithAssist(__instance);
	#if DEBUG
	anvil.Logger.VerboseDebug("Prefix_OnSplit");
	#endif

	if (anvil.IsShavable && anvil.Voxel(voxelPos.X, voxelPos.Y, voxelPos.Z) == EnumVoxelMaterial.Metal) {
		#if DEBUG
		anvil.Logger.VerboseDebug("{0}, Split into {1} @{2}, Total:{3}",anvil.OutputCode ,anvil.BaseMetal, voxelPos, anvil.SplitCount);
		#endif
		anvil.SplitCount++;
		}		
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(BlockEntityAnvil.OnSplit))]
	private static void Postfix_OnSplit(Vec3i voxelPos, BlockEntityAnvil __instance)
	{
	var anvil = new SmithAssist(__instance);
	if (anvil.World.Side.IsClient( )) return;

	#if DEBUG
	anvil.Logger.VerboseDebug("Postfix_OnSplit");
	#endif
	if (anvil.WorkEntirelySplit( )) 
		{
		#if DEBUG
		anvil.Logger.VerboseDebug("Work item entirely split up.");
		#endif
		anvil.IssueCancelStackEquivalent();//anvil.IssueShavings(null);		
		anvil.ClearWork();
		}
	}

	//internal void OnUseOver -->
	[HarmonyPrefix]
	[HarmonyPatch(nameof(BlockEntityAnvil.CheckIfFinished))]
	private static void Prefix_CheckIfFinished(IPlayer byPlayer, BlockEntityAnvil __instance)
	{
	var anvil = new SmithAssist(__instance);
		if (anvil.WorkMatchesRecipe( )) {
		anvil.IssueShavings(byPlayer);		
		}		
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(BlockEntityAnvil.GetBlockInfo))]	
	private static void Postfix_GetBlockInfo(IPlayer forPlayer, StringBuilder dsc, BlockEntityAnvil __instance)
	{	
	var anvil = new SmithAssist(__instance);

		if (anvil.BaseMaterial != null && anvil.SplitCount > 0) 
		{			
		dsc.AppendFormat("[ {0} ] : {1} × {2}\n", anvil.SplitCount, Lang.GetUnformatted($"game:item-metalbit-{anvil.BaseMetal}"), anvil.ShavingQuantity);
		}
	}

	

	/* [HarmonyReversePatch] 
	private bool MatchesRecipe() //But faster?
	*/
		 

	} /************** HARMONY CLASS ENDS *******************/

	/// <summary>
	/// Special tools for handling the Anvil's data/state, ect...
	/// </summary>
	internal class SmithAssist
	{
		private readonly BlockEntityAnvil bea;

		internal const string splitCountKey = @"splitCount";
		internal const int shavingValue = 5;

		private AccessTools.FieldRef<BlockEntityAnvil, ItemStack> workItemStack_R = AccessTools.FieldRefAccess<BlockEntityAnvil, ItemStack>(@"workItemStack");
		private AccessTools.FieldRef<BlockEntityAnvil, ItemStack> returnOnCancelStack_R = AccessTools.FieldRefAccess<BlockEntityAnvil, ItemStack>(@"returnOnCancelStack");

		internal SmithAssist(BlockEntityAnvil a)
		{
		this.bea = a;
		}

		internal ILogger Logger {
			get
			{
			return bea.Api.World.Logger;
			}
		}

		internal IWorldAccessor World {
			get
			{
			return bea.Api.World;
			}
		}

		public static AssetLocation MetalShavingsCode {
			get
			{
			return new AssetLocation(GlobalConstants.DefaultDomain, @"metalbit");
			}
		}

		// public byte[,,] Voxels = new byte[16, 6, 16];
		internal EnumVoxelMaterial Voxel( int X , int Y , int Z ) 
		{		
		return (EnumVoxelMaterial)bea.Voxels[X, Y, Z]; 
		}

		private AMRConfig CachedConfiguration {
			get
			{
			return ( AMRConfig )bea.Api.ObjectCache[MetalRecoverySystem._configFilename];
			}
		}

		public int SplitCount {
			get
			{
			return bea.WorkItemStack?.Attributes.TryGetInt(splitCountKey) ?? 0;
			}
			set
			{
			bea.WorkItemStack?.Attributes.SetInt(splitCountKey, value);
			}
		}

		public bool IsShavable {
			get
			{
			if (bea.WorkItemStack?.Collectible?.FirstCodePart().Equals(@"ironbloom") == true
				|| (bea.SelectedRecipe != null
				    && PrefixMatcher(CachedConfiguration.BlackList, this.OutputCode) )) 
				{
					#if DEBUG
					this.Logger.VerboseDebug("That ain't shavable: {0}", this.OutputCode);
					#endif
					return false;
				}

			return true;
			}
		}

		public int ShavingQuantity 
		{
			get { return ( int )(Math.Round(SplitCount * CachedConfiguration.VoxelEquivalentValue) / shavingValue); }
		}

		internal IAnvilWorkable AnvilWorkpiece {
			get
			{
			if (bea.WorkItemStack != null && bea.WorkItemStack.Collectible is IAnvilWorkable) { return bea.WorkItemStack.Collectible as IAnvilWorkable; }

			return null;
			}
		}

		internal ItemStack BaseMaterial {
			get
			{
			if (bea.WorkItemStack != null) return AnvilWorkpiece.GetBaseMaterial(bea.WorkItemStack);//Right??
			return null;
			}
		}

		internal AssetLocation OutputCode {
			get
			{
			return bea.SelectedRecipe.Output.Code;
			}
		}

		public string BaseMetal {
			get
			{
			return this?.BaseMaterial?.Collectible.LastCodePart( );
			}
		}

		public int MetalVoxelCount {
			get { return bea.Voxels.OfType<byte>( ).Count(vox => vox == (byte)EnumVoxelMaterial.Metal); }
		}


		internal void IssueShavings(IPlayer byPlayer )
		{
		if (this.SplitCount > 0) {
		int shavingQty = ShavingQuantity;

		if (shavingQty > 0) 
		{			
		Item metalShavingsItem = World.GetItem(MetalShavingsCode.AppendPathVariant(BaseMetal));

		if (metalShavingsItem != null) 
			{
			ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, shavingQty);

			if (byPlayer != null) 
				{
				if (byPlayer.InventoryManager.TryGiveItemstack(metalShavingsStack, false) == false) 
					{ World.SpawnItemEntity(metalShavingsStack, bea.Pos.ToVec3d( ).Add(0.1d, 0, 0)); }			
				}
				else 
				{
				//Just spew itemstack on top of anvil...Player ran off?
				World.SpawnItemEntity(metalShavingsStack, bea.Pos.ToVec3d( ).Add(0.1d, 0, 0));
				}
				#if DEBUG
				Logger.VerboseDebug("RecoveryAnvil: Smithing done - recover: {0} shavings of {1}", shavingQty, metalShavingsItem.Code);
				#endif
			}
			else {
				Logger.Warning("Missing or Invalid Item: {0} ", MetalShavingsCode.WithPathAppendix("-" + BaseMaterial));
				}		
			}
		}
		this.SplitCount = 0;
		}

		/// <summary>
		/// Copy-Paste, from 'BEAnvil.cs'
		/// </summary>
		/// <returns>The matches recipe.</returns>
		internal bool WorkMatchesRecipe()
		{
		if (bea.SelectedRecipe == null) return false;

		int ymax = Math.Min(6, bea.SelectedRecipe.QuantityLayers);//Why ignore higher layers?

		var theRecipie = bea.recipeVoxels; //RotatedRecipeVoxels

		for (int x = 0; x < 16; x++) {
		for (int y = 0; y < ymax; y++) {
		for (int z = 0; z < 16; z++) {
		byte desiredMat = ( byte )(theRecipie[x, y, z] ? EnumVoxelMaterial.Metal : EnumVoxelMaterial.Empty);

		if (bea.Voxels[x, y, z] != desiredMat) { return false;	}	}	}	}

		return true;
		}

		/// <summary>
		/// If the Work voxels are _GONE_ and everything is split UP?
		/// </summary>
		/// <returns>If entirely split.</returns>
		internal bool WorkEntirelySplit( )
		{
			if ( SplitCount > 0  && bea.SelectedRecipe != null) {		//Work-Item stack OK; for post split check...
		foreach (EnumVoxelMaterial voxel in bea.Voxels) {
			if (voxel == EnumVoxelMaterial.Metal || voxel == EnumVoxelMaterial.Slag) return false;		
			}
		return true;
		}

		return false;
		}

		internal bool PrefixMatcher(List<AssetLocation> nameList, AssetLocation target)
		{
		if (nameList == null || nameList.Count == 0 || target == null) return false;

			foreach (var aName in nameList) {
			if (target.BeginsWith(aName.Domain, aName.Path)) return true;			
		}

		return false;
		}

		internal void IssueCancelStackEquivalent( )
		{

			if (bea.SelectedRecipeId > 0 && bea.SelectedRecipe != null ) 
			{
				var itemToVoxelLookup = MetalRecoverySystem.GetCachedLookupTable(World);
				if (itemToVoxelLookup.ContainsKey(bea.SelectedRecipe.Output.Code)) {
					var result = itemToVoxelLookup[bea.SelectedRecipe.Output.Code];
					#if DEBUG
					Logger.VerboseDebug("(old) Selected Recipe: {0} base-material: '{1}' worth {2} units; spawning", bea.SelectedRecipe.Output.Code, result.IngotCode, result.Quantity);
					#endif

					int shavingQty = ( int )(result.Quantity / MetalRecoverySystem.IngotVoxelDefault);

					if (shavingQty > 0) {
					Item metalShavingsItem = World.GetItem(MetalShavingsCode.AppendPathVariant(result.IngotCode.PathEnding()));

					if (metalShavingsItem != null) {
					ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, shavingQty);					
					World.SpawnItemEntity(metalShavingsStack, bea.Pos.ToVec3d( ).Add(0.1d, 0, 0));
					}
					}
				}
			}
		}

		internal void ClearWork( )
		{
		#if DEBUG
		Logger.Debug("Manually Clearing Work Item stack & Anvil state...the hard way.");
		#endif

		workItemStack_R(bea) = null;//bea.workItemStack = null;
		returnOnCancelStack_R(bea) = null; //Needed?
		bea.Voxels = new byte[16, 6, 16];				
		bea.rotation = 0;
		bea.SelectedRecipeId = -1;
		bea.MarkDirty(false, null);
		}
	}
}

