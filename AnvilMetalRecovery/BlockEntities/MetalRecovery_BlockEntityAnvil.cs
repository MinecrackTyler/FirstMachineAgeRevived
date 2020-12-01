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
		private const string splitCountKey = @"splitCount";	

		private ILogger Logger { 
			get
			{
			return Api.World.Logger;
			}
		}

		public static AssetLocation MetalShavingsCode {
			get
			{
			return new AssetLocation(@"fma", @"metal_shaving");
			}
		}


		internal int SplitCount 
		{
			get {				
			return this.WorkItemStack?.Attributes.TryGetInt(splitCountKey) ?? 0;
			}
			set {
			this.WorkItemStack?.Attributes.SetInt(splitCountKey, value);
			}
		}

		public override void OnSplit(Vec3i voxelPos)
		{

		if (this.IsShavable && Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == ( byte )EnumVoxelMaterial.Metal) {
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
		int splitTemp = SplitCount;
		base.CheckIfFinished(byPlayer);
		// base.MatchesRecipe( ) -- Private; still in V1.14.... :\
		/*
		 this.Voxels = new byte[16, 6, 16];
		this.workItemStack = null;
		this.selectedRecipeId = -1;

		base.MarkDirty (false);	
		 */

		if (splitTemp > 0 && this.WorkItemStack == null && this.SelectedRecipe == null) {
			int metalShavings = ( int )(splitTemp / 5);

			if (metalShavings > 0) 
			{
			#if DEBUG
			Logger.VerboseDebug("RecoveryAnvil: Smithing done - recover: {0} shavings of {1}", metalShavings, this.BaseMaterial.Collectible.LastCodePart( ));
			#endif

			Item metalShavingsItem = Api.World.GetItem(MetalShavingsCode.WithPathAppendix("-" + this.BaseMaterial.Collectible.LastCodePart( )));

			if (metalShavingsItem == null) return;
			ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, metalShavings);

				if (byPlayer != null) {
				byPlayer.InventoryManager.TryGiveItemstack(metalShavingsStack, false);
				//Api.World.SpawnItemEntity(metalShavingsStack, Pos.ToVec3d( ).Add(0.5, 0.5, 0.5));
				}
			}
		}
		}

		protected bool IsShavable {
			get { 
				//this.SelectedRecipe <-- things that are recoverable?
				return this.WorkItemStack?.Collectible?.FirstCodePart( ).Equals("ironbloom") == false; 
			}
		}

		protected IAnvilWorkable AnvilWorkpiece {
			get
			{
			if (this.WorkItemStack != null && this.WorkItemStack.Collectible is IAnvilWorkable) 
			{ return this.WorkItemStack.Collectible as IAnvilWorkable; }

			return null;
			}
		}

		protected ItemStack BaseMaterial 
		{
			get
			{
			return AnvilWorkpiece.GetBaseMaterial(this.WorkItemStack);//Right??
			}
		}
	}
}