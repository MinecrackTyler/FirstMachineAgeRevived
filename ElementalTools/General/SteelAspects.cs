using System;
using System.Linq;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElementalTools
{

	public static class SteelAspects
	{

		internal const string _timestampKey = @"timestamp";
		public const string hardenableKeyword = @"hardenable";
		public const string sharpenableKeyword = @"sharpenable";
		public const string metalNameKeyword = @"metalName";
		public const string hardnessKeyword = @"hardness";
		public const string sharpnessKeyword = @"sharpness";
		public const string durabilityKeyword = @"durability";
		public const float eutectoid_transition_temperature = 727f;//Celcius
		public const float quenchTimeConstant = 180f;
		public const float quench_min_temperature = 450f;//Celcius

		public static readonly BGRAColor_Int32 color_Rough = new BGRAColor_Int32(0xFF, 0x66, 0x00);
		public static readonly BGRAColor_Int32 color_Dull = new BGRAColor_Int32(0xFF, 0xBE, 0x00);
		public static readonly BGRAColor_Int32 color_Honed = new BGRAColor_Int32(0xE8, 0xFF, 0x00);
		public static readonly BGRAColor_Int32 color_Keen = new BGRAColor_Int32(0x7D, 0xFF, 0x00);
		public static readonly BGRAColor_Int32 color_Sharp = new BGRAColor_Int32(0x00, 0xFF, 0x12);
		public static readonly BGRAColor_Int32 color_Razor = new BGRAColor_Int32(0x00, 0xFF, 0xD7);
		public static readonly BGRAColor_Int32 color_Default = new BGRAColor_Int32(0xFF, 0x00, 0x00);

		/// <summary>
		/// Match against:Variant(s){   metal,	material  } == 'iron'
		/// </summary>
		/// <returns>The ferric metal.</returns>
		/// <param name="something">Something collectable.</param>
		public static bool IsFerricMetal(this CollectibleObject something)
		{
		return something.Variant.KeyValueMatch(ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.IronNameKey) ||
				 something.Variant.KeyValueMatch(ElementalToolsSystem.MaterialNameKey, ElementalToolsSystem.IronNameKey);
		}

		/// <summary>
		/// Match against:Variant(s){   metal,	material  } == 'steel'
		/// </summary>
		/// <returns>The Steel metal. </returns>
		/// <param name="something">Something collectable.</param>
		public static bool IsSteelMetal(this CollectibleObject something)
		{
		return something.Variant.KeyValueEndingMatch(ElementalToolsSystem.MetalNameKey, ElementalToolsSystem.SteelNameKey) ||
				 something.Variant.KeyValueEndingMatch(ElementalToolsSystem.MaterialNameKey, ElementalToolsSystem.SteelNameKey);
		}

		/// <summary>
		/// Using ItemSharpener class....
		/// </summary>
		/// <returns>If a sharpener.</returns>
		/// <param name="something">Something.</param>
		public static bool IsSharpener(this CollectibleObject something)
		{
		return String.Equals(something.Class, ElementalToolsSystem.sharpeningStoneItemKey, StringComparison.Ordinal);
		}

		/// <summary>
		/// Has Edge that can wear down...
		/// </summary>
		/// <returns>The impliment.</returns>
		/// <param name="what">What.</param>
		public static bool EdgedImpliment(this EnumTool? what)
		{
		if (what != null || what.HasValue && (
				what == EnumTool.Axe ||
				what == EnumTool.Chisel ||
				what == EnumTool.Hoe ||
				what == EnumTool.Knife ||
				what == EnumTool.Pickaxe ||
				what == EnumTool.Saw ||
				what == EnumTool.Scythe ||
				what == EnumTool.Shears ||
				what == EnumTool.Sickle ||
				what == EnumTool.Spear ||
				what == EnumTool.Sword)
			) {
		return true;
		}
		return false;
		}

		/// <summary>
		/// Consider this as Weaspon ONLY (Axe being the special exception)
		/// </summary>
		/// <returns>The impliment.</returns>
		/// <param name="what">What.</param>
		public static bool Weapons(this EnumTool? what)
		{
		if (what != null || what.HasValue && (
				what == EnumTool.Axe || //Arguable
				what == EnumTool.Bow ||
				what == EnumTool.Knife ||
				what == EnumTool.Spear ||
				what == EnumTool.Sling ||
				what == EnumTool.Sword)
			) {
		return true;
		}
		return false;
		}

		/// <summary>
		/// Consider this as Tools ONLY (Axe being the special exception)
		/// </summary>
		/// <returns>The impliment.</returns>
		/// <param name="what">What.</param>
		public static bool Tools(this EnumTool? what)
		{
		if (what != null || what.HasValue && (
				what == EnumTool.Axe || //Arguable
				what == EnumTool.Hammer||
				what == EnumTool.Hoe ||
				what == EnumTool.Chisel ||
				what == EnumTool.Drill ||
				what == EnumTool.Meter ||
				what == EnumTool.Pickaxe ||
				what == EnumTool.Probe ||
				what == EnumTool.Saw ||
				what == EnumTool.Scythe ||
				what == EnumTool.Shears ||
				what == EnumTool.Shovel ||
				what == EnumTool.Sickle ||
				what == EnumTool.Wrench 
			)
			) {
		return true;
		}
		return false;
		}

		public static bool RecomendedUsage(this Item item,  EnumBlockMaterial blockMaterial)
		{
		if (item.MiningSpeed != null && item.MiningSpeed.ContainsKey(blockMaterial)) return true;
		return false;
		}

		public static SteelThingViaStack AsSteelThing(this IItemStack someStack)
		{
		if (someStack.Class == EnumItemClass.Item && someStack.Item is ISteelBase) {
		return new SteelThingViaStack(someStack);
		}

		return null;
		}

		public static void SetHitpoints(IItemStack recipient, int wearLevel)
		{
		recipient.Attributes.SetInt(SteelAspects.durabilityKeyword, wearLevel);
		}

		#region Common Steel Methods

		public static void GetHeldItemInfo(ICoreAPI api, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
		if (inSlot == null || inSlot.Empty || inSlot.Inventory == null) {
		#if DEBUG
		api.World.Logger.Warning("GetHeldItemInfo -> Invetory / slot / stack: FUBAR!");
		#endif
		return;
		}					
		var steelInfo = inSlot.Itemstack.AsSteelThing( );

		dsc.AppendFormat(Lang.Get(@"fma:prop-metal", Lang.GetUnformatted(@"fma:metalname-" + steelInfo.BaseMetalName)));

		if (steelInfo.Hardenable || steelInfo.Hardness != HardnessState.Soft) {
			dsc.Append(@"<font color='#007F7F'>");
			dsc.AppendFormat(Lang.Get(@"fma:prop-temper", Lang.GetUnformatted(@"fma:hardness-" +  (int)steelInfo.Hardness)));
			dsc.Append("</font>");
			}

		if (steelInfo.Sharpenable) {
			dsc.Append(@"<font color='#5C8282'>");
			dsc.AppendFormat(Lang.Get(@"fma:prop-edge", Lang.GetUnformatted(@"fma:sharpness-" + ( int )steelInfo.Sharpness)));
			dsc.Append("</font>");
			}
		}

		/// <summary>
		/// For; Quench-hardening...(in fluid)
		/// </summary>
		/// <param name="entityItem">Entity item.(Itself)</param>
		public static void QuenchHarden(ISteelBase steelBased, EntityItem entityItem, ICoreAPI api)
		{
		if (api.Side.IsServer( ) && (entityItem.Swimming || entityItem.FeetInLiquid)) {

		if (!steelBased.Hardenable) return;

		float temperature = entityItem.Itemstack.Collectible.GetTemperature(api.World, entityItem.Itemstack);
		//Track first moment in liquid;
		SteelAspects.SetTimestamp(entityItem);//Need to clear when NORMALIZING.

		//temperature <= eutectoid_transition_temperature ||
		if (temperature >= quench_min_temperature) {
		//TODO: Thermal capacity & Transfer coefficients for NON-Water fluids...and surfaces too!
		var elapsedTime = SteelAspects.GetTimestampElapsed(entityItem);

		uint quenchUnits = ( uint )Math.Round(elapsedTime.TotalMilliseconds / quenchTimeConstant, 0);

		var steelThing = entityItem.Itemstack.AsSteelThing();

		if (quenchUnits < ( uint )HardnessState.Brittle) {
		steelThing.Hardness = ( HardnessState )quenchUnits;
		}
		else {
		steelThing.Hardness =  HardnessState.Brittle;
		}

		//Being that water conducts heat well - reduce Temperature _FASTER_
		entityItem.Itemstack.Collectible.SetTemperature(api.World, entityItem.Itemstack, temperature - 15, false);

		#if DEBUG
		api.World.Logger.VerboseDebug("Quench process: {0}S elapsed @{1}C H:{2} ~ QU#{3}", elapsedTime.TotalSeconds, temperature, steelThing.Hardness, quenchUnits);
		#endif
		}
		}					
		}

		public static float AttackPower(ISteelBase steelBased, IItemStack withItemStack, ICoreAPI api)
		{
		var defaultPower = withItemStack.Item.AttackPower;
		var steelThing = withItemStack.AsSteelThing( );

		if (steelBased.Sharpenable && withItemStack.Item.Tool.EdgedImpliment() ) {
		var sharpness = steelThing.Sharpness;
		float pctBoost = 0;//CONSIDER: Perhaps make this external?

		switch (sharpness) {
		case SharpnessState.Rough:
			pctBoost = -0.25f;
			break;
		case SharpnessState.Dull:
			pctBoost = -0.20f;
			break;
		case SharpnessState.Honed:
			pctBoost = 0.10f;
			break;
		case SharpnessState.Keen:
			pctBoost = 0.15f;
			break;
		case SharpnessState.Sharp:
			pctBoost = 0.20f;
			break;
		case SharpnessState.Razor:
			pctBoost = 0.25f;
			break;
		}

		return defaultPower + (pctBoost * defaultPower);
		}

		return defaultPower;
		}

		public static float MiningSpeed(IItemStack itemStack, BlockSelection blockSel, Block block, IPlayer forPlayer, ICoreAPI api)
		{
		var baseSpeed = 1f;
		var steelThing = itemStack.AsSteelThing( );
		var item = itemStack.Item;

		//Boost for Edged tools / weapons
		if (item.MiningSpeed != null && item.MiningSpeed.ContainsKey(block.BlockMaterial) && item.Tool.EdgedImpliment( )) {

		baseSpeed = item.MiningSpeed[block.BlockMaterial] * GlobalConstants.ToolMiningSpeedModifier;

		float pctBoost = 0f;
		switch (steelThing.Sharpness) {
		case SharpnessState.Rough:
			pctBoost = -0.35f;
			break;
		case SharpnessState.Dull:
			pctBoost = -0.20f;
			break;
		case SharpnessState.Honed:
			pctBoost = 0.10f;
			break;
		case SharpnessState.Keen:
			pctBoost = 0.20f;
			break;
		case SharpnessState.Sharp:
			pctBoost = 0.25f;
			break;
		case SharpnessState.Razor:
			pctBoost = 0.30f;
			break;
		}

		return baseSpeed + (pctBoost * baseSpeed);
		}

		return baseSpeed;
		}

		public static void WhenUsedInAttack(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot, ICoreAPI api)
		{
		var item = itemslot.Itemstack.Item;
		bool edged = item.Tool.EdgedImpliment();
		bool weapon = item.Tool.Weapons();		
		var steelThing = itemslot.Itemstack.AsSteelThing( );					

		//Only called for attacks on ENTITIES. Scen# 1 - 4 here.
		#if DEBUG
		api.World.Logger.VerboseDebug($"OnAttackingWith:: (Weap:{weapon},Edge:{edged}) {byEntity.Code} -> {attackedEntity.Code}");
		#endif

		TakeDamage(world, byEntity ,attackedEntity, null, itemslot, api );

		if (steelThing.Hardness > HardnessState.Hard) //VS.: High-Tier Mobs / High-Tier Player armor ?
			{
			bool catasptrophicFailure = world.Rand.Next(1, 1000) >= 999;
			if (catasptrophicFailure) 
				{
				#if DEBUG
				world.Logger.VerboseDebug("Catastrophic brittle fracture of {0} !", item.Code);
				#endif
				SteelAspects.SetHitpoints(itemslot.Itemstack, 0);
				item.DamageItem(world, byEntity, itemslot, 9999);
				return;
				}
			}		
		}

		public static void WhenUsedForBlockBreak(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, ICoreAPI api )
		{
		var item = itemslot.Itemstack.Item;
		bool edged = item.Tool.EdgedImpliment( );
		bool weapon = item.Tool.Weapons( );
		
		var steelThing = itemslot.Itemstack.AsSteelThing( );

		var targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
		int targetTier = targetBlock.ToolTier;
		float targetResistance = targetBlock.Resistance;
		bool recomendedUsage = item.RecomendedUsage(targetBlock.BlockMaterial);
		var hardness = steelThing.Hardness;				

		//Only called for attacks on BLOCKS / Envrionment. Scen# 5 - 6 here.	
		
		#if DEBUG
		api.World.Logger.VerboseDebug($"OnBlockBrokenWith:: (Weap:{weapon},Edge:{edged},OK: {recomendedUsage},T.T#{targetTier}) {byEntity.Code} -> {targetBlock.Code}");
		#endif

		TakeDamage(world, byEntity, null, blockSel, itemslot, api);

		if (recomendedUsage == false && hardness > HardnessState.Hard) {
		bool catasptrophicFailure = world.Rand.Next(1, 1000) >= (999 - (targetTier * 5));

		if (catasptrophicFailure) {
		world.Logger.VerboseDebug("Catastrophic brittle fracture of {0} !", item.Code);
		SteelAspects.SetHitpoints(itemslot.Itemstack, 0);
		item.DamageItem(world, byEntity, itemslot, 9999);
		
		}
		}							
		}

		public static void TakeDamage(IWorldAccessor world, Entity owner, Entity attackOnEntity, BlockSelection breakingBlock, ItemSlot itemslot, ICoreAPI api, int nomAmmount = 1)
		{
		var item = itemslot.Itemstack.Item;		
		var steelThing = itemslot.Itemstack.AsSteelThing( );
		
		/*DETERMINE: 
		* Usage - Blade/Edged weapon attack Vs. creature Sc.#1 [What about armored players?]
		* Non-edged (blunt) weapon vs. creature Sc. #2 [What about armored players?]
		* [Improvised-arms] Edged-Tool (non-weapon) vs. Creature Sc.#3
		* [Improvised-arms] Blunt-Tool (non-weapon) vs. Creature Sc.#4
		* Tool Against Envrionment (Pickaxe / Axe / Propick / Saw / Shovel) Sc. #5
		* WEAPONS Vs. Envrionment (hiting dirt with a sword!) Sc. #6
		* Tools - don't really benefit from edges vs. envrionment...?
		*/
		uint extraBias = 0;

		if (attackOnEntity != null) 
		{
		bool edged = item.Tool.EdgedImpliment( );
		bool weapon = item.Tool.Weapons( );		
		int armourTier = 0;//Most Creatures have no 'Armour' class...?
		float damageFactor = 0f;//Scaling
		int maxDamage = 1;
		int rndDamageOutcome = 1;		

		if (attackOnEntity is EntityPlayer) {
		var atkdPlayer = attackOnEntity as EntityPlayer;		
		var atkeeInv = atkdPlayer.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
		var armourSlots = new ItemSlot[ ] { atkeeInv[( int )EnumCharacterDressType.ArmorHead], atkeeInv[( int )EnumCharacterDressType.ArmorBody], atkeeInv[( int )EnumCharacterDressType.ArmorLegs] };
		//TODO: Cache Armor combined-stat data...too pesky to extract all the time

		foreach (var aSlot in armourSlots) {
		if (aSlot.Empty == false && aSlot.Itemstack.Class == EnumItemClass.Item && aSlot.Itemstack.Item is ItemWearable) {
			var armourItem = aSlot.Itemstack.Item as ItemWearable;
			if (armourItem.ProtectionModifiers != null) armourTier = Math.Max(armourItem.ProtectionModifiers.ProtectionTier, armourTier);
			}
		}
		}

		//Scenarios #1, thru #4...
		if (edged && weapon) {//#1 Edged weapon
		int tierDisparity = armourTier - item.ToolTier;
		damageFactor = 1 + (tierDisparity * 0.2f) - (( int )(steelThing.Hardness) * 0.10f);
		maxDamage = 1 + (HardnessState.Brittle - steelThing.Hardness);
		extraBias = ( uint )(tierDisparity * 5);

		}
		else if (weapon) {//#2 Blunt Weapon

		}
		else if (edged && !weapon) {//#3 Improvised Edged weapon

		}
		else {//#4 Improvised Blunt weapon
		
		}

		rndDamageOutcome = api.World.Rand.Next(0, ( int )(maxDamage * damageFactor));
		if (rndDamageOutcome > 0) item.DamageItem(world, null, itemslot, rndDamageOutcome);

		}//Entities being attacked; ends.
		else if (breakingBlock != null) 
		{
		//Scenarios #5, #6...
		bool tool = item.Tool.Tools( );
		if (tool) {//#5
		
		}
		else {//#6
		
		
		}	
		}//Blocks being broken; ends.

		//TODO: Handle Crafting \ Other damage sources...here? (also done elsewhere)
		MabeyDull(steelThing,api, extraBias );
		
		}

		/// <summary>
		/// Dull and/or DamageItem 
		/// </summary>
		public static void ToolInRecipeUse(Item @this, ItemSlot[ ] allInputSlots, GridRecipe matchingRecipe, ICoreAPI api )
		{
		var steelToolSlot = (from inputSlot in allInputSlots
							 where inputSlot.Empty == false
							 where inputSlot.Itemstack.Class == EnumItemClass.Item
							 where inputSlot.Itemstack.Collectible.Code == @this.Code
							 select inputSlot).FirstOrDefault( );//Freaky recipie with TWO of same tool?!

		var steelThing = steelToolSlot.Itemstack.AsSteelThing( );
		//Edged tool vs. non-edged tool
		bool edgedTool = @this.Tool.EdgedImpliment( );

		float hardnessMult = (( int )HardnessState.Brittle + 1) / (( int )steelThing.Hardness + 1) * 0.25f;
		float wearMax = 1;

		if (edgedTool) {
		wearMax = ( byte )SharpnessState.Razor - ( byte )steelThing.Sharpness;
		}

		int actualDmg = ( int )Math.Round(NatFloat.createTri(wearMax, hardnessMult).nextFloat(1.0f, api.World.Rand), 1);

		#if DEBUG
		api.World.Logger.VerboseDebug($"tooluse [{@this.Code}] --> Harndess effect: [ Hardness {hardnessMult} Vs. Rate: {wearMax} apply dmg: {actualDmg}, edged: {edgedTool} ]");
		#endif
		if (actualDmg > 0) 
			{
			steelToolSlot.Itemstack.Collectible.DamageItem(api.World, null, steelToolSlot, actualDmg);
			MabeyDull(steelThing, api);
			}

		}

		public static void MabeyDull(SteelThingViaStack someSteelyThing, ICoreAPI api, uint extraBias = 0)
		{
		// **************** Edge blunting
		var hardness = someSteelyThing.Hardness;
		bool edgeBlunting = false;
		switch (hardness) {

		case HardnessState.Soft:
			edgeBlunting = api.World.Rand.Next(1, 100) >= (99 - extraBias);
			break;

		case HardnessState.Medium:
			edgeBlunting = api.World.Rand.Next(1, 200) >= (199 - extraBias);
			break;

		case HardnessState.Hard:
			edgeBlunting = api.World.Rand.Next(1, 300) >= (299 - extraBias);
			break;

		case HardnessState.Brittle:
			edgeBlunting = api.World.Rand.Next(1, 400) >= 399;
			break;
		}
		if (edgeBlunting) someSteelyThing.Dull();
		}

		public static void SharpenOneSteelItem(ItemSlot[ ] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ICoreAPI api)
		{
		//Failsafe[s]
		string name = "unset!";
		if (byRecipe == null || byRecipe.Ingredients == null || byRecipe.IngredientPattern == null || byRecipe.Output == null) {		
		name = byRecipe?.Name.ToString( );
		api.World.Logger.Error("Invalid / Incomplete / Corrupt (sharpening) Recipe: {0}", name);
		return;
		}

		var steelItemSlot = (from inputSlot in allInputslots
							 where inputSlot.Empty == false
							 where inputSlot.Itemstack.Class == EnumItemClass.Item
							 where inputSlot.Itemstack.Collectible.IsSteelMetal( )
							 select inputSlot).SingleOrDefault( );

		var sharpenerItemSlot = (from inputSlot in allInputslots
								 where inputSlot.Empty == false
								 where inputSlot.Itemstack.Class == EnumItemClass.Item
								 where inputSlot.Itemstack.Collectible.IsSharpener( )
								 select inputSlot).SingleOrDefault( );

		if (sharpenerItemSlot.Empty) return;//Not a sharpening recipie

		if (steelItemSlot != null && steelItemSlot.Itemstack.Class == EnumItemClass.Item) {
		if (steelItemSlot.Itemstack.Item is ISteelByStack) 
			{
			//OLD WAY
			var steelItem = steelItemSlot.Itemstack.Item;

			#if DEBUG
			api.World.Logger.VerboseDebug("Input (ingredient) Item {0} supports; ISteelByStack Interface ", steelItem.Code);
			#endif
			if (!outputSlot.Empty && outputSlot.Itemstack.Class == EnumItemClass.Item
				&& outputSlot.Itemstack.Item is ISteelByStack) 
				{
				var outputItem = outputSlot.Itemstack.Item;
				var fullMetalInterface = outputSlot.Itemstack.Item as ISteelByStack;
				#if DEBUG
				api.World.Logger.VerboseDebug("Output Item {0} supports; ISteelByStack Interface ", steelItem.Code);
				#endif

				fullMetalInterface.CopyStackAttributes(steelItemSlot.Itemstack, outputSlot.Itemstack);

				if (sharpenerItemSlot != null) fullMetalInterface.Sharpen(outputSlot.Itemstack);
				#if DEBUG
				api.World.Logger.VerboseDebug("Attributes perpetuated from {0} to {1} ", steelItem.Code, outputItem.Code);
				#endif
				}
				} else if (steelItemSlot.Itemstack.Item is ISteelThingInstance)
				{
				//NEW WAY
				var oldSteelThing = steelItemSlot.Itemstack.AsSteelThing( );

				oldSteelThing.CloneStackAttributes(outputSlot.Itemstack);
				#if DEBUG
				api.World.Logger.VerboseDebug("Attributes perpetuated from {0} to {1}, using ISteelThingInstance ", steelItemSlot.Itemstack.Item.Code, outputSlot.Itemstack.Item.Code);
				#endif
		}
			
			}
		else {
				#if DEBUG
				api.World.Logger.Debug("Could not find steel item; in Recipie:{0} ",name);
				#endif
			}
		}

		public static void SetTimestamp(EntityItem entityItem)
		{

		if (!entityItem.Attributes.HasAttribute(SteelAspects._timestampKey)) {
		entityItem.Attributes.SetLong(SteelAspects._timestampKey, DateTime.Now.Ticks);
		}
		}

		public static TimeSpan GetTimestampElapsed(EntityItem entityItem)
		{
		if (entityItem.Attributes.HasAttribute(SteelAspects._timestampKey)) {
		var ts = TimeSpan.FromTicks(entityItem.Attributes.GetLong(SteelAspects._timestampKey));
		return ts.Subtract(TimeSpan.FromTicks(DateTime.Now.Ticks)).Negate( );
		}
		return TimeSpan.Zero;
		}

		#endregion
	}
}
