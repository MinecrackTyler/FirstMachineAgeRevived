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
			if (byEntity.World.Side == EnumAppSide.Client) 
			{
				ClientAPI = (byEntity.World.Api as ICoreClientAPI);
			}

			if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorLockable>( )) 
			{
				IPlayer player = (byEntity as EntityPlayer).Player;




				if (AccessControlsMod.LockedForPlayer(blockSel.Position, player, this.Code) == false)//already has a lock...
				{
					ClientAPI?.TriggerIngameError(this, "cannotlock", Lang.Get("ingameerror-cannotlock"));
				} else {
					ClientAPI?.ShowChatMessage(Lang.Get("lockapplied"));
					slot.TakeOut(1);
					slot.MarkDirty( );

					AccessControlsMod.ApplyLock(blockSel, player, slot);
				}

				handling = EnumHandHandling.PreventDefault;
				return;
			}

			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}
}

