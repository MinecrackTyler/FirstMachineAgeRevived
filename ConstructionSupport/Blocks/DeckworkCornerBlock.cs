using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

using Vintagestory.GameContent;

namespace ConstructionSupport
{
	public class DeckworkCornerBlock : GenericScaffold
	{
		public static readonly string BlockClassName = @"DeckworkCorner";



		public DeckworkCornerBlock( )
		{
		}

		/*
		 "deckwork_corner":	
		Diagonal corner N+W / N+E / S+W / S+E : MUST contact 1 other "deckwork_horiz" > (brace)+Deck
		--Must be in contact(NEWS) with 1 other "deckwork_horiz" (or more)
		--Directly below: AIR ONLY!

		[If 'attached' side-scafolds - block breaks off too!]
		[If B.U.D. with solid (non-truss) block Above - scaffold(s) breaks !]
		 */

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
		{
		var placeSpot = blockSel.Position.AddCopy(blockSel.Face.Opposite, 1);
		var lookSpot = blockSel.Position.Copy( ).Offset(blockSel.Face.Opposite);
		var surfaceBlock = world.BlockAccessor.GetBlock(lookSpot);

		if (base.ValidAttachmentFaces.Contains(blockSel.Face)) {					

		//of the 4 Corners - any 1 a solid block; AND attachment to deckwork on faces... 
		if (IsDeckwork(world.BlockAccessor, lookSpot) 
				&& CheckCornerSolid(world.BlockAccessor, placeSpot )) 
		{					
		#if DEBUG
		api.World.Logger.VerboseDebug($"Success: {blockSel.Face} for {this.Code} onto {surfaceBlock.Code} @ {blockSel.Position}");
		#endif

		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
		return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		}
		}
		}

		#if DEBUG
		api.World.Logger.VerboseDebug($"Attempt to place fails: {blockSel.Face} for {this.Code} onto {surfaceBlock.Code}");
		#endif

		failureCode = "surface_solid_diagonal";
		return false;
		}

		public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
		{
		bool result = true;
		bool preventDefault = false;

		foreach (BlockBehavior behavior in BlockBehaviors) {
		EnumHandling handled = EnumHandling.PassThrough;

		bool behaviorResult = behavior.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handled);

		if (handled != EnumHandling.PassThrough) {
		result &= behaviorResult;
		preventDefault = true;
		}

		if (handled == EnumHandling.PreventSubsequent) return result;
		}

		if (preventDefault) return result;

		var rotatedBlockId = RotateToFace(blockSel.Face.Opposite);
		//Switcheroo!
		world.BlockAccessor.SetBlock(rotatedBlockId.BlockId, blockSel.Position, byItemStack);


		return true;
		}




		/// <summary>
		/// Prevent most other 'normal' blocks from attaching to this.
		/// </summary>
		/// <returns>The <see cref="T:System.Boolean"/>.</returns>
		/// <param name="world">World.</param>
		/// <param name="block">Block.</param>
		/// <param name="pos">Position.</param>
		/// <param name="blockFace">Block face.</param>
		public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea)
		{

		if (block.HasBehavior<BlockBehaviorLadder>( )) return true;

		#if DEBUG
		api.World.Logger.VerboseDebug($"Reject Attach: {blockFace} for {this.Code} onto {block.Code} @ {pos}");
		#endif

		return false;
		}

		//If above block is unsupported/interfereing material; BREAK OFF!
		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{
		//Above: Dropping blocks cause breakage!
		if (pos.Above(neibpos)) {
		var blockAbove = api.World.BlockAccessor.GetBlock(neibpos);

		if (blockAbove.IsGaseous( ) == false) {
		if (blockAbove.RainPermeable
					|| blockAbove.HasBehavior<BlockBehaviorLadder>( )
					|| blockAbove.HasBehavior<BlockBehaviorOmniAttachable>( )
				) return;

		if (blockAbove.MaterialDensity > 200 || blockAbove.HasBehavior<BlockBehaviorUnstableFalling>( ))
			world.BlockAccessor.BreakBlock(pos, null);
		#if DEBUG
		api.World.Logger.VerboseDebug($"Collapsing! {this.Code} because {blockAbove.Code} @ {pos}");
		#endif
		}
		}
		else
		//Corner attached: Missing supports cause brakeage!
		if (pos.OnSide(this.OwnRotation, neibpos)) {
		var mabeyBlock = api.World.BlockAccessor.GetBlock(neibpos);
		if (mabeyBlock == null || mabeyBlock.IsGaseous( ))
			world.BlockAccessor.BreakBlock(pos, null);
		#if DEBUG
		api.World.Logger.VerboseDebug($"Lost support! V.Faces: {string.Join("+", this.ValidAttachmentFaces.Select(bf => bf.Code))} , Facing:{OwnRotation.Code}, Other:{(mabeyBlock == null ? "null" : mabeyBlock.BlockMaterial.ToString( ))}");
		#endif
		}
		//Things below arn't considered.
		}
	}
}

