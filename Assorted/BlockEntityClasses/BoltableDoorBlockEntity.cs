using System;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FirstMachineAge
{
	public class BoltableDoorBlockEntity : BlockEntity
	{
		private string _boltedKey = @"Bolted";

		public bool Bolted { get; set; }

		public override void Initialize(ICoreAPI api)
		{
		base.Initialize(api);
		
		}

		public BoltableDoor DoorBlock {
			get { return this.Block as BoltableDoor; }
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		this.Bolted = tree.GetBool(_boltedKey, false);		
		}


		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		base.ToTreeAttributes(tree);		
		tree.SetBool(_boltedKey, this.Bolted);				
		}


		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
		base.GetBlockInfo(forPlayer, dsc);

		if (DoorBlock.IsOpen == false ) 
		{
			if (forPlayer?.CurrentBlockSelection.SelectionBoxIndex == 1 ) 
			{
			//BoltableDoorBlockEntity realEntity = DoorBlock.Entity(this.Pos.Copy( ));
			dsc.AppendLine(this.Bolted ? Lang.Get("defensive:bolted_shut") : Lang.Get("defensive:bolted_open"));
			}
		}
		}
	}
}

