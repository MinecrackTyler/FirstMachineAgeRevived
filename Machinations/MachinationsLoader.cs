using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Machinations
{
	public partial class MachinationsLoader : ModSystem
	{
		private ICoreAPI CoreAPI;
		private ICoreServerAPI ServerAPI;
		private ICoreClientAPI ClientAPI;
		private ServerCoreAPI ServerCore { get { return ServerAPI as ServerCoreAPI; }  }
		private ClientCoreAPI ClientCore { get { return ClientAPI as ClientCoreAPI; } }

		public override bool AllowRuntimeReload {
			get { return false; }
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
		return true;
		}

		public override double ExecuteOrder( )
		{
		return 0.10d;
		}

		public override void Start(ICoreAPI api)
		{
		this.CoreAPI = api;
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
		this.ServerAPI = api;
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
		this.ClientAPI = api;

		AttachClientCommands();
		}
	}
}

