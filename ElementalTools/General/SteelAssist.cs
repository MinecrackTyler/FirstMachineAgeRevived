using System;

using Vintagestory.API.Common;

namespace ElementalTools
{
	/// <summary>
	/// Item wrapped with some Tool helpers & stuff
	/// </summary>
	public abstract class SteelAssistItem : Item
	{
		private const string altToolKeyword = @"AltTool";

		protected SteelAssistItem() : base()
		{
		}

		protected SteelAssistItem(int itemId) : base (itemId)
		{	
		}

		public EnumTool? AltTool {
			get
			{
			if (this.Attributes != null && this.Attributes.KeyExists(altToolKeyword)) 
				{
				EnumTool altEnumVal = (EnumTool)(this.Attributes[altToolKeyword].AsInt(0));
				return altEnumVal;
				}
			return null;
			}
			//HACK: to workaround null tool values...
		}

		public bool Edged {
			get
			{
			if (this.Tool.HasValue) return this.Tool.EdgedImpliment( );
			if (this.AltTool.HasValue) return this.AltTool.EdgedImpliment( );
			return false;
			}
		}

		public bool Weapon {
			get
			{
			if (this.Tool.HasValue) return this.Tool.Weapons( );
			if (this.AltTool.HasValue) return this.AltTool.Weapons( );
			return false;
			}
		}

		public bool RecomendedUsage(EnumBlockMaterial blockMaterial)
		{
		if( this.MiningSpeed != null && this.MiningSpeed.ContainsKey(blockMaterial)) return true;
		return false;			
		}			
	}
}

