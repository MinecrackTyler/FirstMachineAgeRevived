using System;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElementalTools
{
	/// <summary>
	/// Item with some common Tool helpers, Base common Steely-features
	/// </summary>
	public abstract class SteelBaseItem : Item, ISteelByStack, ISteelBase
	{
		private const string altToolKeyword = @"AltTool";



		protected SteelBaseItem( ) : base( )
		{
		}

		protected SteelBaseItem(int itemId) : base(itemId)
		{
		}

		public void OverwriteFields(Item setee)
		{
		//setee.MiningSpeed = this.MiningSpeed;//Ect...
		Traverse.IterateFields(this, setee, Traverse.CopyFields);
		}

		public EnumTool? AltTool {
			get
			{
			if (this.Attributes != null && this.Attributes.KeyExists(altToolKeyword)) {
			EnumTool altEnumVal = ( EnumTool )(this.Attributes[altToolKeyword].AsInt(0));
			return altEnumVal;
			}
			return null;
			}
			//HACK: to workaround null tool values...
		}

		public bool Edged {
			get
			{
			if (this.Tool.HasValue) return this.Tool.EdgedImpliment( );
			if (this.AltTool.HasValue) return this.AltTool.EdgedImpliment( );
			return false;
			}
		}

		public bool Weapon {
			get
			{
			if (this.Tool.HasValue) return this.Tool.Weapons( );
			if (this.AltTool.HasValue) return this.AltTool.Weapons( );
			return false;
			}
		}




		#region Static Properties
		public virtual bool Hardenable {
			get
			{
			return this.Attributes[SteelAspects.hardenableKeyword].AsBool(false);
			}
		}


		public virtual string BaseMetalName {
			get
			{
			return this.Attributes[SteelAspects.metalNameKeyword].AsString("?");
			}
		}

		public virtual bool Sharpenable {
			get
			{
			return this.Attributes[SteelAspects.sharpenableKeyword].AsBool(false);
			}
		}
		#endregion


		#region ISteelByStack
		public virtual SharpnessState Sharpness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(SteelAspects.sharpnessKeyword)) {
		byte[ ] bytes = new byte[1];
		bytes = someStack.Attributes.GetBytes(SteelAspects.sharpnessKeyword, bytes);
		return bytes == null ? SharpnessState.Rough : ( SharpnessState )bytes[0];
		}

		return SharpnessState.Rough;
		}

		public virtual void Sharpness(IItemStack someStack, SharpnessState set)
		{
		byte[ ] bytes = new byte[1];
		bytes[0] = ( byte )set;
		someStack.Attributes.SetBytes(SteelAspects.sharpnessKeyword, bytes);
		}

		public virtual SharpnessState Sharpen(IItemStack someStack)
		{
		if (this.Sharpenable == false) {
		api.World.Logger.VerboseDebug("Can't sharpen! {0}", this.Code);
		return this.Sharpness(someStack); ;
		}

		SharpnessState sharp = Sharpness(someStack);

		if (sharp < SharpnessState.Razor) { Sharpness(someStack, ++sharp); }
		//TODO: Play sound effect
		#if DEBUG
		api.World.Logger.VerboseDebug("Sharpness of '{1}' increased to: {0}", sharp, this.Code);
		#endif

		//TODO: If durability exists - decriment based on Hardnes Vs. Wear...
		if (this.Durability > 1) {

		var currentDur = GetRemainingDurability(someStack as ItemStack);
		SteelAspects.SetHitpoints(someStack, --currentDur);
		}

		return sharp;
		}


		public virtual SharpnessState Dull(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(SteelAspects.sharpnessKeyword)) {
		byte[ ] bytes = new byte[1];
		bytes = someStack.Attributes.GetBytes(SteelAspects.sharpnessKeyword, bytes);
		var state = ( SharpnessState )bytes[0];

		if (state > SharpnessState.Rough) state--;

		bytes[0] = ( byte )state;
		someStack.Attributes.SetBytes(SteelAspects.sharpnessKeyword, bytes);

		return state;
		}
		return SharpnessState.Rough;
		}



		public virtual HardnessState Hardness(IItemStack someStack)
		{
		if (someStack.Attributes != null && someStack.Attributes.HasAttribute(SteelAspects.hardnessKeyword)) {
		byte[ ] bytes = new byte[1];
		bytes = someStack.Attributes.GetBytes(SteelAspects.hardnessKeyword, bytes);
		return bytes == null ? HardnessState.Soft : ( HardnessState )bytes[0];
		}

		return HardnessState.Soft;
		}

		public virtual void Hardness(IItemStack someStack, HardnessState set)
		{
		byte[ ] bytes = new byte[1];
		bytes[0] = ( byte )set;
		someStack.Attributes.SetBytes(SteelAspects.hardnessKeyword, bytes);
		}

		public virtual void CopyStackAttributes(ItemStack donor, ItemStack recipient)
		{

		if (donor.Class == recipient.Class) {
		var hI = (donor.Item as ISteelByStack).Hardness(donor);
		var sI = (donor.Item as ISteelByStack).Sharpness(donor);

		(recipient.Item as ISteelByStack).Hardness(recipient, hI);
		(recipient.Item as ISteelByStack).Sharpness(recipient, sI);

		if (donor.Item.Durability > 0) {
		var wear = GetRemainingDurability(donor);

			if (donor.Item.IsFerricMetal( ) && recipient.Item.IsSteelMetal( )) {
				var percentWear = (wear / donor.Item.Durability);
				SteelAspects.SetHitpoints(recipient, recipient.Item.Durability * percentWear);
				}
				else SteelAspects.SetHitpoints(recipient, wear);
				}
		}
		}

		#endregion
	}

	
}

