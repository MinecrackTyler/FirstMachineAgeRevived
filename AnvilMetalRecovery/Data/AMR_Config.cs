using System;

using ProtoBuf;

namespace AnvilMetalRecovery
{
	[ProtoContract(ImplicitFields = ImplicitFields.None)]
	public class AMRConfig
	{
		public AMRConfig( )
		{
		ToolFragmentRecovery = true;
		VoxelEquivalentValue = MetalRecoverySystem.IngotVoxelDefault;
		}

		[ProtoMember(1)]
		public bool ToolFragmentRecovery;

		[ProtoMember(2)]
		public float VoxelEquivalentValue;


		[ProtoAfterDeserialization]
		private void ClampRange( )
		{
		VoxelEquivalentValue = Math.Max(1f, Math.Min(VoxelEquivalentValue, MetalRecoverySystem.IngotVoxelDefault));
		}


	}
}

