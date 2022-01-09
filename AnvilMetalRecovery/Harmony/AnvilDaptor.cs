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

	if (anvil.IsShavable && anvil.Voxel(voxelPos.X, voxelPos.Y, voxelPos.Z) == EnumVoxelMaterial.Metal) {
	#if DEBUG
	anvil.Logger.VerboseDebug("{0}, Split into {1} @{2}, Total:{3}",anvil.OutputCode ,anvil.BaseMetal, voxelPos, anvil.SplitCount);
	#endif
	anvil.SplitCount++;
	}
		
	}

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
		 

	}

	/// <summary>
	/// Special tools for handling the Anvil's data/state, ect...
	/// </summary>
	internal class SmithAssist
	{
		private readonly BlockEntityAnvil bea;

		internal const string splitCountKey = @"splitCount";
		internal const int shavingValue = 5;

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
			Item metalShavingsItem = bea.Api.World.GetItem(MetalShavingsCode.AppendPathVariant(BaseMetal));

			if (metalShavingsItem != null) 
			{
				ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, shavingQty);

				if (byPlayer != null) {
				if (byPlayer.InventoryManager.TryGiveItemstack(metalShavingsStack, false) == false) { bea.Api.World.SpawnItemEntity(metalShavingsStack, bea.Pos.ToVec3d().Add(0.1d,0,0) ); }
				#if DEBUG
				Logger.VerboseDebug("RecoveryAnvil: Smithing done - recover: {0} shavings of {1}", shavingQty, metalShavingsItem.Code);
				#endif
				}
			}
			else 
				{
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

		internal bool PrefixMatcher(List<AssetLocation> nameList, AssetLocation target)
		{
		if (nameList == null || nameList.Count == 0 || target == null) return false;

			foreach (var aName in nameList) {
			if (target.BeginsWith(aName.Domain, aName.Path)) return true;			
		}

		return false;
		}
	}
}

