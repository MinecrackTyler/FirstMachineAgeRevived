using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class BlockBehaviorVerticalOrientation : BlockBehavior
	{
		private readonly string verticalKey;

		/* 
		 * "abstract/verticalorientation"
		*/


		public BlockBehaviorVerticalOrientation(Block block) : base(block)
        { 
		foreach (var entry in block.VariantStrict) {
		//api.World.Logger.VerboseDebug($"{entry.Key}: {entry.Value}");
		if (string.Equals(entry.Value, BlockFacing.UP.Code, StringComparison.InvariantCultureIgnoreCase) ||
			string.Equals(entry.Value, BlockFacing.DOWN.Code, StringComparison.InvariantCultureIgnoreCase)
			) 
			{
			//block.api.World.Logger.VerboseDebug($"VerticalKey is: {entry.Key} - [{block.Code}]");//Key of "up" ?
			verticalKey = entry.Key;
			break;
			}
		}
		}

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
		{
		if (world.Side.IsClient()) return true;

		Block blockRotated;
		handling = EnumHandling.PreventDefault;

		if (blockSel.Face.IsVertical) {		
		blockRotated = world.GetBlock(block.CodeWithVariant(verticalKey, blockSel.Face.Code ));
		
		}
		else {
		var closestFace = blockSel.HitPosition.Y < 0.5f ? BlockFacing.UP : BlockFacing.DOWN;

		blockRotated = world.GetBlock(block.CodeWithVariant(verticalKey, closestFace.Code));
		}

		if (blockRotated.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
		//world.BlockAccessor.SetBlock(blockRotated.BlockId, blockSel.Position);
		#if DEBUG
		world.Logger.VerboseDebug($"Rotated: [{blockRotated.Code}] - from [{block.Code}]");
		#endif
		blockRotated.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

		return true;
		}

		return false;
		}


		//Just opposite of whatever 'THIS' is
		public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
		{
		//return base.GetVerticallyFlippedBlockCode(ref handling);
		handling = EnumHandling.PreventDefault;

		if (string.Equals(block.VariantStrict[verticalKey], BlockFacing.UP.Code, StringComparison.InvariantCultureIgnoreCase)) {		
		return block.CodeWithVariant(verticalKey, BlockFacing.DOWN.Code); 
		}
		else {
		return block.CodeWithVariant(verticalKey, BlockFacing.UP.Code); 
		}
		}

	}
}

