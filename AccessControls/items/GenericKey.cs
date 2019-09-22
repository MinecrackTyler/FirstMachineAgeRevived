using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public abstract class GenericKey : Item
	{
		
		public uint? KeyID(ItemSlot sourceSlot)
		{
			if (sourceSlot.Itemstack.Attributes.HasAttribute(AccessControlsMod._KeyIDKey)) {
				return ( uint? )sourceSlot.Itemstack.Attributes.GetInt(AccessControlsMod._KeyIDKey);
			} else {
				return new uint( );
			}
		}

		public BlockPos LockLocation(ItemSlot sourceSlot)
		{
			if (sourceSlot.Itemstack.Attributes.HasAttribute(AccessControlsMod._LockLocationKey)) {
				return sourceSlot.Itemstack.Attributes.GetBlockPos(AccessControlsMod._LockLocationKey);
			}
			else {
				return null;
			}
		}


		/*
		public string Description 
		{
			get;
		}
		*/		


		//Attributes to -> AccessControlNode
		//Copy keyID, owner?

		//itemstack.Collectible.Attributes[_keyIdKey].AsInt(null);


	}
}

