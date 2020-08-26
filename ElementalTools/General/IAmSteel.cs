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
		//Fracture rate?

		/// <summary>
		/// Read Sharpness; Dynamic properties from ItemStack - attribs
		/// </summary>
		/// <param name="someStack">Source stack.</param>
		SharpnessState Sharpness(IItemStack someStack);

		/// <summary>
		/// Set Sharpness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		/// <param name="set">Value.</param>
		void Sharpness(IItemStack someStack, SharpnessState set);//Apply sharpen - what unit if numerical?

		/// <summary>
		/// Incriments the sharpness.
		/// </summary>
		/// <returns>The sharpness.</returns>
		/// <param name="someStack">Some stack.</param>
		SharpnessState Sharpen(IItemStack someStack);

		/// <summary>
		/// Reduce the sharpness.
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		SharpnessState Dull(IItemStack someStack);

		/// <summary>
		/// Read Hardness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		HardnessState Hardness(IItemStack someStack);

		/// <summary>
		/// Set Hardness
		/// </summary>
		/// <param name="someStack">Some stack.</param>
		/// <param name="set">Value.</param>
		void Hardness(IItemStack someStack, HardnessState set);//Apply harden - Perhaps translate these to brinell units...


		/// <summary>
		/// Perpetuate Steely attributes from donor to recipient
		/// </summary>
		/// <param name="donor">From here</param>
		/// <param name="recipient">To here.</param>
		void CopyAttributes(ItemStack donor, ItemStack recipient);
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
	/// The metals crystal state; changed only by normalizing/quenching/annealing
	/// </summary>
	/// <remarks>
	/// Reduces wear - with a penalty of random catastrophic failure...Metal's "Temper'
	/// </remarks>
	public enum HardnessState : byte
	{
		Soft	=	0,
		Mild 	= 	1,
		Medium	=	2,
		Hard	=	3,
		Brittle	=	4,
	}

}

