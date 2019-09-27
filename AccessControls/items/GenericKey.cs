using System;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public class GenericKey : Item
	{
		
		public static int KeyID(ItemStack sourceStack)
		{
			if (sourceStack.Attributes.HasAttribute(AccessControlsMod._KeyIDKey)) {
				return sourceStack.Attributes.GetInt(AccessControlsMod._KeyIDKey);
			} else {
				return new int( );
			}
		}

		public static BlockPos LockLocation(ItemStack sourceStack)
		{
			if (sourceStack.Attributes.HasAttribute(AccessControlsMod._LockLocationKey)) {
				return sourceStack.Attributes.GetBlockPos(AccessControlsMod._LockLocationKey);
			}
			else {
				return null;
			}
		}

		public string Description(ItemStack sourceStack)
		{
			if (sourceStack.Attributes.HasAttribute(AccessControlsMod._itemDescription)) 
			{
			return sourceStack.Attributes.GetString(AccessControlsMod._itemDescription);
			}

		return string.Empty;
		}

		internal static void WriteACL_ItemStack(ref ItemStack targetStack, AccessControlNode acn, BlockPos location)
		{
		targetStack.Attributes.SetInt(AccessControlsMod._KeyIDKey, acn.KeyID.Value);
		targetStack.Attributes.SetBlockPos(AccessControlsMod._LockLocationKey, location);
		targetStack.Attributes.SetString(AccessControlsMod._itemDescription, acn.NameOfLock);
		}


		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);		

		#if DEBUG
		int keyId = KeyID(inSlot.Itemstack);
		dsc.AppendFormat("\nKey #{0:D},", keyId);
		#endif


		string desc = Description(inSlot.Itemstack);
		dsc.AppendFormat("\n' {0} '", desc);
		}




	}
}

