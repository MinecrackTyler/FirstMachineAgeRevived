using System;
using System.IO;
using System.Text;

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
		private const uint splitValue = 5;

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
			Logger.VerboseDebug("Split some {0} @{1}, Total:{2}", this.BaseMetal, voxelPos, SplitCount);
		#endif
		SplitCount++;
		}

		base.OnSplit(voxelPos);
		}

		//Would be great if this returned a bool!
		public override void CheckIfFinished(IPlayer byPlayer)
		{
		int splitTemp = SplitCount;
		string baseMaterial = this.BaseMetal;
		base.CheckIfFinished(byPlayer);
		// base.MatchesRecipe( ) -- Private; still in V1.14.... :\
		/*
		 this.Voxels = new byte[16, 6, 16];
		this.workItemStack = null;
		this.selectedRecipeId = -1;

		base.MarkDirty (false);	
		 */

		if (splitTemp > 0 && this.WorkItemStack == null && this.SelectedRecipe == null) 
		{
		int shavingsCount = ( int )(splitTemp / splitValue);

		if (shavingsCount > 0) 
		{
		#if DEBUG
		Logger.VerboseDebug("RecoveryAnvil: Smithing done - recover: {0} shavings of {1}", shavingsCount, baseMaterial);
		#endif

		Item metalShavingsItem = Api.World.GetItem(MetalShavingsCode.WithPathAppendix("-" + baseMaterial));

		if (metalShavingsItem != null) 
			{
			ItemStack metalShavingsStack = new ItemStack(metalShavingsItem, shavingsCount);

			if (byPlayer != null) { byPlayer.InventoryManager.TryGiveItemstack(metalShavingsStack, false); }
			}
		else 
			{	
			Logger.Warning("Missing or Invalid Item: {0} ", MetalShavingsCode.WithPathAppendix("-" + baseMaterial));
			}
		}
		}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
		base.GetBlockInfo(forPlayer, dsc);

		if (this.IsShavable && this.SplitCount > 0 && this.BaseMaterial != null ) {
		dsc.AppendFormat("[ {0} ÷ {1} ] | {2}",this.SplitCount, splitValue, Lang.GetUnformatted($"fma:item-metal_shaving-{this.BaseMetal}"));
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
			if (this.WorkItemStack != null)	return AnvilWorkpiece.GetBaseMaterial(this.WorkItemStack);//Right??
			return null;
			}
		}

		protected string BaseMetal 
		{
			get
			{
			return this?.BaseMaterial?.Collectible.LastCodePart( );
			}
		}
	}
}