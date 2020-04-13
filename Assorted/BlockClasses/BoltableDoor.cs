using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class BoltableDoor : BlockBaseDoor
	{
		public BoltableDoor( )
		{
			
		}

		public override BlockFacing GetDirection( )
		{
			
		return BlockFacing.FromCode(this.Variant["horizontalorientation"]);
		}

		public override string GetKnobOrientation( )
		{
		return @"left";
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection selBox)
		{
		string info = $"Offset:{selBox.DidOffset}, Face:{selBox?.Face}, Hit:{selBox?.HitPosition}, B.POS:{selBox?.Position}, S.Index{selBox.SelectionBoxIndex}";

		world.Logger.VerboseDebug("BoltableDoor: [{0}]", info);

		if (!this.DoesBehaviorAllow(world, byPlayer, selBox)) {
		return true;
		}

		BlockPos position = selBox.Position;
		this.Open(world, byPlayer, position);

		world.PlaySoundAt(new AssetLocation("sounds/block/door"), ( double )(( float )position.X + 0.5f), ( double )(( float )position.Y + 0.5f), ( double )(( float )position.Z + 0.5f), byPlayer, true, 32f, 1f);

		this.TryOpenConnectedDoor(world, byPlayer, position);

		IClientPlayer clientPlayer = byPlayer as IClientPlayer;

		if (clientPlayer != null) {
		clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		}

		return true;
		}

		protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position)
		{
		//throw new NotImplementedException( );
		}

		protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
		{
		if (this.IsUpperHalf) 
		{
		return pos.DownCopy( );		
		}

		return pos.UpCopy( );
		}

		public bool IsUpperHalf
		{
			get { return this.Variant["part"] == "up";}
		}

	}
}

