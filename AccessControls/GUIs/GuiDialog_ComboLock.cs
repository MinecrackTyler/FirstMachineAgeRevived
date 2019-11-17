using System;

using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public class GuiDialog_ComboLock : GuiDialog
	{
		private ICoreClientAPI ClientAPI;
		private AccessControlsMod ACM;


		public GuiDialog_ComboLock(ICoreClientAPI capi, uint tier, BlockPos acnPosition) : base(capi)
		{
			ClientAPI = capi;
			ACM = ClientAPI.World.Api.ModLoader.GetModSystem<AccessControlsMod>( );

		ComposeElements( );//?
		}

		public override string ToggleKeyCombinationCode {
			get {
				return null;
			}
		}

		public override void OnRenderGUI(float deltaTime)
		{
		SingleComposer = base.SingleComposer;


		base.OnRenderGUI(deltaTime);
		}


		public override void OnGuiOpened( )
		{
			//Reset fields to zero - set # of boxes by tier.
		}

		private void ComposeElements( )
		{
			

		}



		//On 'Try' button click event -> send packet on channel
	}
}

