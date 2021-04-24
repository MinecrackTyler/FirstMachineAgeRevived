using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace AnvilMetalRecovery
{
	public class VariableMetalItem : Item
	{
		private const string metalQuantityKey = @"metalQuantity";
		private const string metalIngotCodeKey = @"metalIngotCode";

		protected ClientCoreAPI ClientAPI { get; private set; }

		protected AssetLocation MetalCode(ItemStack itemStack)
		{
		return new AssetLocation(itemStack.Attributes.GetString(metalIngotCodeKey, @"game:ingot-copper"));
		}

		protected int MetalQuantity(ItemStack itemStack)
		{
		return itemStack.Attributes.GetInt(metalQuantityKey, 10);
		}


		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
		//Set correct material texture from ItemStack attributes
		var ingotCode = MetalCode(itemstack);

		var textureDonatorItem = ClientAPI.World.GetItem(ingotCode);

		this.Textures["metal"] = textureDonatorItem.FirstTexture;

		//renderinfo.TextureId = textureDonatorItem.FirstTexture.??

		}

		/* (from Itemstack -attr)
		 * On creation/container entry: - assign (everywhere!):
		 * CombustibleProps
		 * .SmeltedStack
		 * .ResolvedItemstack.
		 * 
		*/




		//virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot) //Here?

		//virtual void OnModifiedInInventorySlot //Only for new-Inserts (?)

		//virtual bool CanSmelt //YES!
		//virtual float GetMeltingDuration //Order-ops, or failsafe?
		//virtual float GetMeltingPoint //Order-ops, or failsafe?
		//virtual void DoSmelt //Mabey?


		public void ApplyMetalProperties(RecoveryEntry become, ItemStack contStack)
		{
		contStack.Attributes.SetInt(metalQuantityKey, ( int )become.Quantity);
		contStack.Attributes.SetString(metalIngotCodeKey, become.IngotCode.ToString( ));

		if (CombustibleProps == null) {
		CombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = 999,//TODO: This is where a Rules based metal propperties Master-file would help!
			MeltingDuration = 123,
			SmeltedRatio = ( int )Math.Round(become.Quantity * MetalRecoverySystem.IngotVoxelEquivalent, 0),
			SmeltedStack = new JsonItemStack( ) { Code = become.IngotCode.Clone( ), Quantity = 1 }
		};
		CombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_apply", true);


		}

		}

		public void RegenerateCombustablePropsFromStack(ItemStack contStack)
		{
		//TODO: Lookup Metal data from Ingot-{metal} code....Clumsy!
		int quantity = 1;
		AssetLocation ingotCode = null;

		if (CombustibleProps == null) {
		CombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = 999,//TODO: This is where a Rules based metal propperties Master-file would help!
			MeltingDuration = 123,
			SmeltedRatio = ( int )Math.Round(quantity * MetalRecoverySystem.IngotVoxelEquivalent, 0),
			SmeltedStack = new JsonItemStack( ) { Code = ingotCode.Clone( ), Quantity = 1 }
		};
		CombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_regen", true);
		}
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		var metalName = Lang.Get(MetalCode(inSlot.Itemstack).ToString( ));
		var metalQuantity = MetalQuantity(inSlot.Itemstack);

		dsc.AppendLine(Lang.Get("metal_worth", metalQuantity, metalName));
		}

	}

}

