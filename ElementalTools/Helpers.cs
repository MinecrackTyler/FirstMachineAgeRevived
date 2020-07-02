using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTools
{
	public static class Helpers
	{

		public static bool KeyValueMatch(this IDictionary<string, string> stringyDict, string key, string value) 
		{
		if (stringyDict.ContainsKey(key)) {
		return stringyDict.Any(kvp => String.CompareOrdinal(kvp.Key, key) == 0
							   && String.CompareOrdinal(kvp.Value, value) == 0);
		}

		return false;
		}

	}
}

