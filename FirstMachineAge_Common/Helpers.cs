using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace FirstMachineAge
{
	public static class Helpers
	{



		public static string PrettyCoords(this BlockPos location, ICoreAPI CoreApi)
		{
			var start = CoreApi.World.DefaultSpawnPosition.AsBlockPos;

			return string.Format("X{0}, Y{1}, Z{2}", location.X - start.X, location.Y, location.Z - start.Z);
		}

		public static BlockPos AverageHighestPos(List<BlockPos> positions)
		{
			int x = 0, y = 0, z = 0, length = positions.Count;
			foreach (BlockPos pos in positions) {
				x += pos.X;
				y = Math.Max(y, pos.Y);//Mutant Y-axis, take "HIGHEST"
				z += pos.Z;
			}
			return new BlockPos(x / length, y, z / length);
		}

		public static BlockPos PickRepresentativePosition(List<BlockPos> positions)
		{
			var averagePos = AverageHighestPos(positions);
			if (positions.Any(pos => pos.X == averagePos.X && pos.Y == averagePos.Y && pos.Z == averagePos.Z)) {
				return averagePos;//lucky ~ center was it!
			}

			//Otherwise...pick one
			var whichever = positions.Last(poz => poz.Y == averagePos.Y);

			return whichever;
		}



		/// <summary>
		/// Find a BLOCK partial path match: BlockID
		/// </summary>
		/// <returns>Matching finds</returns>
		/// <param name="assetName">Asset name.</param>
		public static Dictionary<int, string> ArbitrarytBlockIdHunter(this ICoreAPI CoreApi, AssetLocation assetName, EnumBlockMaterial? material = null)
		{
			Dictionary<int, string> arbBlockIDTable = new Dictionary<int, string>( );
			uint emptyCount = 0;

			if (CoreApi.World.Blocks != null) {

#if DEBUG
				CoreApi.World.Logger.VerboseDebug(" World Blocks [Count: {0}]", CoreApi.World.Blocks.Count);
#endif
				//If Brute force won't work; use GROOT FORCE!
				//var theBlock = ClientApi.World.BlockAccessor.GetBlock(0);

				if (!material.HasValue) {
					foreach (Block blk in CoreApi.World.Blocks) {
						if (blk.IsMissing || blk.Id == 0 || blk.BlockId == 0) {
							emptyCount++;
						} else if (blk.Code != null && blk.Code.BeginsWith(assetName.Domain, assetName.Path)) {
#if DEBUG
							//CoreApi.World.Logger.VerboseDebug("Block: [{0} ({1})] =  #{2}", blk.Code.Path, blk.BlockMaterial, blk.BlockId);
#endif

							arbBlockIDTable.Add(blk.BlockId, blk.Code.Path);
						}
					}
				} else {
					foreach (Block blk in CoreApi.World.Blocks) {
						if (blk.IsMissing || blk.Id == 0 || blk.BlockId == 0) {
							emptyCount++;
						} else if (blk.Code != null && material.Value == blk.BlockMaterial && blk.Code.BeginsWith(assetName.Domain, assetName.Path)) {
#if DEBUG
							//CoreApi.World.Logger.VerboseDebug("Block: [{0} ({1})] =  #{2}", blk.Code.Path, blk.BlockMaterial, blk.BlockId);
#endif

							arbBlockIDTable.Add(blk.BlockId, blk.Code.Path);
						}
					}
				}

#if DEBUG
				CoreApi.World.Logger.VerboseDebug("Block gaps: {0}", emptyCount);
#endif
			}

			return arbBlockIDTable;
		}

		public static long ToChunkIndex3D(this IBlockAccessor blocks, BlockPos blockPos)
		{
			return ToChunkIndex3D(blocks, blockPos.X / blocks.ChunkSize, blockPos.Y / blocks.ChunkSize, blockPos.Z / blocks.ChunkSize);
		}

		public static long ToChunkIndex3D(this IBlockAccessor blocks, Vec3i chunkPos)
		{
			return ToChunkIndex3D(blocks, chunkPos.X , chunkPos.Y , chunkPos.Z );
		}

		public static long ToChunkIndex3D(this IBlockAccessor blocks, int chunkX, int chunkY, int chunkZ)
		{
			int ChunkMapSizeX = blocks.MapSizeX / blocks.ChunkSize;
			int ChunkMapSizeZ = blocks.MapSizeZ / blocks.ChunkSize;

			return (( long )chunkY * ChunkMapSizeZ + chunkZ) * ChunkMapSizeX + chunkX;
		}


		/// <summary>
		/// Chunk local index. Not block position!
		/// </summary>
		/// <remarks>Clamps to 5 bit ranges automagically</remarks>
		public static int ChunkBlockIndicie16(int X_index, int Y_index, int Z_index)
		{
			return ((Y_index & 31) * 32 + (Z_index & 31)) * 32 + (X_index & 31);
		}

		/// <summary>
		/// Chunk index converted from block position (in world)
		/// </summary>
		/// <returns>The block indicie.</returns>
		/// <param name="blockPos">Block position.</param>
		/// <remarks>Clamps to 5 bit ranges automagically</remarks>
		public static int ChunkBlockIndicie16(BlockPos blockPos)
		{
			//Chunk masked
			return ((blockPos.Y & 31) * 32 + (blockPos.Z & 31)) * 32 + (blockPos.X & 31);
		}


		public static Vec3i[] ComputeChunkBubble(Vec3i center)
		{
			Vec3i[] chunkPositions = new Vec3i[]
			{
				center.AddCopy(-1,1,1),
				center.AddCopy(0,1,1),
				center.AddCopy(1,1,1),
				center.AddCopy(-1,0,1),
				center.AddCopy(0,0,1),
				center.AddCopy(1,0,1),
				center.AddCopy(-1,-1,1),
				center.AddCopy(0,-1,1),
				center.AddCopy(1,-1,1),

				center.AddCopy(-1,1,0),
		      	center.AddCopy(0,1,0),
		      	center.AddCopy(1,1,0),
				center.AddCopy(-1,0,0),
				center.AddCopy(0,0,0),
		      	center.AddCopy(1,0,0),
		      	center.AddCopy(-1,-1,0),		      	
		      	center.AddCopy(0,-1,0),
		      	center.AddCopy(1,-1,0),

				center.AddCopy(-1,1,-1),
				center.AddCopy(0,1,-1),
				center.AddCopy(1,1,-1),
				center.AddCopy(-1,0,-1),
				center.AddCopy(0,0,-1),
				center.AddCopy(1,0,-1),
				center.AddCopy(-1,-1,-1),
				center.AddCopy(0,-1,-1),
				center.AddCopy(1,-1,-1),

			};




			return chunkPositions;
		}

		public static Vec3i ToChunkPos(this IBlockAccessor blocks, BlockPos blockPos)
		{
			return new Vec3i(blockPos.X / blocks.ChunkSize, blockPos.Y / blocks.ChunkSize, blockPos.Z / blocks.ChunkSize);
		}


		public static void SetBlockPos(this ITreeAttribute source, string key, BlockPos value) 
		{
			byte[] buffer = new byte[12];

			using (MemoryStream bytesStream = new MemoryStream(buffer,true)) 
			{
				BinaryWriter byteWriter = new BinaryWriter(bytesStream);

				value.ToBytes(byteWriter);

				byteWriter.Flush( );

				source.SetBytes(key, buffer);
			}
		}

		public static BlockPos GetBlockPos(this ITreeAttribute source,string key) 
		{
			byte[] rawBytes = source.GetBytes(key);

			BlockPos positon = null;

			using (MemoryStream bytesStream = new MemoryStream(rawBytes)) {
				BinaryReader binRead = new BinaryReader(bytesStream);

				positon = BlockPos.CreateFromBytes(binRead);
			}
			return positon;
		}
	}
}

