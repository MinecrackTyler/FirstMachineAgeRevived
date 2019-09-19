using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class ItemCombolock : GenericLock
	{		
		



		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			if (byEntity.World.Side.IsClient()) 
			{
				ClientAPI = (byEntity.World.Api as ICoreClientAPI);
			}

			if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorLockable>( )) 
			{
				IPlayer player = (byEntity as EntityPlayer).Player;


				if (AccessControlsMod.LockState(blockSel.Position, player) != LockStatus.None )//already has a lock...?
				{
					ClientAPI?.TriggerIngameError(this, "cannotlock", Lang.Get("ingameerror-cannotlock"));
				} else {
					AccessControlsMod.ApplyLock(blockSel, player, slot);

					ClientAPI?.ShowChatMessage(Lang.Get("lockapplied"));
					slot.TakeOut(1);
					slot.MarkDirty( );
				}

				handling = EnumHandHandling.PreventDefault;
				return;
			}

			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}
}

