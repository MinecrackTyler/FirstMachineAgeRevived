using System;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
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



		public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
		base.FromTreeAtributes(tree, worldAccessForResolve);
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
		var boltableDoor = this.Block as BoltableDoor;
		BoltableDoorBlockEntity realEntity = boltableDoor.Entity(this.Pos.Copy( ));
		if (realEntity != null) dsc.AppendLine($"Bolted: {(realEntity.Bolted?"<font color='red'>Yes</font>":"No")}");		
		}
	}
}

