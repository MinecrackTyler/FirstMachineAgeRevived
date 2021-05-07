using System;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Machinations
{
	public class MechNetAnalyser : LocalClientCommand
	{
		public MechNetAnalyser(ICoreClientAPI _clientAPI) : base(_clientAPI)
		{
		this.Command = @"mprobe";
		this.Description = "Probes local information about Mechanical-Network block(s).";
		this.handler += ProbeMechnet;
		this.Syntax = " {facing} | blockPos";
		}

		internal void ProbeMechnet(int groupId, CmdArgs args)
		{
		var pos = ClientAPI.World.Player.CurrentBlockSelection.Position.Copy( );
		if (args.Length > 1) {
		pos = args.PopVec3i( ).AsBlockPos;
		}

		var someBlock = ClientAPI.World.BlockAccessor.GetBlock(pos);

			if (someBlock != null && !someBlock.IsMissing && someBlock.MatterState == EnumMatterState.Solid) 
			{
				if (someBlock is IMechanicalPowerBlock) 
				{
				var mechNetInterface = someBlock as IMechanicalPowerBlock;
				var mpNetwork = mechNetInterface.GetNetwork(ClientAPI.World, pos);
				
				if (mpNetwork == null) 
					{
					ClientAPI.ShowChatMessage("No valid network present.");
					return;
					}

				var report = ReportOnMechnetwork(mpNetwork);
				ClientAPI.ShowChatMessage(report.ToString( ));
				}
				else 
				{
				ClientAPI.ShowChatMessage(string.Format("Block: '{0}' not Mechanical...", someBlock.GetPlacedBlockName(ClientAPI.World, pos)));
				}
			}

		}
		internal StringBuilder ReportOnMechnetwork(MechanicalNetwork mpNetwork)
		{
		var report = new StringBuilder(1000);

		report.AppendLine($"Network #{mpNetwork.networkId} OK:{mpNetwork.Valid} Loaded:{mpNetwork.fullyLoaded} ");
		report.AppendLine($"DIR:{mpNetwork.TurnDir} REV:{mpNetwork.DirectionHasReversed} SPD:{mpNetwork.Speed:F1} LSPD:{mpNetwork.clientSpeed:F1} %TRQ:{mpNetwork.TotalAvailableTorque:F1} NTRQ:{mpNetwork.NetworkTorque:F1} NRST:{mpNetwork.NetworkResistance:F1}  ");
		report.AppendLine($"TURNS:{mpNetwork.AngleRad:F2} Nodes: {mpNetwork.nodes.Count}");


		return report;
		}

	}

}