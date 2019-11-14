using System;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class GroupLocksCmd: ServerChatCommand
	{
		public GroupLocksCmd( )
		//What about Clan/Faction leaders - for shared combos?
		{
			this.Command = "grouplocks";
			this.Description = "Change lock permissions and assigend groupIDs.";
			//this.handler += LocksmithParser;
			this.Syntax = "grant [group/player] [player-name/group-name] / revoke [group/player] [player-name/group-name]";
			//this.RequiredPrivilege = "grouplocks"; 

		}
	}
}

