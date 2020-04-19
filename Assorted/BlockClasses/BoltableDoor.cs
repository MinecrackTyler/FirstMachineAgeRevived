using System;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

		internal BoltableDoorBlockEntity Entity(BlockPos here)
		{
		//Always in Upper half
		BlockPos upperPos;
		if (this.IsUpperHalf) {
		upperPos = here.Copy( );
		}
		else {
		upperPos = here.UpCopy( );
		}

		return api.World.BlockAccessor.GetBlockEntity(upperPos) as BoltableDoorBlockEntity;			
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
		var doorEntity = this.Entity(position);
		var clientPlayer = byPlayer as IClientPlayer;
		if (selBox.SelectionBoxIndex == 0) 
		{						
			if (doorEntity.Bolted == false) 
			{
			//Normal Door behavior			
			this.TryOpenConnectedDoor(world, byPlayer, position);

			
			clientPlayer?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			
			}
			else 
			{
			//Door in Bolted shut state; 
			(api as ICoreClientAPI).TriggerIngameError(this, "doorbolted", Lang.Get("ingameerror-door-boltedclosed"));
			}
		}else if (selBox.SelectionBoxIndex == 1) {
		//Bolt toggle behavior
		world.PlaySoundAt(new AssetLocation(GlobalConstants.DefaultDomain,"sounds/tool/padlock"), ( double )(( float )position.X + 0.5f), ( double )(( float )position.Y + 0.5f), ( double )(( float )position.Z + 0.5f), byPlayer, true, 32f, 1f);
		doorEntity.Bolted = !doorEntity.Bolted;
		//TODO: Start bolt animation

		}
		
		return true;
		}

		/// <summary>
		/// More like: Toggle Open/Close state
		/// </summary>
		/// <param name="world">World.</param>
		/// <param name="byPlayer">By player.</param>
		/// <param name="position">Position.</param>
		protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position)
		{
		var doorEntity = this.Entity(position);
		doorEntity.Bolted = false;
		world.PlaySoundAt(new AssetLocation(GlobalConstants.DefaultDomain, "sounds/block/door"), ( double )(( float )position.X + 0.5f), ( double )(( float )position.Y + 0.5f), ( double )(( float )position.Z + 0.5f), byPlayer, true, 32f, 1f);

		BlockPos upperPos, lowerPos;
		Block upperBlock, lowerBlock;
		if (this.IsUpperHalf) {
		upperPos = position.Copy( );
		lowerPos = position.DownCopy( );
		}
		else {
		lowerPos = position.Copy( );
		upperPos = position.UpCopy( );
		}

		AssetLocation upperCode = CodeWithVariants(
			new Dictionary<string, string>( ) {
					{ "horizontalorientation", GetDirection().Code },
					{ "part", "up" },
					{ "state", IsOpen ? "closed" : "opened" }
					});
		upperBlock = world.BlockAccessor.GetBlock(upperCode);

		world.BlockAccessor.ExchangeBlock(upperBlock.BlockId, upperPos);
		world.BlockAccessor.MarkBlockDirty(upperPos);

		AssetLocation lowerCode = CodeWithVariants(
			new Dictionary<string, string>( ) {
					{ "horizontalorientation", GetDirection().Code },
					{ "part", "down"  },
					{ "state", IsOpen ? "closed" : "opened" }
					});
		lowerBlock = world.BlockAccessor.GetBlock(lowerCode);

		world.BlockAccessor.ExchangeBlock(lowerBlock.BlockId, lowerPos);
		world.BlockAccessor.MarkBlockDirty(lowerPos);

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

		public bool IsOpen {
			get { return this.Variant["state"] == "opened"; }//state: ["closed", "opened"]
		}

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
		{
		BlockPos abovePos = blockSel.Position.AddCopy(0, 1, 0);
		IBlockAccessor ba = world.BlockAccessor;

		if (ba.GetBlockId(abovePos) == 0 && CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
		BlockFacing[ ] horVer = SuggestedHVOrientation(byPlayer, blockSel);
						

		AssetLocation downBlockCode = CodeWithVariants(new Dictionary<string, string>( ) {
					{ "horizontalorientation", horVer[0].Code },
					{ "part", "down" },
					{ "state", "closed" },
				});

		Block downBlock = ba.GetBlock(downBlockCode);

		AssetLocation upBlockCode = downBlock.CodeWithVariant("part", "up");
		Block upBlock = ba.GetBlock(upBlockCode);
						
		ba.SetBlock(downBlock.BlockId, blockSel.Position);
		ba.SetBlock(upBlock.BlockId, abovePos);
		return true;
		}

		return false;
		}

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
		{
		BlockPos upperPos, lowerPos;
		Block upperBlock, lowerBlock;
		if (this.IsUpperHalf) {
			upperPos = pos.Copy( );
			lowerPos = pos.DownCopy( );
		}
		else 
		{
			lowerPos = pos.Copy( );
			upperPos = pos.UpCopy( );
		}
		upperBlock = world.BlockAccessor.GetBlock(upperPos);
		lowerBlock = world.BlockAccessor.GetBlock(lowerPos);

		if (upperBlock != null && upperBlock is BoltableDoor ) 
		{
		world.BlockAccessor.RemoveBlockEntity(upperPos);
		world.BlockAccessor.SetBlock(0, upperPos);
		}

		
		if (lowerBlock != null &&  lowerBlock is BoltableDoor) {
		world.BlockAccessor.SetBlock(0, lowerPos);
		}

		}
	}
}

