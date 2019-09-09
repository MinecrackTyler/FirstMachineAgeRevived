using System;
using System.Collections.Generic;


using ProtoBuf;

using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	[ProtoContract]
	public class ACLPersisted
	{
		[ProtoMember(0)]
		public int KeyId_Sequence;

		//Stats, other info?

	}
}

