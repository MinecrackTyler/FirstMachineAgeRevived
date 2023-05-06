using System;
using System.Collections.Generic;

using ProtoBuf;

using Vintagestory.API.Common;

namespace AnvilMetalRecovery
{
	[ProtoContract(ImplicitFields = ImplicitFields.None, SkipConstructor = true)]
	public class AMRConfig
	{
		public AMRConfig( )
		{
		ToolFragmentRecovery = true;
		VoxelEquivalentValue = MetalRecoverySystem.IngotVoxelDefault;
		ToolRecoveryRate = 0.85f;
		}

		public AMRConfig(bool setDefaultBL) 
		{
		ToolFragmentRecovery = true;
		VoxelEquivalentValue = MetalRecoverySystem.IngotVoxelDefault;
		ToolRecoveryRate = 0.85f;
		if (setDefaultBL) {
		BlackList = new List<AssetLocation> {					
					new AssetLocation(@"game:metalplate"),
					new AssetLocation(@"game:metallamellae"),
					new AssetLocation(@"game:metalchain"),
					new AssetLocation(@"game:metalscale"),
					};
			}
		}

		[ProtoMember(1)]
		public bool ToolFragmentRecovery;

		[ProtoMember(2)]
		public float VoxelEquivalentValue;

		/// <summary>
		/// Never Issue Metal bits for these; from Anvil OR breakage.
		/// </summary>
		[ProtoMember(3)]
		public List<AssetLocation> BlackList;

		[ProtoMember(4)]
		public float ToolRecoveryRate;


		[ProtoAfterDeserialization]
		private void ClampRange( )
		{
		VoxelEquivalentValue = Math.Max(1f, Math.Min(VoxelEquivalentValue, MetalRecoverySystem.IngotVoxelDefault));
		ToolRecoveryRate = Math.Min(1f, ToolRecoveryRate);
		ToolRecoveryRate = Math.Max(0.1f, ToolRecoveryRate);
		}


	}
}

