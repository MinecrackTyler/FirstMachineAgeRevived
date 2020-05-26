using System;

namespace ConstructionSupport
{
	public class DeckworkCornerBlock: GenericScaffold
	{
		public static readonly string BlockClassName = @"DeckworkDiagonal";



		public DeckworkCornerBlock( )
		{
		}

		/*
		 "deckwork_corner":	
		Diagonal corner N+E / S+W : MUST contact 1 "deckwork_horiz" > (brace)+Deck
		--Must be in contact(NEWS) with 1 other "deckwork_horiz" (or 2, or more)
		--Directly below: AIR ONLY!

		[If 'attached' face/block breaks - scaffold(s) break off surface too!]
		[If B.U.D. with solid (non-truss) block Above - scaffold(s) breaks !]
		 */
	}
}

