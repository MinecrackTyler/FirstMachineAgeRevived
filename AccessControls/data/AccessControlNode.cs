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
			Entries = new Dictionary<BlockPos, AccessControlNode>( );
		}

		public ChunkACNodes(Vec3i originChunk )
		{
		Entries = new Dictionary<BlockPos, AccessControlNode>( );
		Altered = true;
		OriginChunk = originChunk;
		}

		[ProtoMember(1)]
		public Vec3i OriginChunk;

		[ProtoMember(2)]
		public Dictionary<BlockPos, AccessControlNode> Entries;

		//Last update DateTime?

		///<summary>
		/// Chunk had some kinda activity - Save it Soonest!
		///</summary>
		[ProtoIgnore]
		public bool Altered { get; set; }
	}


	[ProtoContract]
	public class AccessControlNode
	{
		public AccessControlNode( )
		{
			LockStyle = LockKinds.None;
		}

		public AccessControlNode(string OwnerUID, LockKinds originalType)
		{
		this.OwnerPlayerUID = OwnerUID;
		this.LockStyle = originalType;
		}

		[ProtoMember(1)]
		public string OwnerPlayerUID;

		[ProtoMember(2, IsRequired = true)]
		public LockKinds LockStyle;

		[ProtoMember(3)]
		public string NameOfLock;//Limit & trim length 

		[ProtoMember(4)]
		public byte[] CombinationCode;//Nullable

		[ProtoMember(5)]
		public int? KeyID;

		[ProtoMember(6)]
		public List<AccessEntry> PermittedPlayers;//Also nullable - key locks should NEVER have entries here (ignored)

		[ProtoMember(7)]
		public bool LockDefeated;//Ya Picked it; Taffer!

		[ProtoMember(8)]
		public uint Tier;

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


		[ProtoMember(1)]
		public Dictionary<BlockPos,LockCacheNode> LockStatesByBlockPos;

		/*
		[ProtoMember(1)]
		public Vec3i ChunkOrigin;
		*/


		//Last RX time for Cache-TTL ?
	}

}

