using System;
using System.Collections.Generic;


using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TarKilns
{
	public class GenericTogglePortBlock : Block
	{
		private const string _openKey = @"opened";
		private const string _closeKey = @"closed";
		private const string _stateKey = @"state";


		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
		BlockPos pos = blockSel.Position;

		if (Variant[_stateKey] == _closeKey) {
		world.BlockAccessor.SetBlock(world.GetBlock(CodeWithVariant(_stateKey, _openKey)).Id, pos);
		world.PlaySoundAt(Sounds.Inside, pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5, byPlayer, true);
		}
		else {
		world.BlockAccessor.SetBlock(world.GetBlock(CodeWithVariant(_stateKey, _closeKey)).Id, pos);
		world.PlaySoundAt(Sounds.Inside, pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5, byPlayer, true);
		}

		return true;
		}
	}
}

