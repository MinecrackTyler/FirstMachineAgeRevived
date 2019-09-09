using System;

using System.ComponentModel;

namespace FirstMachineAge
{
	[DefaultValue(LockStatus.None)]
	public enum LockStatus : byte
	{
		None,//Do nothing = 'no lock here'.
		Locked,//Old behavior
		ComboUnknown,//GUI
		ComboKnown,//GUI prefilled?
		KeyHave,//Message?
		KeyNope,//Message!
		Unknown, //for cache non-update state? - e.g. LAG while updating.
	}
}

