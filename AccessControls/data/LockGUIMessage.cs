using System;

using ProtoBuf;

using Vintagestory.API.MathTools;

namespace FirstMachineAge
{

	[ProtoContract]
	public class LockGUIMessage
	{
		[ProtoMember(0)]
		public BlockPos position;

		[ProtoMember(1)]
		public byte[] comboGuess;

		public LockGUIMessage(BlockPos position, byte[] comboGuess)
		{
			this.position = position;
			this.comboGuess = comboGuess;
		}
	}
}

