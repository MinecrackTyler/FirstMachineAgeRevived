using System;
using System.Collections.Generic;


using ProtoBuf;

using Vintagestory.API.MathTools;

namespace FirstMachineAge
{


	/// <summary>
	/// Holds individual Access control entries for that Chunk 
	/// </summary>
	/// <remarks>
	/// (by block Position)
	/// </remarks>
	[ProtoContract]
	public class ChunkACNodes
	{
		public ChunkACNodes( )
		{
			Entries = new SortedDictionary<BlockPos, AccessControlNode>( );//SET Comparer?
		}

		[ProtoMember(0)]
		public Vec3i OriginChunk;

		[ProtoMember(1)]
		public SortedDictionary<BlockPos, AccessControlNode> Entries;//CHECK: does it *NEED* to be sorted?

		//Last update DateTime?
	}


	[ProtoContract]
	public class AccessControlNode
	{
		public AccessControlNode( )
		{
			LockStyle = LockKinds.None;
		}

		[ProtoMember(0)]
		public string OwnerPlayerUID;

		[ProtoMember(1, IsRequired = true)]
		public LockKinds LockStyle;

		[ProtoMember(2)]
		public string SourceItemName;

		[ProtoMember(3)]
		public byte[] CombinationCode;//Nullable

		[ProtoMember(4)]
		public int? KeyID;

		[ProtoMember(5)]
		public List<AccessEntry> PermittedPlayers;//Also nullable - key locks should NEVER have entries here!

		[ProtoMember(6)]
		public bool LockDefeated;//Ya Picked it; Taffer!

		//public BlockPos Origin ; //Placement of lock in world (on block)

	}


    [ProtoContract]
	public class AccessEntry
	{
		[ProtoMember(0)]
		public string PlayerUID;

		//Access type; Player or Group ?

		[ProtoMember(1)]
		public int? GroupID;


	}


	/// <summary>
	/// A Chunk's, Lock status list.
	/// </summary>
	/// <remarks>
	/// Used client-side for fast lookup
	/// </remarks>
	[ProtoContract]
	public class LockStatusList
	{
		[ProtoMember(0)]
		public Dictionary<BlockPos,LockStatus> LockStatesByBlockPos;

		[ProtoMember(1)]
		public Vec3i ChunkOrigin;



		//Last RX time for Cache-TTL
	}

}

