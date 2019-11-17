using System;
using System.Text;

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
		this.Syntax = "nodes / keys / remove / info ";
		this.RequiredPrivilege = "locksmith";

		if (_coreAPI.Side.IsServer( )) {
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
			BlockPos targetPos;
			//if (args.Length > 0) {
			//targetPos = args.PopVec3i(null).AsBlockPos;

			if (player.CurrentBlockSelection == null) return;
			targetPos = player.CurrentBlockSelection.Position.Copy( );

			RemoveLock(targetPos, player);
			break;

		case "info":
			if (player.CurrentBlockSelection == null) return;
			targetPos = player.CurrentBlockSelection.Position.Copy( );

			PrintLockInfo(targetPos, player);
			break;

		case "destroy":

			break;

		default:
			player.SendMessage(GlobalConstants.CurrentChatGroup, "unrecognised command", EnumChatType.CommandError);
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

		private void RemoveLock(BlockPos targetPos, IServerPlayer player)
		{
		if (ServerAPI.World.BlockAccessor.IsValidPos(targetPos)) {
		var actualThing = ServerAPI.World.BlockAccessor.GetBlock(targetPos);
		if (actualThing.HasBehavior<BlockBehaviorComplexLockable>( )) {
		var acn = AccessControlsMod.RetrieveACN(targetPos);

		if (acn != null) {
		if (acn.LockStyle != LockKinds.None) {
		AccessControlsMod.RemoveLock(targetPos, player);
		player.SendMessage(GlobalConstants.CurrentChatGroup, "OK, Lock Removed !", EnumChatType.CommandSuccess);
		}
		else { player.SendMessage(GlobalConstants.CurrentChatGroup, "ACN in Default 'None' state. (No lock)", EnumChatType.CommandError); }
		}
		else { player.SendMessage(GlobalConstants.CurrentChatGroup, "NO ACN (or lock...) there!", EnumChatType.CommandError); }
		}
		else { player.SendMessage(GlobalConstants.CurrentChatGroup, "Thing selected can't be lockable anyways...", EnumChatType.CommandError); }
		}
		else {
		player.SendMessage(GlobalConstants.CurrentChatGroup, "Invalid location selected", EnumChatType.CommandError);
		}

		}

		private void PrintNodes(IServerPlayer player, int groupId, CmdArgs args)
		{
		if (args.Length > 0) {
		var chunkPos = args.PopVec3i(null);
		}
		else {
		BlockPos location = player.Entity.ServerPos.AsBlockPos;
		Vec3i chunkPos = ServerAPI.World.BlockAccessor.ToChunkPos(location);

		var acn_List = AccessControlsMod.RetrieveACNs_ByChunk(chunkPos);

		foreach (var acn in acn_List) {
		string name = ServerAPI.World.PlayerByUid(acn.Value.OwnerPlayerUID).PlayerName;
		player.SendMessage(GlobalConstants.InfoLogChatGroup, $"Node@{acn.Key}:{acn.Value.LockStyle} own:{name} '{acn.Value.NameOfLock}'\n", EnumChatType.CommandSuccess);
		}

		}
		}

		private void PrintKeys(IServerPlayer player, int groupId, CmdArgs args)
		{
		var key_List = AccessControlsMod.RetrieveKnownKeys( );

		foreach (var acn in key_List) {
		string name = ServerAPI.World.PlayerByUid(acn.Value.Value.OwnerPlayerUID).PlayerName;
		player.SendMessage(GlobalConstants.InfoLogChatGroup, $"Key#{acn.Key} @{acn.Value.Key} {acn.Value.Value.LockStyle} own:{name} '{acn.Value.Value.NameOfLock}'\n", EnumChatType.CommandSuccess);
		}
		}


		private void PrintLockInfo(BlockPos targetPos, IServerPlayer player)
		{
		if (ServerAPI.World.BlockAccessor.IsValidPos(targetPos)) {
		var actualThing = ServerAPI.World.BlockAccessor.GetBlock(targetPos);
		if (actualThing.HasBehavior<BlockBehaviorComplexLockable>( )) {
		AccessControlNode acn = AccessControlsMod.RetrieveACN(targetPos);

		if (acn != null && acn.LockStyle != LockKinds.None) {
		string name = ServerAPI.World.PlayerByUid(acn.OwnerPlayerUID).PlayerName;
		StringBuilder lockData = new StringBuilder( );

		lockData.AppendFormat($"Pos@{targetPos}:{acn.LockStyle} owner:{name} '{acn.NameOfLock}' Tier:{acn.Tier}");

		switch (acn.LockStyle) {

		case LockKinds.Classic:
			lockData.Append(" <Classic lock> ");
			break;

		case LockKinds.Combination:
			if (acn.CombinationCode != null) { lockData.AppendFormat(" Combo# {0}", String.Join("-", acn.CombinationCode)); }
			else {
			lockData.Append(" Combo unset!");
			}
			break;

		case LockKinds.Key:
			if (acn.KeyID.HasValue) { lockData.AppendFormat(" KeyID# {0}", acn.KeyID.HasValue); }
			else {
			lockData.Append(" KeyID# UNSET?!");
			}
			break;

		default:
			break;
		}

		player.SendMessage(GlobalConstants.CurrentChatGroup, lockData.ToString( ), EnumChatType.CommandSuccess);
		}

		}
		else { player.SendMessage(GlobalConstants.CurrentChatGroup, "Thing selected can't be lockable anyways...", EnumChatType.CommandError); }
		}

		}



	}
}
