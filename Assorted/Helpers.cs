using System;

using EnumsNET;

using Newtonsoft.Json.Linq;

using Vintagestory.API;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;



namespace FirstMachineAge
{
	internal static class Helpers
	{
		internal static T AsType<T>(this JsonObject @this, T defaultValue = default(T)) where  T : struct
		{
		if (@this.Exists == false) return defaultValue;
		if (!(@this.Token is JValue)) return defaultValue;


		return @this.Token.Value<T>( );
		}

		internal static T[] FromEnumStrings<T>(this JsonObject @this) where T : struct //, Enum
		{
		var stuffList = @this.AsArray<string>( );		

		var resultList = new T[stuffList.Length];
		for (int i = 0; i<stuffList.Length; i++) {
		var stuff = default(T);
				if (Enums.TryParseUnsafe(stuffList[i], out stuff))
				{
				resultList[i] = stuff;
				}
			}
		return resultList;
		}


		internal static bool Above(this BlockPos pos, BlockPos other)
		{
		if (pos.UpCopy( ) == other.Copy( )) return true;

		return false;
		}
	}
}

