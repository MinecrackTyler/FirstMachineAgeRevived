using System;

//using System.CodeDom;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;




namespace ElementalTools
{
	/// <summary>
	/// Abstract Factory of (Item) Steel behavior inheriting things
	/// </summary>
	public static class FactoryOfSteel
	{
		private static Assembly thineOwnAssembly;
		private static AssemblyName steelAssemblyName;
		private static AssemblyBuilder stAsmBuilder;

		static FactoryOfSteel()
		{
		thineOwnAssembly = typeof(FactoryOfSteel).Assembly;
		AppDomain localAppDomain = Thread.GetDomain( );
		steelAssemblyName = new AssemblyName(@"Dynamic_Steel");
		stAsmBuilder = localAppDomain.DefineDynamicAssembly(
							 steelAssemblyName,
							 AssemblyBuilderAccess.RunAndCollect);//?
		}

		/// <summary>
		/// Emit Dynamic class of specified 'ResultingClassName'.
		/// </summary>
		/// <param name="ResultingClassName">Resulting class name.</param>
		public static Item ManufactureItemClass( string resultingClassName, Item original )
		{			
		ModuleBuilder moduleBuilder = stAsmBuilder.DefineDynamicModule(resultingClassName);

		

		//Item customItemClass = original.GetType( );

		//Generate class & remap methods to Steely ones...

		//var fWrapper = SourceItem.GetAttackPower;


		return customItemClass;
		}




	}
}

