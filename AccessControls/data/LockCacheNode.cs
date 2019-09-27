using System;

using ProtoBuf;

namespace FirstMachineAge
{
	[ProtoContract]
	public struct LockCacheNode
	{
		[ProtoMember(1)]
		public LockStatus LockState;

		[ProtoMember(2)]
		public uint Tier;
	}
}

