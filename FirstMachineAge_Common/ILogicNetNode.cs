using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{

	//The Basis for ABSTRACT-CIRCUITS.
	/*
	 Domain (LogicDomain) <- Circuits (LogicNetwork) <- Subcircuit <LogicNetworkSubcircuit) <- BlockFace; <EndpointDescriptor>
	 */
	//basic single ended, digital logic for this ... complex multi-bus / analog is a different interface
	public interface ILogicNetNode<T> where T: Block //Blocks only, since these are meant to be placable in-world
	{
		ICollection<LogicDomain> DOMAINs { get; } //Rod-logic, Tension-Cable & Pulley, Electrical, Pnumatic, Hydraulic, Redstone?, more than 1 == some kinda adaptor

		LogicNetwork Circuit { get; } //Per domain (AKA Network ID), encloses several 'sub' circuits (possibly monotonic), all connected (at least 1 io). Zero - considered unassigned or invalid

		IDictionary<BlockFacing, LogicNetworkSubcircuit> SubCircuits { get; } //A CONNECTED 'Sub' Circuit; MUST be unique for each sub-circuit; an impulse going somewhere, doing SOMETHING...

		IDictionary<BlockFacing, bool> VALUEs { get; set;} //Boolean STATE - per face, may/should trigger "events" on changes forced or 'natural'

		IDictionary<BlockFacing, EndpointDescriptor> AvailableConnections { get; } //Pins, sockets, connections; Inputs - Outputs, Both? Which faces?? Default Pullups/down???


		bool TryGetNetwork(IWorldAccessor world, BlockPos anywhere, out LogicNetwork linkedNet);

		bool HasInterface(BlockPos anywhere, BlockFacing face);

		EndpointDescriptor GetInterface(BlockPos anywhere, BlockFacing face);

		bool CanLink(Block otherNetBlock, BlockFacing forFace);

		bool CanLink(Block otherNetBlock, EndpointDescriptor byInterface);//for a GUI to see what could connect if anything


		LogicNetworkSubcircuit MakeConnection(BlockPos atLocation, BlockFacing forFace);//point blank connections allowed ~ depending

		void BreakConnection( LogicNetworkSubcircuit cutLink);

		void BreakConnection(BlockFacing cutLinkBySide);

		void BreakAllConnections();

		event LogicStateChange OnAnyChange;
	}

	public abstract class LogicDomain
	{
		public readonly static string NAME;//TTL, CMOS, Rod-Logic, Cable-Link, Pnumatic, ect...
		//Global ID tag?
		//Other information about domain features

	}

	public abstract class LogicNetwork
	{
		public LogicDomain ParentDomain { get; }
		public ulong NetworkID { get; }//Monotonic - reuse ID# only at own peril.
		public IList<LogicNetworkSubcircuit> ConnectedSubCircuits { get; }

	}

	public abstract class LogicNetworkSubcircuit//A link between Pin, contact, face or perhaps functions?
	{
		public uint SubCircuitID { get; }
		public IList<LogicNetworkSubcircuit> AtttachedNodes { get; }
		public LogicNetwork ParentCircuit { get; }
		public EndpointDescriptor LocalPoint{ get; }
		public bool PreviousState { get; }
		public event LogicStateChange OnChange;
	}

	//As it might change 'dyamically' or be editable...
	public abstract class EndpointDescriptor
	{
		public sbyte Number { get; }
		public readonly string Description;//Name of Pin or Contact or function
		public BlockFacing ForFace { get; }
		public LogicIO EndKind { get; }
		public DefaultState Normally { get; }
		public LogicDomain TargetDomain { get; }//For Adaptors
	}

	public enum LogicIO
	{
		Input,
		Output,
		Both,//Just don't connect to same Sub-Circuit!
	}

	//Pullups/downs, floatin' or random?
	public enum DefaultState
	{
		High,
		Low,
		Undefined//! - Mabey Random, or can't be determined before execution
	}

	public delegate void LogicStateChange(LogicNetworkSubcircuit origin, bool from, bool to);//Same state ~ pulse?





}

