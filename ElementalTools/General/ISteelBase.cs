using System;
namespace ElementalTools
{
	/// <summary>
	/// General 'Steel' base properties [unchangeing/immutable]
	/// </summary>
	public interface ISteelBase
	{
		
		//Static read-only properties from Item Def.
		string BaseMetalName { get; }//Blister, Shear, Cast, Damascus, Wootz, Stainless
		bool Sharpenable { get; }
		bool Hardenable { get; }
		//Fracture rate?
		//Rust resistance?


	}
}

