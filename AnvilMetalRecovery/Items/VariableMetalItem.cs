using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace AnvilMetalRecovery
{
	public class VariableMetalItem : Item
	{
		private const string default_IngotCode = @"game:ingot-copper";
		private const string metalQuantityKey = @"metalQuantity";
		private const string metalIngotCodeKey = @"metalIngotCode";

		protected AssetLocation MetalCode(ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return new AssetLocation(default_IngotCode);
		return new AssetLocation(itemStack.Attributes.GetString(metalIngotCodeKey, default_IngotCode));
		}

		protected int MetalQuantity(ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return 10;
		return itemStack.Attributes.GetInt(metalQuantityKey, 10);
		}

		protected string MetalName(ItemStack itemStack)
		{
		//TODO: generic 'material' Language entries...
		if (itemStack == null || itemStack.Attributes == null) return String.Empty;
		var sliced = Lang.GetUnformatted("item-"+MetalCode(itemStack).Path).Split(' ');
		return String.Join(" ", sliced.Take(sliced.Length - 1));
		}

		protected MetalRecoverySystem AnvilMetalRecoveryMod 
		{
			get
			{
				return this.api.ModLoader.GetModSystem<MetalRecoverySystem>( );
			}
		}


		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
		//Set correct material texture from ItemStack attributes
		LoadedTexture texturePlaceholder = new LoadedTexture(capi);
		var ingotCode = MetalCode(itemstack);
		var textureDonatorItem = capi.World.GetItem(ingotCode);
		var newTexture = textureDonatorItem.FirstTexture.Base.WithPathAppendixOnce(".png");

		//#if DEBUG
		//capi.Logger.VerboseDebug("VariableMetalItem Txr: {0}", newTexture);
      	//#endif			                  
		
		capi.Render.GetOrLoadTexture(newTexture, ref texturePlaceholder);

		renderinfo.TextureId = texturePlaceholder.TextureId;

		//this.Textures["metal"] = textureDonatorItem.FirstTexture;

		//Cache TextId# on TempAttributes ?
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


		public void ApplyMetalProperties(RecoveryEntry recoveryData, ref ItemStack contStack)
		{
		contStack.Attributes.SetInt(metalQuantityKey, ( int )recoveryData.Quantity);
		contStack.Attributes.SetString(metalIngotCodeKey, recoveryData.IngotCode.ToString( ));

		if (CombustibleProps == null) {
		CombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = recoveryData.Melting_Point,
			MeltingDuration = recoveryData.Melting_Duration,
			SmeltedRatio = ( int )(100 / (recoveryData.Quantity * MetalRecoverySystem.IngotVoxelEquivalent)),
			SmeltedStack = new JsonItemStack( ) { Type = EnumItemClass.Item, Code = recoveryData.IngotCode.Clone( ), Quantity = 1 }
		};
		CombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_apply", true);
		}

		}

		protected void RegenerateCombustablePropsFromStack(ref ItemStack contStack)
		{
		AssetLocation ingotCode = null;
		var metalCode = MetalCode(contStack);

		if (metalCode != null && AnvilMetalRecoveryMod.ItemFilterList.Contains(MetalCode(contStack)))
		{
		var recoveryData = AnvilMetalRecoveryMod.ItemRecoveryTable[metalCode];	

		if (CombustibleProps == null) 
			{
				CombustibleProps = new CombustibleProperties( ) {
					SmeltingType = EnumSmeltType.Smelt,
					MeltingPoint = recoveryData.Melting_Point,
					MeltingDuration = recoveryData.Melting_Duration,
					SmeltedRatio = ( int )(100 / (recoveryData.Quantity * MetalRecoverySystem.IngotVoxelEquivalent)),
					SmeltedStack = new JsonItemStack( ) { Code = ingotCode.Clone( ), Quantity = 1 }
				};
				CombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_regen", true);
			}
		}

		}

		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		var metalName = MetalName(inSlot.Itemstack);
		var metalQuantity = MetalQuantity(inSlot.Itemstack);

		dsc.AppendLine(Lang.Get("fma:metal_worth", metalQuantity, metalName));
		}
	}

}

