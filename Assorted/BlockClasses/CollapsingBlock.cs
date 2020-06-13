using System;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class CollapsingBlock : Block, ITexPositionSource
	{
		internal const string CamoKey = @"camo";
		internal const string WoodKey = @"wood";

		#region ITexPositionSource
		internal string camoMaterial;
		public Size2i AtlasSize { get; private set; }
		private ITexPositionSource camoTextureSource;
		private ITexPositionSource woodTextureSource;
		private ITexPositionSource defaultTextureSource;

		public TextureAtlasPosition this[string textureCode] {
			get
			{
			if (camoTextureSource == null ) camoTextureSource = ClientAPI.Tesselator.GetTexSource(this);
			if (woodTextureSource == null)  woodTextureSource = ClientAPI.Tesselator.GetTexSource(this);
			if (defaultTextureSource == null)  defaultTextureSource = ClientAPI.Tesselator.GetTexSource(this);


			if (textureCode == CamoKey) return camoTextureSource[CamoKey];
			if (textureCode == WoodKey) return woodTextureSource[WoodKey];
			
			return defaultTextureSource[textureCode];
			}
		}

		#endregion
		/*
		EntityPlayer : EntityHumanoid
		EntityHumanoid : EntityAgent
		EntityPlayerBot : EntityAnimalBot
		EntityAnimalBot : EntityAgent
		EntityAgent
		 */
		public CollapsingBlock( )
		{

		}

		public ICoreClientAPI ClientAPI 
		{
			get { return this.api as ICoreClientAPI; }
		}

		internal CollapsingBlockEntity Entity(BlockPos here)
		{
		var collapseBlockEntity = api.World.BlockAccessor.GetBlockEntity(here) as CollapsingBlockEntity;

		if (collapseBlockEntity == null) {
		#if DEBUG
		api.World.Logger.Warning($"CollapsingBlockEntity [{here}]: BlockEntity NULL! (regenerating)");
		#endif
		api.World.BlockAccessor.SpawnBlockEntity(AssortedModSystems.CollapsingBlockEntityNameKey, here);
		}

		return collapseBlockEntity;
		}


		public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
		{
		//Get Ready to CRUMBLE!
		api.World.Logger.VerboseDebug($"OnEntityInside ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos}");
		}

		public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
		{
		//Start to shake...with particles , dust
		api.World.Logger.VerboseDebug($"OnEntityCollide ({entity.Code}) of [{entity.GetType( ).Name}] @ {pos} impact: {isImpact}");
		//Check 'Volumne' of Bounding box; large ones - cause collapse...

		}
	}
}

/*
Recipie: attributes: {camo: "{camo}" }
As 'Item' -> Attribute  
public override bool DoPlaceBlock -> BlockEntity settings string material = byItemStack.Attributes.GetString("material");
public override ItemStack OnPickBlock { stack.Attributes.SetString("camo", be.camo); }
*/
