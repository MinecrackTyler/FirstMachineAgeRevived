using System;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public class FalseWall : BoltableDoor
	{
		public FalseWall( )
		{
			
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{
		if (this.IsOpen) {
			if (!String.IsNullOrEmpty(this.Material)) {
				return Lang.Get("defensive:block-false_wall-open");
			}
		}
		else {
			if (!String.IsNullOrEmpty(this.Material)) {
				return Lang.Get(GlobalConstants.DefaultDomain + $":block-stonebricks-{this.Material}");
			}
		}

		return @"Error?";
		}


		private string Material {
			/*
			{ code: "cover",  states: [ "sand","gravel" ] },
			{ code: "material", loadFromProperties: "block/rock", combine: "SelectiveMultiply", onVariant: "cover" },
			{ code: "alt_cover", states: [ "soil" ], combine: "Add" },
			*/
			get
			{
			return this.Variant[@"material"];
			}
		}
	}
}

