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
	/// Multi-use Lockable behavior for combo/key/other locks
	/// </summary>
	/// <remarks>Replaces the old behavior...</remarks>
	public class BlockBehaviorComplexLockable : BlockBehaviorLockable
	{
		private AccessControlsMod acm;

		public BlockBehaviorComplexLockable(Block block) : base(block)
		{

		}

		public override void OnLoaded(ICoreAPI api)
		{
			acm = api.ModLoader.GetModSystem<AccessControlsMod>( );
		}


		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			var blockPos = blockSel.Position.Copy();
			acm.AdjustBlockPostionForMultiBlockStructure(ref blockPos);

			if (acm.LockedForPlayer(blockPos, byPlayer)) //Checks for keys and known combos, ect...
			{
				LockStatus lockState = acm.LockState(blockPos, byPlayer);

				if (world.Side == EnumAppSide.Client) 
				{
					ICoreClientAPI clientAPI = (world.Api as ICoreClientAPI);

					switch (lockState) 
					{
					case LockStatus.ComboUnknown:
						//Does Not already know combo...
						ShowComboLockGUI(clientAPI, byPlayer,blockPos);

					break;

					case LockStatus.KeyHave:
						clientAPI.TriggerChatMessage("opened with a key...");
						handling = EnumHandling.PassThrough;
						return true;

					case LockStatus.KeyNope:
			          	//Did not have key...
						clientAPI.TriggerIngameError(this, "locked", Lang.Get("ingameerror-nokey", new object[0]));
					break;

					default:
						//Normal or 'default' lock:
						clientAPI.TriggerIngameError(this, "locked", Lang.Get("ingameerror-locked", new object[0]));
					break;
					}
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

		protected void ShowComboLockGUI(ICoreClientAPI clientAPI, IPlayer byPlayer, BlockPos blockPos)
		{
		//Popup GUI window;

		

		GuiDialog_ComboLock comboGUI = new GuiDialog_ComboLock(clientAPI );
		comboGUI.TryOpen( );
		}
}
}