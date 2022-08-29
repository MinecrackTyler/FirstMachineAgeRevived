using System;
using System.IO;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AnvilMetalRecovery
{
	public class HotbarObserverData : IAttribute
	{		
		public AssetLocation ItemCode { get; private set; }
		public string InventoryID { get; private set; }
		public int Inventory_SlotID { get; private set; }
		public string PlayerUID { get; private set; }

		public HotbarObserverData(string inventoryID, int slotID, AssetLocation itemCode, string playerUID)
		{
		InventoryID = inventoryID;
		Inventory_SlotID = slotID;
		this.ItemCode = itemCode.Clone();
		PlayerUID = playerUID;
		}

		public bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
		{
		throw new NotImplementedException( );
		}

		public void FromBytes(BinaryReader stream)
		{
		InventoryID = stream.ReadString( );
		Inventory_SlotID = stream.ReadInt32( );
		ItemCode = new AssetLocation( stream.ReadString( ));
		PlayerUID = stream.ReadString( );
		}

		public int GetAttributeId( )
		{
		return 75505;//Seems like a collision risk still....GUIDS?
		}

		public object GetValue( )
		{
		throw new NotImplementedException( );
		}

		public void ToBytes(BinaryWriter stream)
		{
		stream.Write(InventoryID );
		stream.Write(Inventory_SlotID);
		stream.Write(ItemCode.ToString());
		stream.Write(PlayerUID);
		}

		public string ToJsonToken( )
		{
		throw new NotImplementedException( );
		}

		public IAttribute Clone( )
		{
		var newbie = new HotbarObserverData(this.InventoryID, this.Inventory_SlotID, this.ItemCode.Clone( ), this.PlayerUID);

		return newbie;
		}
	}
}

