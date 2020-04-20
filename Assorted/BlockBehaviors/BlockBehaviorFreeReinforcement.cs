using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	//behaviors: [{name: "FreeReinforcement", properties: { howmuch: 5 }}],		}
	public class BlockBehaviorFreeReinforcement : BlockBehavior
	{
		private const string _howmuchKey = @"howmuch";

		private ModSystemBlockReinforcement ReinforcementSystem;

		private uint Howmuch 
		{
			get
			{
			return properties[_howmuchKey].AsType<uint>(1u);
			}
		}
		 
		public BlockBehaviorFreeReinforcement(Block block) : base(block)
        {
		}

		public override void OnLoaded(ICoreAPI api)
		{
			
		if (api.Side.IsServer( ) && api.ModLoader.IsModSystemEnabled(@"Vintagestory.GameContent.ModSystemBlockReinforcement")) {
		ReinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>( );
		#if DEBUG
		api.World.Logger.Debug("FreeReinforcements a'Go");
		#endif
		}

		}

		public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
		{
		handling = EnumHandling.PassThrough;

		world.Api.Event.RegisterCallback((elapse) => { PostPlacementReinforce(elapse,blockSel.Position.Copy( ), byPlayer, this.Howmuch); }, 16);

		return true;
		}

		private void PostPlacementReinforce(float elapse,BlockPos pos, IPlayer player, uint ammount)
		{
		if (this.ReinforcementSystem != null ) {	
		
		ReinforcementSystem.StrengthenBlock(pos.Copy( ), player, ( int )this.Howmuch);
		}
		}

	}
}

