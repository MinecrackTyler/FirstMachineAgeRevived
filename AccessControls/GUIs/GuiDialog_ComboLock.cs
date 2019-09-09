using System;

using Vintagestory.API.Client;

namespace FirstMachineAge
{
	public class GuiDialog_ComboLock : GuiDialog
	{
		private ICoreClientAPI ClientAPI;


		public GuiDialog_ComboLock(ICoreClientAPI capi) : base(capi)
		{
			ClientAPI = capi;

		}

		public override string ToggleKeyCombinationCode {
			get {
				return String.Empty;
			}
		}
	}
}

