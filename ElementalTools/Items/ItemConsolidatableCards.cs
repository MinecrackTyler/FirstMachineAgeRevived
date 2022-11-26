using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElementalTools
{
	public class ItemConsolidatableCards : Item, IAnvilWorkable
	{


		#region IAnvilWorkable
		public bool CanWork(ItemStack stack)
		{
		throw new NotImplementedException( );
		}

		public ItemStack GetBaseMaterial(ItemStack stack)
		{
		return stack;//Or Shear steel?
		}

		public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
		{
		return EnumHelveWorkableMode.NotWorkable;//Manual only
		}

		public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
		{
			throw new NotImplementedException( ); //(X) -> Steel Ingot
		}

		public int GetRequiredAnvilTier(ItemStack stack)
		{
		return 3;//Iron+
		}

		public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
		{
			//Set smithing Voxels here.

		/*
  		if (!CanWork(stack)) return null;

            Item item = api.World.GetItem(new AssetLocation("workitem-" + Variant["metal"]));
            if (item == null) return null;

            ItemStack workItemStack = new ItemStack(item);
            workItemStack.Collectible.SetTemperature(api.World, workItemStack, stack.Collectible.GetTemperature(api.World, stack));

            if (beAnvil.WorkItemStack == null)
            {
                CreateVoxelsFromIngot(api, ref beAnvil.Voxels, isBlisterSteel);
            } else
            {
                if (isBlisterSteel) return null;

                IAnvilWorkable workable = beAnvil.WorkItemStack.Collectible as IAnvilWorkable;

                if (!workable.GetBaseMaterial(beAnvil.WorkItemStack).Equals(api.World, GetBaseMaterial(stack), GlobalConstants.IgnoredStackAttributes))
                {
                    if (api.Side == EnumAppSide.Client) (api as ICoreClientAPI).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                    return null;
                }

                AddVoxelsFromIngot(api, ref beAnvil.Voxels);
            }

            return workItemStack;
 		*/
		return null;
		}
		#endregion

		protected byte[ , , ] GenSmithingVoxels()
		{
		//Cards standing vertically, some-slag also randomly...
		var voxels = new byte[16, 6, 16];

		return voxels;		
		}


	}
}

