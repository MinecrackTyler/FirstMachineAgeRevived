using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace FirstMachineAge
{
	public class GenericKey : Item
	{		
		
		public static int? KeyID(ItemStack sourceStack)
		{
			if (sourceStack.Attributes.HasAttribute(AccessControlsMod._KeyIDKey)) {
				return sourceStack.Attributes.GetInt(AccessControlsMod._KeyIDKey);
			} else {
				return null;
			}
		}

		public static BlockPos LockLocation(ItemStack sourceStack)//Location of matching ACN
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
		targetStack.Attributes.SetBlockPos(AccessControlsMod._LockLocationKey, location.Copy());
		targetStack.Attributes.SetString(AccessControlsMod._itemDescription, acn.NameOfLock);
		}


		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);		

		#if DEBUG
		int? keyId = KeyID(inSlot.Itemstack);
		dsc.AppendFormat("\nKey #{0:D},", keyId);
		#endif


		string desc = Description(inSlot.Itemstack);
		if (!string.IsNullOrWhiteSpace(desc)) {
		dsc.AppendFormat("\n'{0}'", desc);
		}

		}




	}
}

