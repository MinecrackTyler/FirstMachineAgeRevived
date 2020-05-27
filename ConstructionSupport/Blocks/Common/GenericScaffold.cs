using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ConstructionSupport
{
	public abstract class GenericScaffold : Block
	{
		internal const string ValidAttachmentFacesKey = @"ValidAttachemtFaces";
		internal const string AirgapFacesKey = @"AirgapFaces";
		internal const string RotationKey = @"rot";

		internal BlockFacing[ ] _ValidAttachmentFaces;
		internal BlockFacing[ ] _AirgapFaces;


		/// <summary>
		/// Where can this block be stuck onto?
		/// </summary>
		/// <value>The valid attachment faces.</value>
		/// <remarks>Decode from readonly Json properties</remarks>
		public virtual BlockFacing[] ValidAttachmentFaces 
		{ 
			get
			{
			if (_ValidAttachmentFaces != null) return _ValidAttachmentFaces;

			if (this.Attributes[ValidAttachmentFacesKey].Exists) {
			var directions = this.Attributes[ValidAttachmentFacesKey].AsArray<string>( );
			_ValidAttachmentFaces = directions.Select(dir => BlockFacing.FromCode(dir)).ToArray( );
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

		public override bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, Vintagestory.API.Common.Entities.EntityProperties type, Vintagestory.API.Common.Entities.BaseSpawnConditions sc)
		{
		return false;//Never, anything.
		}

		protected bool IsHardSurface(IBlockAccessor world, Block checkBlock, BlockPos checkPos, BlockFacing checkFace)
		{
			if (checkBlock != null && checkBlock.MatterState == EnumMatterState.Solid &&
			    (
			    checkBlock.BlockMaterial == EnumBlockMaterial.Brick ||
			 	checkBlock.BlockMaterial == EnumBlockMaterial.Ceramic ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Mantle ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Ore ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Other ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Stone ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Wood 			   
			   ))
			{
			return checkBlock.CanAttachBlockAt(world, this, checkPos, checkFace);
			//checkBlock.SideSolid[blockFace.Index]
		}
			
		return false;
		}

		protected BlockFacing OwnRotation 
		{
			get
			{
			if (this.Variant.ContainsKey(RotationKey)) 
			{
			return BlockFacing.FromCode(this.Variant[RotationKey]);
			}
			return BlockFacing.NORTH;
			}
		}

		protected Block RotateToFace(BlockFacing turnTo)
		{
		AssetLocation rotatedCode = CodeWithVariant(RotationKey, turnTo.Code);
		var rotBlock = api.World.BlockAccessor.GetBlock(rotatedCode);
		return rotBlock;
		}

	}



	/*
	 public override bool TryPlaceBlock
	 public virtual bool DoPlaceBlock
	 public override ItemStack OnPickBlock
	 public override void OnNeighbourBlockChange
	 public override bool CanAttachBlockAt(
	 public override bool CanPlaceBlock(I
	 public override AssetLocation GetRotatedBlockCode(
	 public bool SideSolid(
	*/
}

