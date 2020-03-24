using System;
using System.IO;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	public class MetalRecovery_BlockEntityAnvil : BlockEntityAnvil
	{
		private uint SplitCount;


		private ILogger Logger { get; set; }

		public static AssetLocation MetalShavingsCode {
			get
			{
			return new AssetLocation(@"fma", @"metal_shaving");
			}
		}

		public override void Initialize(ICoreAPI api)
		{
		base.Initialize(api);
		Logger = api.World.Logger;
#if DEBUG
		Logger.VerboseDebug("Metal Recovery - Initialize");
#endif
		}

		public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
		base.FromTreeAtributes(tree, worldForResolving);
		Logger = worldForResolving.Logger;
#if DEBUG
		Logger.VerboseDebug("Metal Recovery - FromTreeAtributes");
#endif
		//Get SplitCount
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		base.ToTreeAttributes(tree);
#if DEBUG
		Logger.VerboseDebug("Metal Recovery - ToTreeAttributes");
#endif
		//Set SplitCount
		}

		public override void OnSplit(Vec3i voxelPos)
		{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == ( byte )EnumVoxelMaterial.Metal) {
#if DEBUG
		Logger.VerboseDebug("Split some {0} @{1}, Total:{2}", this.BaseMaterial.Collectible.LastCodePart( ), voxelPos, SplitCount);
#endif
		SplitCount++;
		}

		base.OnSplit(voxelPos);

		}

		//Would be great if this returned a bool!
		public override void CheckIfFinished(IPlayer byPlayer)
		{
		base.CheckIfFinished(byPlayer);
		// base.MatchesRecipe( ) -- Private; argh
		/*
		 this.Voxels = new byte[16, 6, 16];
		this.workItemStack = null;
		this.selectedRecipeId = -1;

		base.MarkDirty (false);	
		 */

		if (SplitCount > 0 && this.WorkItemStack == null && this.SelectedRecipe == null) {
			int metalShavings = ( int )(SplitCount / 5);

			if (metalShavings > 0) 
			{
			#if DEBUG
			Logger.VerboseDebug("RecoveryAnvil: Smithing done - recover: {0} shavings of {1}", metalShavings, this.BaseMaterial.Collectible.LastCodePart( ));
			#endif

			Item metalShavingsItem = Api.World.GetItem(MetalShavingsCode.WithPathAppendix("-" + this.BaseMaterial.Collectible.LastCodePart( )));
			ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, ( int )(SplitCount / 5));

				if (byPlayer != null) {
				byPlayer.InventoryManager.TryGiveItemstack(metalShavingsStack, false);
				//Api.World.SpawnItemEntity(metalShavingsStack, Pos.ToVec3d( ).Add(0.5, 0.5, 0.5));
				}
			}
		}


		}
	}
}