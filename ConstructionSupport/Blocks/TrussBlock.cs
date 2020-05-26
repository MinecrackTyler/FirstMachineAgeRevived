using System;


namespace ConstructionSupport
{
	public class TrussBlock: GenericScaffold
	{
		public static readonly string BlockClassName = @"TrussVertical";



		public TrussBlock( )
		{
		}
		/*

		"truss_vert":
		Vertical attach to same-type mate UP/DOWN : Ladder with stuts to support/hold "deckwork_h" > ladder + transom + struts
		--Bounding blocks on 4 faces 3 inside, one outside 'block' [front+back-sides]
		--Opposite (from placed) face (NEWS): AIR ONLY! 

		[If 'attached' face/block breaks - scaffold(s) break off surface too!]
		[If B.U.D. with solid (non-truss) block Above - scaffold(s) breaks !]

		*/
	}
}

