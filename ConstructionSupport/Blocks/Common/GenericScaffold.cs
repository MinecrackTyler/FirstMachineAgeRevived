using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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

		public override bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
		{
		return false;//Never, anything.
		}

		public bool IsHardSurface(IBlockAccessor world, Block checkBlock, BlockPos checkPos, BlockFacing checkFace)
		{
			if (checkBlock != null && checkBlock.MatterState == EnumMatterState.Solid &&
			    (
			    checkBlock.BlockMaterial == EnumBlockMaterial.Brick ||
			 	checkBlock.BlockMaterial == EnumBlockMaterial.Ceramic ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Mantle ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Ore ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Other ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Stone ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Metal ||
			    checkBlock.BlockMaterial == EnumBlockMaterial.Wood 			   
			   ))
			{
			return checkBlock.CanAttachBlockAt(world, this, checkPos, checkFace);
			//checkBlock.SideSolid[blockFace.Index]
		}
			
		return false;
		}

		public bool CheckCornerSolid(IBlockAccessor world,  BlockPos checkPos )
		{
		//Visit all diagonal combinations
		Stack<BlockPos> checkList = new Stack<BlockPos>( );

		checkList.Push(checkPos.Copy( ).Add( 1, 1, 0));
		checkList.Push(checkPos.Copy( ).Add( 1,-1, 0));
		checkList.Push(checkPos.Copy( ).Add(-1, 1, 0));
		checkList.Push(checkPos.Copy( ).Add(-1,-1, 0));
		Block toCheck;
		while (checkList.Count > 0 )
		{
		toCheck = world.GetBlock(checkList.Pop( ));
		if (toCheck != null && toCheck.IsSolid()) return true;
		}				

		return false;
		}

		public bool IsDeckwork(IBlockAccessor world, BlockPos checkPos)
		{
		var aBlock = world.GetBlock(checkPos);

		if (aBlock != null && aBlock is GenericScaffold) {
		return true;
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

