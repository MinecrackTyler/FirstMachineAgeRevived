using System;

using System.ComponentModel;

namespace FirstMachineAge
{
	[DefaultValue(LockKinds.None)]
	public enum LockKinds : byte
	{
		None, //No lock here! 
		Classic,//Magic locks which open for their 'owner'
		Combination,//Mechanical locks that need manual input entry of a number or sequence to operate
		Key,//Mech. locks which need a specific item in inventory to open
		//Group,//ACL controlled lock, for Factions?

	}
}

