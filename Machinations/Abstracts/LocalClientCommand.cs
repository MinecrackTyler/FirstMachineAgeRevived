using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Machinations
{
	public class LocalClientCommand : ClientChatCommand
	{
		protected ICoreClientAPI ClientAPI;
		protected ILogger Logger;


		public LocalClientCommand(ICoreClientAPI _clientAPI) 
		{
			ClientAPI = _clientAPI;
			Logger = _clientAPI.World.Logger;
		}

		private LocalClientCommand( )
		{
			throw new NotSupportedException( );
		}


	}
}

