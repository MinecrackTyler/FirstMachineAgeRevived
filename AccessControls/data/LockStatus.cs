using System;

using System.ComponentModel;

namespace FirstMachineAge
{
	[DefaultValue(LockStatus.None)]
	public enum LockStatus : byte
	{
		None,//Do nothing = 'no lock here'.
		Locked,//Old behavior
		Unlocked,//Old style, but with ACL entry for your group/self
		ComboUnknown,//GUI
		ComboKnown,//pass-thru; skip GUI
		KeyHave,//Message? (used key in inventory)
		KeyNope,//Message! (none of your keys work on this lock)
		Unknown, //for cache non-update state? - e.g. LAG while updating. ** TIMEOUT **
	}
}

