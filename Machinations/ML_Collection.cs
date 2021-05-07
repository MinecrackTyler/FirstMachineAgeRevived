using System;

using Vintagestory.API.Common;

namespace Machinations
{
	public partial class MachinationsLoader : ModSystem
	{

		private void AttachClientCommands( )
		{
			ClientAPI.RegisterCommand(new MechNetAnalyser(ClientAPI));
		}
	}
}

