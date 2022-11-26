using System;

using Vintagestory.API.Common;

namespace ElementalTools
{
	public class SteelThingViaStack: ISteelThingInstance, ISteelBase
	{
		

		protected IItemStack sourceStack { get; set;}

		private SteelThingViaStack( ) { }
		private ILogger Logger { get; set; }

		public SteelThingViaStack(IItemStack inputStack)
		{
		sourceStack = inputStack;
		}

		#region ISteelBase

		public string BaseMetalName {
			get
			{
			return sourceStack.Item.Attributes[SteelAspects.metalNameKeyword].AsString("?");
			}
		}

		public bool Sharpenable {
			get
			{
			return sourceStack.Item.Attributes[SteelAspects.sharpenableKeyword].AsBool(false);
			}
		}

		public bool Hardenable {
			get
			{
			return sourceStack.Item.Attributes[SteelAspects.hardenableKeyword].AsBool(false);
			}
		}
		#endregion

		#region ISteelThingInstance

		public SharpnessState Sharpness {
			get
			{
			if (sourceStack.Attributes != null && sourceStack.Attributes.HasAttribute(SteelAspects.sharpnessKeyword)) {
			byte[ ] bytes = new byte[1];
			bytes = sourceStack.Attributes.GetBytes(SteelAspects.sharpnessKeyword, bytes);
			return bytes == null ? SharpnessState.Rough : ( SharpnessState )bytes[0];
			}

			return SharpnessState.Rough;
			}

			set
			{
			byte[ ] bytes = new byte[1];
			bytes[0] = ( byte )value;
			sourceStack.Attributes.SetBytes(SteelAspects.sharpnessKeyword, bytes);
			}
		}

		public HardnessState Hardness {
			get
			{
			if (sourceStack.Attributes != null && sourceStack.Attributes.HasAttribute(SteelAspects.hardnessKeyword)) {
			byte[ ] bytes = new byte[1];
			bytes = sourceStack.Attributes.GetBytes(SteelAspects.hardnessKeyword, bytes);
			return bytes == null ? HardnessState.Soft : ( HardnessState )bytes[0];
			}
			return HardnessState.Soft;
			}

			set
			{
			byte[ ] bytes = new byte[1];
			bytes[0] = ( byte )value;
			sourceStack.Attributes.SetBytes(SteelAspects.hardnessKeyword, bytes);
			}
		}



		public SharpnessState Sharpen( )
		{
		if (this.Sharpenable == false) {
		Logger.Notification("Can't sharpen! {0}", sourceStack.Item.Code);
		return Sharpness;
		}
		SharpnessState sharp = this.Sharpness;

		if (sharp < SharpnessState.Razor) { this.Sharpness = ++sharp; }
		//TODO: Play sound effect
		#if DEBUG
		Logger.VerboseDebug("Sharpness of '{1}' increased to: {0}", sharp, sourceStack.Item.Code);
		#endif

		//TODO: If durability exists - decriment based on Hardness Vs. Wear...
		if (this.Hitpoints > 1) {					
		Hitpoints = --Hitpoints;
		}

		return sharp;
		}

		public SharpnessState Dull( )
		{
		if (this.Sharpenable == false) {
		Logger.Notification("Can't dull! {0}", sourceStack.Item.Code);
		return Sharpness;
		}
		SharpnessState sharp = this.Sharpness;

		if (sharp > SharpnessState.Dull) { this.Sharpness = --sharp; }
		#if DEBUG
		Logger.VerboseDebug("Sharpness of '{1}' decreased to: {0}", sharp, sourceStack.Item.Code);
		#endif

		//TODO: If durability exists - decriment based on Hardness Vs. Wear...
		if (this.Hitpoints > 1) {
		Hitpoints = --Hitpoints;
		}

		return sharp;
		}

		public HardnessState Harden( )
		{
		if (this.Hardenable == false) {
		Logger.Notification("Can't Harden! {0}", sourceStack.Item.Code);
		return Hardness;
		}
		var hard = this.Hardness;

		if (hard < HardnessState.Brittle) { this.Hardness = ++hard; }
		
		#if DEBUG
		Logger.VerboseDebug("Hardness of '{1}' increased to: {0}", hard, sourceStack.Item.Code);
		#endif

		//TODO: If durability exists - decriment based on Hardness Vs. Wear...
		if (this.Hitpoints > 1) {
		Hitpoints = --Hitpoints;
		}

		return hard;
		}

		/// <summary>
		/// Clones the stack attributes.
		/// </summary>
		/// <returns>The stack attributes.</returns>
		/// <param name="target">Target.</param>
		//TODO: Clone _ALL_ the attributes!
		public void CloneStackAttributes(ItemStack target)
		{
		if (target.Collectible.Code == sourceStack.Collectible.Code) {
		var targetSteel = target.AsSteelThing();
		targetSteel.Sharpness = this.Sharpness;
		targetSteel.Hardness = this.Hardness;


		if (sourceStack.Item.Durability > 1) {		
		SteelAspects.SetHitpoints(target, this.Hitpoints);
		}
		
		}
		}

		#endregion


		protected int Hitpoints {
			get
			{
				return sourceStack.Attributes.GetInt(SteelAspects.durabilityKeyword, sourceStack.Item.Durability);
			}

			set { 
				sourceStack.Attributes.SetInt(SteelAspects.durabilityKeyword, value);
			}
		}



	}
}

