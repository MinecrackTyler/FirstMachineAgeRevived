using System;
using System.IO;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AnvilMetalRecovery
{
	public class HotbarObserverData : IAttribute
	{		
		public AssetLocation ItemCode { get; private set; }
		public int SlotID { get; private set; }
		public string PlayerUID { get; private set; }

		public HotbarObserverData(int slotID, Item item, string playerUID)
		{
		SlotID = slotID;
		this.ItemCode = item.Code.Clone();
		PlayerUID = playerUID;
		}

		public bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
		{
		throw new NotImplementedException( );
		}

		public void FromBytes(BinaryReader stream)
		{
		SlotID = stream.ReadInt32( );
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
		stream.Write(SlotID );
		stream.Write(ItemCode.ToString());
		stream.Write(PlayerUID);
		}

		public string ToJsonToken( )
		{
		throw new NotImplementedException( );
		}
	}
}

