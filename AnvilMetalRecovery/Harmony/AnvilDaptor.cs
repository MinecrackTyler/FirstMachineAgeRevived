using System;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	/// <summary>
	/// Harmony patcher class to generate Bus-Events from Anvil, and other-effects
	/// </summary>
	[HarmonyPatch(typeof(BlockEntityAnvil))]
	public class AnvilDaptor 
	{
	 
	[HarmonyPrefix]
	[HarmonyPatch(nameof(BlockEntityAnvil.OnSplit))]	
	private static void Prefix_OnSplit(Vec3i voxelPos, BlockEntityAnvil __instance)
	{
	SmithAssist anvil = __instance as SmithAssist;	

	if (anvil.IsShavable && anvil.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == ( byte )EnumVoxelMaterial.Metal) {
	#if DEBUG
	anvil.Logger.VerboseDebug("Split some {0} @{1}, Total:{2}", anvil.BaseMetal, voxelPos, anvil.SplitCount);
	#endif
	anvil.SplitCount++;
	}
		
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(BlockEntityAnvil.GetBlockInfo))]
	private static void Postfix_GetBlockInfo(IPlayer forPlayer, StringBuilder dsc, BlockEntityAnvil __instance)
	{	
	SmithAssist anvil = __instance as SmithAssist;

	if (anvil.IsShavable && anvil.SplitCount > 0 && anvil.BaseMaterial != null) {
	dsc.AppendFormat("[ {0} ÷ {1} ] = {2}", anvil.SplitCount, SmithAssist.splitValue, Lang.GetUnformatted($"fma:item-metal_shaving-{anvil.BaseMetal}"));
	}

	}


	}

	/// <summary>
	/// Special tools for handling the Anvil's data/state, ect...
	/// </summary>
	internal class SmithAssist : BlockEntityAnvil
	{
		internal const string splitCountKey = @"splitCount";
		internal const uint splitValue = 5;

		internal ILogger Logger {
			get
			{
			return Api.World.Logger;
			}
		}

		internal static AssetLocation MetalShavingsCode {
			get
			{
			return new AssetLocation(@"fma", @"metal_shaving");
			}
		}


		internal int SplitCount {
			get
			{
			return this.WorkItemStack?.Attributes.TryGetInt(splitCountKey) ?? 0;
			}
			set
			{
			this.WorkItemStack?.Attributes.SetInt(splitCountKey, value);
			}
		}

		internal bool IsShavable {
			get
			{
			//this.SelectedRecipe <-- things that are recoverable?
			return this.WorkItemStack?.Collectible?.FirstCodePart( ).Equals(@"ironbloom") == false;
			}
		}

		internal IAnvilWorkable AnvilWorkpiece {
			get
			{
			if (this.WorkItemStack != null && this.WorkItemStack.Collectible is IAnvilWorkable) { return this.WorkItemStack.Collectible as IAnvilWorkable; }

			return null;
			}
		}

		internal ItemStack BaseMaterial {
			get
			{
			if (this.WorkItemStack != null) return AnvilWorkpiece.GetBaseMaterial(this.WorkItemStack);//Right??
			return null;
			}
		}

		internal string BaseMetal {
			get
			{
			return this?.BaseMaterial?.Collectible.LastCodePart( );
			}
		}
	}
}

