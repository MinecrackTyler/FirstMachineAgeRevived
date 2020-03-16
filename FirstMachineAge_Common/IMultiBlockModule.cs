using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Essentials;


namespace FirstMachineAge
{

	/*
	 MBM:

	'Cyto' block: points to Nucleus block - 'structural'
	'Membrane' block: points to Nucleus block, but also is input/output point for AbstractCircuits / Power
	'Nucleus' block: houses MBM state/data & definition, as well as list of component block pos, and prototype
	 */
	public interface IMultiBlockModule<T> where T : Block
	{		
		ulong UniqueModuleID { get; }
		IMultiBlockModule<T> NucleusBlock { get; }
		BlockPos NucleusLocation { get; }
		T HostBlock { get; }//The Nucleus - as block

		ILogicNetNode<T> LogicNode { get; }//Possibly null - Only "Membrane's" should have this...
		MBMType ComponentType { get; }

		IList<T> RelatedBlocks { get; }
		//Way to determine - annother module can connect here?
		bool CheckCompatibility(Block subject, BlockFacing forSide);//Could it be 'placed' if it were a "normal" block
		IMultiBlockModule<T> FuseBlock(Block subject, BlockFacing forSide);//Pass back resulting Complex, if fused together...
		IMultiBlockModule<T> FuseAt(Block subject, BlockFacing forSide, BlockPos here);//Pass back resulting Complex, if fused together...
		Block CleaveBlock( );//remove\extract this block from the complex
		Block CleaveAt(BlockPos here );//remove\extract this block from the complex

		BlockFacing[] OuterFaces { get; }
		BlockFacing[] InnerFaces { get; }//What MBM's touch this ~ 

	}



	public enum MBMType
	{
		Cyto,
		Membrane,
		Nucleus,
		//Vacuole // a "empty" 'Space' for Hardpoints or Sub-modules?
	}


}

