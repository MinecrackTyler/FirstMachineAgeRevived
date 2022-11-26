using System;

using Vintagestory.API.Common;

namespace ElementalTools
{
	public interface ISteelThingInstance
	{				
		//Rusty?

		/// <summary>
		/// Read Sharpness; Dynamic properties from ItemStack - attribs
		/// </summary>
		SharpnessState Sharpness { get; set; }

		/// <summary>
		/// Incriments the sharpness.
		/// </summary>
		/// <returns>The sharpness.</returns>
		SharpnessState Sharpen();

		/// <summary>
		/// Reduce the sharpness.
		/// </summary>
		SharpnessState Dull();

		/// <summary>
		/// Set Hardness
		/// </summary>
		HardnessState Hardness{ get; set; }

		/// <summary>
		/// Harden (incriment) this collectable.
		/// </summary>
		HardnessState Harden();

		/// <summary>
		/// Clones the stack attributes ONTO Target. (used when sharpening)
		/// </summary>
		/// <returns>The stack attributes.</returns>
		/// <param name="target">Subject of stack-attribute (overwrite).</param>
		void CloneStackAttributes(ItemStack target);
	}
}