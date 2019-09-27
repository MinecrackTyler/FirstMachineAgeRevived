using System;

using Vintagestory.API.Client;

namespace FirstMachineAge
{
	public class GuiDialog_ComboLock : GuiDialog
	{
		private ICoreClientAPI ClientAPI;
		private AccessControlsMod ACM;


		public GuiDialog_ComboLock(ICoreClientAPI capi) : base(capi)
		{
			ClientAPI = capi;
			ACM = ClientAPI.World.Api.ModLoader.GetModSystem<AccessControlsMod>( );
		}

		public override string ToggleKeyCombinationCode {
			get {
				return null;
			}
		}


		//On 'Try' button click event -> send packet on channel
	}
}

