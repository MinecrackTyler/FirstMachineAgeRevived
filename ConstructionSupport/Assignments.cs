using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading;

using Vintagestory.API.Common;
using Vintagestory.API.Config;




namespace ConstructionSupport
{
	public partial class ConstructionSupportSystem : ModSystem
	{
		internal const string _domain = @"fma";
		internal readonly AssetLocation deckworkHorizontal_assetKey = new AssetLocation(_domain, @"deckwork_horiz");
		internal readonly AssetLocation deckworkCorner_assetKey = new AssetLocation(_domain, @"deckwork_corner");
		internal readonly AssetLocation truss_assetkKey = new AssetLocation(_domain, @"truss_vert");


		private void RegisterBlockClasses()
		{
			CoreAPI.RegisterBlockClass(DeckworkHorizontalBlock.BlockClassName, typeof(DeckworkHorizontalBlock));
			CoreAPI.RegisterBlockClass(DeckworkCornerBlock.BlockClassName, typeof(DeckworkCornerBlock));
			CoreAPI.RegisterBlockClass(TrussBlock.BlockClassName, typeof(TrussBlock));
		}








	}
}

