using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

using Vintagestory.GameContent;

namespace ConstructionSupport
{
	public class DeckworkHorizontalBlock : GenericScaffold
	{		
		public static readonly string BlockClassName = @"DeckworkHorizontal";


		public DeckworkHorizontalBlock( )
		{
		}

		/*
		 "deckwork_horiz":	
		Outer edge N/E/S/W face: MUST contact 1 "truss_vert" OR Hard-surface > (brace)+Deck  (Transom?)+Deck



		[If 'attached' face/block breaks - scaffold(s) break off surface too!]
		[If B.U.D. with solid (non-truss) block Above - scaffold(s) breaks !]
		 */

		/// <summary>
		/// Prevent most other 'normal' blocks from attaching to this.
		/// </summary>
		/// <returns>The <see cref="T:System.Boolean"/>.</returns>
		/// <param name="world">World.</param>
		/// <param name="block">Block.</param>
		/// <param name="pos">Position.</param>
		/// <param name="blockFace">Block face.</param>
		public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace)
		{
		if (ValidAttachmentFaces.Contains(blockFace)) {

		if (block is TrussBlock) return true;

		if (IsHardSurface(world, block, pos, blockFace)) return true;


		}
		else {
		//Wrong Side
		if (api.Side.IsClient()) {
		(api as ICoreClientAPI).TriggerIngameError(this, "surface_unplacable", Lang.Get("surface_unplacable"));
		}
		}

		return false;
		}

		//If above block is unsupported material; BREAK OFF!
		public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{

		if (pos.Above(neibpos)) {
		var blockAbove = api.World.BlockAccessor.GetBlock(neibpos);
		if (blockAbove.MaterialDensity > 200 && blockAbove.HasBehavior<BlockBehaviorUnstableFalling>( )) {		
		//blockAbove.IsSolid( ) &&
		world.BlockAccessor.BreakBlock(pos, null);
		}
		}

		}
	}
}

