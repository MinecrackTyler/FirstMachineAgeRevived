using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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


		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			LockStatus lockState = acm.LockState(blockSel.Position, byPlayer);

			if (acm.LockedForPlayer(blockSel.Position, byPlayer)) //Checks for keys and known combos, ect...
			{
				if (world.Side == EnumAppSide.Client) 
				{
					ICoreClientAPI clientAPI = (world.Api as ICoreClientAPI);

					switch (lockState) 
					{
					case LockStatus.ComboUnknown:
						//Does Not already know combo...
						ShowComboLockGUI(world, byPlayer,blockSel);

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

		protected void ShowComboLockGUI(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			//Popup GUI window;

			//On 'Try' button click event -> send packet on channel

			byte[] comboGuess = null;

			acm.Send_Lock_GUI_Message(byPlayer.PlayerUID, blockSel.Position, comboGuess);
		}
}
}