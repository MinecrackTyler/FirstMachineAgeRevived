using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;


namespace AnvilMetalRecovery
{
	/// <summary>
	/// Applies Ingot-{metal} Combustable properties dynamically.
	/// </summary>
	public class SmartSmeltableItem : Item
	{
		internal static AssetLocation _ingotPrefix = new AssetLocation(@"game", @"ingot");
		public string Metal 
		{ get { return this?.Variant[@"metal"] ?? "copper"; } }

		public int Ratio 
		{ get { return this.Attributes[@"ratio"].AsInt(20); } }

		public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
		{
		RegenerateCombustablePropsByVariant(slot);
		}

		protected void RegenerateCombustablePropsByVariant(ItemSlot slot)
		{
		if (this.CombustibleProps != null || ( slot.Empty == false && slot.Itemstack.Collectible.CombustibleProps != null)) return;

		var ingotAssetCode = _ingotPrefix.AppendPathVariant(Metal);//Wildcard find? excluding domain?
		var ingotEntry = api.World.GetItem(ingotAssetCode);
		var metalSmeltProps = ingotEntry?.CombustibleProps?.Clone( );

		if ((ingotEntry != null || !ingotEntry.IsMissing) && metalSmeltProps != null) 
			{		
			metalSmeltProps.SmeltedRatio = Ratio;

			//Back-Inject source Input Item stack - as Firepit checks THAT	
			slot.Itemstack.Collectible.CombustibleProps = metalSmeltProps.Clone( );
			#if DEBUG
			api.Logger.VerboseDebug("set SmeltProps, for: {0} from {1}", this.Code.ToString( ), ingotAssetCode.ToString());
			#endif
			}
			else 
			{
			#if DEBUG
			api.Logger.VerboseDebug("Non-existant Ingot or C.Props: {0}", ingotAssetCode.ToString( ));
			#endif
			}		
		}
	}
}

