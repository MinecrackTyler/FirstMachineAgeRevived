using Vintagestory.API.Common;

namespace AnvilMetalRecovery;

/// <summary>
///     Applies Ingot-{metal} Combustable properties dynamically.
/// </summary>
public class SmartSmeltableItem : Item
{
	internal static AssetLocation _ingotPrefix = new(@"game", @"ingot");

	public string Metal => this?.Variant[@"metal"] ?? "copper";

	public int Ratio => Attributes[@"ratio"].AsInt(20);

	public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
	{
		RegenerateCombustablePropsByVariant(slot);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		RegenerateCombustablePropsByVariant(itemStack);
		return base.GetHeldItemName(itemStack);
	}

	protected void RegenerateCombustablePropsByVariant(ItemStack stack)
	{
		if (CombustibleProps != null || stack.Collectible.CombustibleProps != null) return;

		var ingotAssetCode = _ingotPrefix.AppendPathVariant(Metal); //Wildcard find? excluding domain?
		var ingotEntry = api.World.GetItem(ingotAssetCode);
		var metalSmeltProps = ingotEntry?.CombustibleProps?.Clone();

		if ((ingotEntry != null || !ingotEntry.IsMissing) && metalSmeltProps != null)
		{
			metalSmeltProps.SmeltedRatio = Ratio;

			//Back-Inject source Input Item stack - as Firepit checks THAT	
			stack.Collectible.CombustibleProps = metalSmeltProps.Clone();
#if DEBUG
			api.Logger.VerboseDebug("set SmeltProps, for: {0} from {1}", Code, ingotAssetCode);
#endif
		}
		else
		{
#if DEBUG
			api.Logger.VerboseDebug("Non-existant Ingot or C.Props: {0}", ingotAssetCode);
#endif
		}
	}

	protected void RegenerateCombustablePropsByVariant(ItemSlot slot)
	{
		if (CombustibleProps != null || (!slot.Empty && slot.Itemstack.Collectible.CombustibleProps != null)) return;

		var ingotAssetCode = _ingotPrefix.AppendPathVariant(Metal); //Wildcard find? excluding domain?
		var ingotEntry = api.World.GetItem(ingotAssetCode);
		var metalSmeltProps = ingotEntry?.CombustibleProps?.Clone();

		if ((ingotEntry != null || !ingotEntry.IsMissing) && metalSmeltProps != null)
		{
			metalSmeltProps.SmeltedRatio = Ratio;

			//Back-Inject source Input Item stack - as Firepit checks THAT	
			slot.Itemstack.Collectible.CombustibleProps = metalSmeltProps.Clone();
#if DEBUG
			api.Logger.VerboseDebug("set SmeltProps, for: {0} from {1}", Code, ingotAssetCode);
#endif
		}
		else
		{
#if DEBUG
			api.Logger.VerboseDebug("Non-existant Ingot or C.Props: {0}", ingotAssetCode);
#endif
		}
	}
}