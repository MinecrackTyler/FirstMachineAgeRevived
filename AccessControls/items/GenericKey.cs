using System;

using Vintagestory.API.Common;

namespace FirstMachineAge
{
	public abstract class GenericKey : Item
	{
		private const string _keyIdKey = @"key_id";

		public uint KeyID { 
			get {
				if (this.Attributes.Exists && this.Attributes.KeyExists(_keyIdKey)) {
					uint keyId = ( uint )this.Attributes[_keyIdKey].AsInt(0);

					return keyId;
				}

				return 0;
			} 
		}

		//Attributes to -> AccessControlNode
		//Copy keyID, owner?

		//itemstack.Collectible.Attributes[_keyIdKey].AsInt(null);


	}
}

