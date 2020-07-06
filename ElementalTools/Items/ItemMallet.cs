using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ElementalTools
{
	public class ItemMallet : Item
	{
		public ItemMallet( )
		{
		}

		#region WoodenClubClone

		public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
		{
		if (blockSel != null) return;

		byEntity.Attributes.SetInt("didattack", 0);

		byEntity.World.RegisterCallback((dt) => {
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		if (byPlayer == null) return;

		if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemAttack) {
		byPlayer.Entity.World.PlaySoundAt(new AssetLocation(GlobalConstants.DefaultDomain,"sounds/player/strike"), byPlayer, byPlayer, true, 16, 0.2f);
		}
		}, 464);

		handling = EnumHandHandling.PreventDefault;
		}

		public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
		return false;
		}

		public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
		{
		if (byEntity.World.Side == EnumAppSide.Client) {
		IClientWorldAccessor world = byEntity.World as IClientWorldAccessor;
		ModelTransform tf = new ModelTransform( );
		tf.EnsureDefaultValues( );

		tf.Rotation.X = Math.Min(60, secondsPassed * 360);
		if (secondsPassed > 0.3) {
		tf.Translation.Z -= Math.Min(1.5f, 36f * (secondsPassed - 0.3f));
		tf.Rotation.X -= Math.Max(-40, secondsPassed * 500);
		}


		byEntity.Controls.UsingHeldItemTransformAfter = tf;

		if (secondsPassed > 0.43f && byEntity.Attributes.GetInt("didattack") == 0) {
		world.TryAttackEntity(entitySel);
		byEntity.Attributes.SetInt("didattack", 1);
		world.AddCameraShake(0.25f);
		}
		}

		return secondsPassed < 0.9f;
		}

		public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
		{

		}


		#endregion

		/// <summary>
		/// When 'consumed' >> Utilized by crafting.
		/// </summary>
		/// <returns>The consumed by crafting.</returns>
		/// <param name="allInputSlots">All input slots.</param>
		/// <param name="stackInSlot">Stack in slot.</param>
		/// <param name="gridRecipe">Grid recipe.</param>
		/// <param name="fromIngredient">From ingredient.</param>
		/// <param name="byPlayer">By player.</param>
		/// <param name="quantity">Quantity.</param>
		public override void OnConsumedByCrafting(ItemSlot[ ] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{
		if (fromIngredient.IsTool) {
		int effectiveTier = 1;


		foreach (var itemSlot in allInputSlots) {
		if (itemSlot.Empty) continue;
		if ( itemSlot.Itemstack.Class == EnumItemClass.Block) {
		Block ingBlock = itemSlot.Itemstack.Block;
		effectiveTier = Math.Max(ingBlock.RequiredMiningTier, effectiveTier);
		}
		else {
		Item ingItem = itemSlot.Itemstack.Item;
		if (ingItem.Tool.HasValue) continue;
		effectiveTier = Math.Max(ingItem.ToolTier, effectiveTier);
		}
		}

		float burnRate = (effectiveTier / this.ToolTier);
						
		int actualDmg = ( int )Math.Round(NatFloat.createTri(effectiveTier, burnRate).nextFloat( ), 1);

		#if DEBUG
		api.World.Logger.VerboseDebug("Variable wear rate [ ToolTier:{0} VS {1}, BurnRate: {2} - apply dmg: {3} ]", this.ToolTier, effectiveTier, burnRate, actualDmg);
		#endif

		stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, actualDmg);
		return;
		}

		base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
		}


	}
}

