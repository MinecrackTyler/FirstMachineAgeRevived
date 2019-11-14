using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	internal sealed class LocksmithCmd : ServerChatCommand
	{
		private ICoreServerAPI ServerAPI;
		private ILogger Logger { get; set; }
		private AccessControlsMod AccessControlsMod { get; set; }

		public LocksmithCmd(ICoreServerAPI _coreAPI)
		{
		this.Command = "locksmith";
		this.Description = "ALTER LOCKS: Remove or Change keys and combos.";
		this.handler += LocksmithParser;
		this.Syntax = "remove / change / downgrade / info ";
		this.RequiredPrivilege = "locksmith";

		if (_coreAPI.Side.IsServer( )) 
			{
			this.ServerAPI = _coreAPI;
			this.Logger = this.ServerAPI.World.Logger;
			AccessControlsMod = ServerAPI.World.Api.ModLoader.GetModSystem<AccessControlsMod>( );
			}

		}

		private LocksmithCmd( )
		{

		}

		private void LocksmithParser(IServerPlayer player, int groupId, CmdArgs args)
		{
		if (args.Length > 0) {
		string command = args.PopWord( );
		switch (command) {
		case "nodes":
			PrintNodes(player, groupId, args);
			break;

		case "keys":
			PrintKeys(player, groupId, args);
			break;

		case "remove":

			break;

		case "destroy":

			break;

		default:
			player.SendMessage(GlobalConstants.CurrentChatGroup, "unecognised command", EnumChatType.CommandError);
			break;

		}

		//List All ACN in a chunk (current)
		//List All ACN in a chunk {param}
		//Remove ALL ACN in a chunk (current)
		//Remove All ACN in a chunk {param}
		//Destroy pointed Lock (and ACN for it)
		}
		else {
		player.SendMessage(GlobalConstants.CurrentChatGroup, "no help yet", EnumChatType.CommandSuccess);

		}

		}

		private void PrintNodes(IServerPlayer player, int groupId, CmdArgs args )
		{
		if (args.Length > 0) {
		var chunkPos = args.PopVec3i(null);
		}
		else {
		BlockPos location = player.Entity.ServerPos.AsBlockPos; ;
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(location);

		var acn_List = AccessControlsMod.RetrieveACNs_ByChunk(chunkPos);

		foreach (var acn in acn_List) {
		string name = ServerAPI.World.PlayerByUid(acn.Value.OwnerPlayerUID).PlayerName;
					player.SendMessage(GlobalConstants.InfoLogChatGroup, $"Node@{acn.Key}:{acn.Value.LockStyle} own:{name} '{acn.Value.NameOfLock}'\n", EnumChatType.CommandSuccess);
		}

		}
		}

		private void PrintKeys(IServerPlayer player, int groupId, CmdArgs args )
		{
			var key_List = AccessControlsMod.RetrieveKnownKeys( );

		foreach (var acn in key_List) {
				string name = ServerAPI.World.PlayerByUid(acn.Value.Value.OwnerPlayerUID).PlayerName;
				player.SendMessage(GlobalConstants.InfoLogChatGroup, $"Key#{acn.Key} @{acn.Value.Key} {acn.Value.Value.LockStyle} own:{name} '{acn.Value.Value.NameOfLock}'\n", EnumChatType.CommandSuccess);
		}
		}


	}
}
