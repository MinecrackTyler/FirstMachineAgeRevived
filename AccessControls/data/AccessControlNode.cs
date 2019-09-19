using System;
using System.Collections.Generic;


using ProtoBuf;

using Vintagestory.API.MathTools;

namespace FirstMachineAge
{


	/// <summary>
	/// Holds individual Access control entries for A Chunk 
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
	/// A Chunk's, Lock status list. (Client cache)
	/// </summary>
	/// <remarks>
	/// Used client-side for fast lookup, server sends these as updates on changes
	/// </remarks>
	[ProtoContract]
	public class LockStatusList
	{
		private LockStatusList( ) { throw new NotSupportedException(); }

		public LockStatusList(BlockPos here)
		{
			//Clear an entry - remove lock from cache

			LockStatesByBlockPos = new Dictionary<BlockPos, LockCacheNode>( );

			var nullifier = new LockCacheNode { LockState = LockStatus.None, Tier = 0 };

			LockStatesByBlockPos.Add(here.Copy( ), nullifier);
		}

		public LockStatusList( BlockPos here, LockCacheNode ownACN)
		{						
			LockStatesByBlockPos = new Dictionary<BlockPos, LockCacheNode>( );

			LockStatesByBlockPos.Add(here.Copy( ), ownACN);
		}

		public LockStatusList(IDictionary<BlockPos, LockCacheNode> nodes)
		{			
			LockStatesByBlockPos = new Dictionary<BlockPos, LockCacheNode>( nodes );
		}


		[ProtoMember(0)]
		public Dictionary<BlockPos,LockCacheNode> LockStatesByBlockPos;

		/*
		[ProtoMember(1)]
		public Vec3i ChunkOrigin;
		*/


		//Last RX time for Cache-TTL ?
	}

}

