using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ElementalTools
{
	[StructLayout(LayoutKind.Explicit, Size = 4, Pack = 1)]
	public struct RGBAColor_Int32 
	{
		[FieldOffset(0)]
		public byte Red;

		[FieldOffset(1)]
		public byte Green;

		[FieldOffset(2)]
		public byte Blue;

		[FieldOffset(3)]
		public byte Alpha;

		[FieldOffset(0)]//Overlaps 4 bytes before
		public readonly int IntegerValue;

		public RGBAColor_Int32(byte r, byte g, byte b)
		{
		IntegerValue = 0;
		Red = r;
		Green = g;
		Blue = b;
		Alpha = 0;
		}

		public RGBAColor_Int32(byte r, byte g, byte b, byte a)
		{
		IntegerValue = 0;
		Red = r;
		Green = g;
		Blue = b;
		Alpha = a;
		}


	}

	[StructLayout(LayoutKind.Explicit, Size = 4, Pack = 1)]
	public struct BGRAColor_Int32
	{
		[FieldOffset(2)]
		public byte Red;

		[FieldOffset(1)]
		public byte Green;

		[FieldOffset(0)]
		public byte Blue;

		[FieldOffset(3)]
		public byte Alpha;

		[FieldOffset(0)]//Overlaps 4 bytes before
		public readonly int IntegerValue;

		public BGRAColor_Int32(byte r, byte g, byte b)
		{
		IntegerValue = 0;
		Red = r;
		Green = g;
		Blue = b;
		Alpha = 0;
		}

		public BGRAColor_Int32(byte r, byte g, byte b, byte a)
		{
		IntegerValue = 0;
		Red = r;
		Green = g;
		Blue = b;
		Alpha = a;
		}


	}




}

