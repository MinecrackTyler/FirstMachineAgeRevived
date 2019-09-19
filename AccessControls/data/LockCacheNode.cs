using System;

using ProtoBuf;

namespace FirstMachineAge
{
	[ProtoContract]
	public struct LockCacheNode
	{
		[ProtoMember(0)]
		public LockStatus LockState;

		[ProtoMember(1)]
		public uint Tier;
	}
}

