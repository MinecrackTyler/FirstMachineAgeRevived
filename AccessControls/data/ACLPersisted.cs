using System;
using System.Collections.Generic;


using ProtoBuf;

using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	[ProtoContract]
	public class ACLPersisted
	{
		public ACLPersisted( )
		{
		KeyId_Sequence = 1;
		}

		[ProtoMember(1)]
		public int KeyId_Sequence;

		//Stats, version, other info?

	}
}

