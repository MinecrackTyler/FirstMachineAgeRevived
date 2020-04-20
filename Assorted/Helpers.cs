using System;

using Newtonsoft.Json.Linq;

using Vintagestory.API;

namespace FirstMachineAge
{
	internal static class Helpers
	{
		internal static T AsType<T>(this JsonObject @this, T defaultValue = default(T)) where  T : struct
		{
		if (@this.Exists == false) return defaultValue;
		if (!(@this.Token is JValue)) return defaultValue;
		

		return @this.Token.Value<T>();
		}

	}
}

