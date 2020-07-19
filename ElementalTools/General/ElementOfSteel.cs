using System;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ElementalTools
{
	public abstract class ElementOfSteel<T> : IAmSteel where T : CollectibleObject
	{
		public virtual bool Hardenable {
			get; set;
		}

		public virtual HardnessState Hardness {
			get; set;
		}

		public virtual bool Sharpenable {
			get; set;
		}

		public virtual SharpnessState Sharpness {
			get; set;
		}
	}
}

