using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class LocksmithCmd : ServerChatCommand
	{
		private ICoreServerAPI ServerAPI;

		public LocksmithCmd( )
		{
			this.Command = "locksmith";
			this.Description = "ALTER LOCKS: Remove or Change keys and combos.";
			this.handler += LocksmithParser;
			this.Syntax = "remove / change / downgrade / info ";
			this.RequiredPrivilege = "locksmith"; 



		}

		private void LocksmithParser(IServerPlayer player, int groupId, CmdArgs args)
		{
			throw new NotImplementedException( );
		}
}
}
