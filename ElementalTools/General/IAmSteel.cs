using System;

using Vintagestory.API.Common;

namespace ElementalTools
{
	public interface IAmSteel
	{
		//Static read-only properties from Item Def.
		string Name { get; }//Blister, Shear, Cast, Damascus, Wootz...mostly descriptive
		bool Sharpenable { get; }
		bool Hardenable { get; }

		//Rust?

		/// <summary>
		/// Read Sharpness; Dynamic properties from ItemStack - attribs
		/// </summary>
		/// <param name="someStack">Source stack.</param>
		SharpnessState Sharpness(ItemStack someStack);

		/// <summary>
		/// Set Sharpness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		/// <param name="set">Value.</param>
		void Sharpness(ItemStack someStack, SharpnessState set);//Apply sharpen - what unit if numerical?

		/// <summary>
		/// Read Harness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		HardnessState Hardness(ItemStack someStack);

		/// <summary>
		/// Set Hardness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		/// <param name="set">Value.</param>
		void Hardness(ItemStack someStack, HardnessState set);//Apply harden - Perhaps translate these to brinell units...

	}

	/// <summary>
	/// "Sharpness" factor of working edge...only where applicable...relative vs. default tool/arm
	/// </summary>
	/// <remarks>
	/// Affects: (GetMiningSpeed) , (GetAttackPower), (DamageItem), (OnConsumedByCrafting)
	/// </remarks>
	public enum SharpnessState : byte
	{
		Rough	=	0,//Unsharpened state *Default*
		Dull	=	1,
		Honed	=	2,
		Keen	=	3,
		Sharp	=	4,
		Razor	=	5,
	}

	/// <summary>
	/// The metals crystal state; changed only by tempering/quenching/annealing
	/// </summary>
	/// <remarks>
	/// Reduces wear - with a penalty of random catastrophic failure...
	/// </remarks>
	public enum HardnessState : byte
	{
		Soft	=	0,
		Medium	=	1,
		Hard	=	2,
		Brittle	=	3,
	}

}

