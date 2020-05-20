using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ConstructionSupport
{
	public abstract class GenericScaffold : Block
	{
		internal const string ValidAttachmentFacesKey = @"ValidAttachemtFaces";
		internal const string AirgapFacesKey = @"AirgapFaces";

		internal BlockFacing[ ] _ValidAttachmentFaces;
		internal BlockFacing[ ] _AirgapFaces;

		//Decode from readonly Json properties
		public virtual BlockFacing[] ValidAttachmentFaces 
		{ 
			get
			{
			if (_ValidAttachmentFaces != null) return _ValidAttachmentFaces;

			if (this.Attributes[ValidAttachmentFacesKey].Exists) {
			_ValidAttachmentFaces = this.Attributes[ValidAttachmentFacesKey].AsArray<BlockFacing>(BlockFacing.ALLFACES);
			}
			else {
			_ValidAttachmentFaces = BlockFacing.ALLFACES;
			}

			return _ValidAttachmentFaces;
			}
		}

		public virtual BlockFacing[ ] AirgapFaces {
			get
			{
			if (_AirgapFaces != null) return _AirgapFaces;

			if (this.Attributes[AirgapFacesKey].Exists) {
			_AirgapFaces = this.Attributes[AirgapFacesKey].AsArray<BlockFacing>(new BlockFacing[ ] { });			
			} else {
			_AirgapFaces = new BlockFacing[ ] { };
			}
			
			return _AirgapFaces;			
			}
		}
	}

	/*
	 public override bool TryPlaceBlock
	 public override ItemStack OnPickBlock
	 public override void OnNeighbourBlockChange
	 public override bool CanAttachBlockAt(
	 public override bool CanPlaceBlock(I
	 public override AssetLocation GetRotatedBlockCode(
	 public bool SideSolid(
	*/
}

