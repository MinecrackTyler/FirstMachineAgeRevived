using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace AnvilMetalRecovery
{
	public class VariableMetalItem : Item
	{
		private const string default_IngotCode = @"game:ingot-copper";
		private const string metalQuantityKey = @"metalQuantity";
		private const string metalIngotCodeKey = @"metalIngotCode";

		/// <summary>
		/// Store Ingot Code (here)
		/// </summary>
		/// <returns>The code.</returns>
		/// <param name="itemStack">Item stack.</param>
		/// <remarks>If base metal still exists - can still smelt...</remarks>
		protected AssetLocation MetalCode(ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return new AssetLocation(default_IngotCode);
		return new AssetLocation(itemStack.Attributes.GetString(metalIngotCodeKey, default_IngotCode));
		}

		/// <summary>
		/// Store metal VOXEL quantity value.
		/// </summary>
		/// <returns>The quantity.</returns>
		/// <param name="itemStack">Item stack.</param>
		protected int MetalQuantity(ItemStack itemStack)
		{
		if (itemStack == null || itemStack.Attributes == null) return 0;
		return itemStack.Attributes.GetInt(metalQuantityKey, 0);
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
		if (textureDonatorItem != null) {
		var newTexture = textureDonatorItem.FirstTexture.Base.WithPathAppendixOnce(".png");

		//#if DEBUG
		//capi.Logger.VerboseDebug("VariableMetalItem Txr: {0}", newTexture);
		//#endif			                  

		capi.Render.GetOrLoadTexture(newTexture, ref texturePlaceholder);

		renderinfo.TextureId = texturePlaceholder.TextureId;
		}
		//Cache TextId# on TempAttributes ?
		}


		public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
		{
		#if DEBUG
		api.Logger.VerboseDebug("CanSmelt: ");
		#endif

		if (inputStack != null ) 
		{
		RegenerateCombustablePropsFromStack(inputStack);
		return (outputStack == null) && this.CombustibleProps.MeltingPoint > 1;
		}
		return false;
		}

		public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
		RegenerateCombustablePropsFromStack(inputSlot?.Itemstack);

		return this?.CombustibleProps.MeltingPoint ?? 0f;
		}

		public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
		{
		RegenerateCombustablePropsFromStack(inputSlot?.Itemstack);

		return this?.CombustibleProps.MeltingDuration ?? 0f;
		}

		public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
		{
		RegenerateCombustablePropsFromStack(inputSlot?.Itemstack);
		#if DEBUG
		world.Logger.VerboseDebug("Invoked: 'DoSmelt' CookSlots#{1} In.stk: {0} ", (inputSlot.Empty ? "empty" : inputSlot.Itemstack.Collectible.Code.ToShortString( )), cookingSlotsProvider.Slots.Length);
		#endif


		base.DoSmelt(world, cookingSlotsProvider, inputSlot, outputSlot);
		}


		public override void GetHeldItemInfo(ItemSlot inSlot, System.Text.StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		var metalName = MetalName(inSlot.Itemstack);
		var metalQuantity = ( int )Math.Floor(MetalQuantity(inSlot.Itemstack) * MetalRecoverySystem.IngotVoxelEquivalent);
		if (CombustibleProps  != null && CombustibleProps.MeltingPoint > 0) {		
		dsc.AppendLine(Lang.Get("game:smeltpoint-smelt", CombustibleProps.MeltingPoint));
		}
		dsc.AppendLine(Lang.Get("fma:metal_worth", metalQuantity, metalName));
		}

		public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
		{
		RegenerateCombustablePropsFromStack(slot.Itemstack);			
		}

		//Merge (same) metal piles together? Upto 100 units.
		//TryMergeStacks ???
		//virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot) //?
		//virtual void OnModifiedInInventorySlot //Only for new-Inserts (?)

		public void ApplyMetalProperties(RecoveryEntry recoveryData, ref ItemStack contStack)
		{
		contStack.Attributes.SetInt(metalQuantityKey, ( int )recoveryData.Quantity);
		contStack.Attributes.SetString(metalIngotCodeKey, recoveryData.IngotCode.ToString( ));

		RegenerateCombustablePropsFromStack(contStack);
		}

		protected void RegenerateCombustablePropsFromStack(ItemStack contStack)
		{
		if (contStack == null ) return;
		if (this.CombustibleProps != null) return;
					
		var metalCode = MetalCode(contStack);
		var metalUnits = MetalQuantity(contStack);

		if (metalCode != null )
		{
		var sourceMetalItem = api.World.GetItem(metalCode);	
						
		CombustibleProps = new CombustibleProperties( ) {
			SmeltingType = EnumSmeltType.Smelt,
			MeltingPoint = sourceMetalItem.CombustibleProps.MeltingPoint,
			MeltingDuration = sourceMetalItem.CombustibleProps.MeltingDuration,
			SmeltedRatio = 100,
			SmeltedStack = new JsonItemStack( ) { Type = EnumItemClass.Item, Code = sourceMetalItem.Code.Clone( ), Quantity = (int)Math.Floor(metalUnits * MetalRecoverySystem.IngotVoxelEquivalent) }
		};
		CombustibleProps.SmeltedStack.Resolve(api.World, "VariableMetalItem_regen", true);
		//Back-Inject source Input Item stack - as Firepit checks THAT
		contStack.Collectible.CombustibleProps = CombustibleProps.Clone( );

		#if DEBUG
				api.Logger.VerboseDebug("Melt point: {0}, Duration: {1}, Ratio: {2}, Out.stk: {3} * {4}", this.CombustibleProps.MeltingPoint,this.CombustibleProps.MeltingDuration,this.CombustibleProps.SmeltedRatio, this.CombustibleProps.SmeltedStack.ResolvedItemstack.Item.Code.ToString(),this.CombustibleProps.SmeltedStack.StackSize );
		#endif
		}		

		}


	}

}

