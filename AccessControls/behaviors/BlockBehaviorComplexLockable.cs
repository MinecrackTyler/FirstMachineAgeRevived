using System;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FirstMachineAge
{

	/// <summary>
	/// Multi-use Lockable behavior for combo/key/fancy locks
	/// </summary>
	/// <remarks>Replaces the old behavior...</remarks>
	public class BlockBehaviorComplexLockable : BlockBehaviorLockable
	{
		private AccessControlsMod acm;
		private ILogger Logger;

		public BlockBehaviorComplexLockable(Block block) : base(block)
		{

		}

		public override void OnLoaded(ICoreAPI api)
		{
			acm = api.ModLoader.GetModSystem<AccessControlsMod>( );
			Logger = acm.Mod.Logger;
		}


		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			var blockPos = blockSel.Position.Copy();
			acm.AdjustBlockPostionForMultiBlockStructure(ref blockPos);

			if (acm.LockedForPlayer(blockPos, byPlayer)) //Checks for keys and known combos, ect...
			{
				LockStatus lockState = acm.LockState(blockPos, byPlayer);

				bool clientSide = world.Side.IsClient( );
				ICoreClientAPI clientAPI = clientSide ? (world.Api as ICoreClientAPI): null;

					switch (lockState) 
					{
					case LockStatus.None:
					handling = EnumHandling.PassThrough;
					return true;

					case LockStatus.ComboUnknown:
						//Does Not already know combo...
					if (clientSide) ShowComboLockGUI(clientAPI, byPlayer,blockPos);

					break;

					case LockStatus.KeyHave:
						if (clientSide) clientAPI.TriggerChatMessage("opened with a key...");
						handling = EnumHandling.PassThrough;
						return true;

					case LockStatus.KeyNope:
			          	//Did not have key...
						if (clientSide) clientAPI.TriggerIngameError(this, "locked", Lang.Get("ingameerror-nokey", new object[0]));
					break;

					case LockStatus.Unknown:
						if (clientSide) clientAPI.TriggerIngameError(this, "error", "Access-Control malfunction or lag?!");
					break;

					default:
						//Normal or 'default' lock:
						if (clientSide) clientAPI.TriggerIngameError(this, "locked", Lang.Get("ingameerror-locked", new object[0]));
					break;
					}

				handling = EnumHandling.PreventSubsequent;
				return false;
			}

			return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
		}


		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		{
		StringBuilder lockInfo = new StringBuilder( );
		//Is locked (by player); and How

			if (acm != null) 
			{
			BlockPos adjPos = pos.Copy( );
			acm.AdjustBlockPostionForMultiBlockStructure(ref adjPos);

			LockStatus lockstate = acm.LockState(adjPos, forPlayer);
			
			if (lockstate != LockStatus.None) 
			{
				var locktier = acm.LockTier(adjPos, forPlayer);
				var lockowner = acm.LockOwnerName(adjPos, forPlayer);
				lockInfo.AppendFormat("LockStatus: {0}, Owner: {1} @ Tier:{2}", lockstate,lockowner, locktier);		

				return lockInfo.ToString( );
			}

			}

		return String.Empty;
		}


		public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
		{
		#if DEBUG
		Logger.VerboseDebug("BlockBehaviorComplexLockable::OnBlockPlaced()");
		#endif

		if (world.Side.IsClient( )) {
		//OnBlockPlaced --> add entry to (client) cache!
		acm.AddPlaceHolder_SelfCache(blockPos);
		}
		else {
		//Placeholder for SERVER too!
		acm.AddPlaceHolder_Server(blockPos);
		}

		base.OnBlockPlaced(world, blockPos, ref handling);
		}

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
		{
		#if DEBUG
		Logger.VerboseDebug("BlockBehaviorComplexLockable::OnBlockRemoved()");
#endif

		if (world.Side.IsClient( )) {
		acm.RemovelaceHolder_SelfCache(pos);
		} else 
		{
		acm.DestroyLock(pos);
		}
					
		base.OnBlockRemoved(world, pos, ref handling);
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
		{

		if (world.Side.IsClient( )) {
		acm.RemovelaceHolder_SelfCache(pos);
		}
		else {
		acm.RemoveLock(pos, byPlayer);
		}

		base.OnBlockBroken(world, pos, byPlayer, ref handling);
		}


		protected void ShowComboLockGUI(ICoreClientAPI clientAPI, IPlayer byPlayer, BlockPos blockPos)
		{
		//Popup GUI window;

		var tier = acm.LockTier(blockPos, byPlayer );

		GuiDialog_ComboLock comboGUI = new GuiDialog_ComboLock(clientAPI,tier,blockPos );
		comboGUI.TryOpen( );
		}
}
}