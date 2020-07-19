using System;
namespace ElementalTools
{
	public interface IAmSteel
	{
		bool Sharpenable { get; set; }
		bool Hardenable { get; set; }
		SharpnessState Sharpness { get; set; }//To sharpen - what unit if numerical?
		HardnessState Hardness { get; set; }//To harden - Perhaps translate these to brinell units...

	}

	public enum SharpnessState : byte
	{
		Rough = 0,//Unsharpened state *Default*
		Dull,
		Honed,
		Keen,
		Sharp,
		Razor,
	}

	public enum HardnessState : byte
	{
		Soft = 0,
		Medium,
		Hard,
		Brittle,
	}

}

