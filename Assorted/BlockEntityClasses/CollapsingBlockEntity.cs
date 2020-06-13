using System;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace FirstMachineAge
{
	public class CollapsingBlockEntity : BlockEntity
	{

		public override void Initialize(ICoreAPI api)
		{
		base.Initialize(api);
		//origlightHsv = Block.LightHsv;
		//lightHsv = ( byte[ ] )Block.LightHsv.Clone( );
		}

		public void UponPlacement(string camo )
		{
		this.Camo = camo;
		
		}

		public string Camo { get; protected set;}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
		{
		//Mirror whatever tooltip for this material normally?
		//sb.AppendLine(Lang.Get("{0} with {1} lining and {2} glass panels", material.UcFirst( ), lining.UcFirst( ), glass));
		}

		public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
		base.FromTreeAtributes(tree, worldForResolving);

		this.Camo = tree.GetString(CollapsingBlock.CamoKey, "dirt");
		
		if (Api != null && Api.Side == EnumAppSide.Client) 
		{ MarkDirty(true);}

		}


		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		base.ToTreeAttributes(tree);

		tree.SetString(CollapsingBlock.CamoKey, this.Camo);		
		}

		/*
		private MeshData getMesh(ITesselatorAPI tesselator)
		{
		Dictionary<string, MeshData> lanternMeshes = ObjectCacheUtil.GetOrCreate(Api, "blockLanternBlockMeshes", ( ) => new Dictionary<string, MeshData>( ));

		MeshData mesh = null;
		BlockLantern block = Api.World.BlockAccessor.GetBlock(Pos) as BlockLantern;
		if (block == null) return null;

		string orient = block.LastCodePart( );

		if (lanternMeshes.TryGetValue(material + "-" + lining + "-" + orient + "-" + glass, out mesh)) {
		return mesh;
		}

		return lanternMeshes[material + "-" + lining + "-" + orient + "-" + glass] = block.GenMesh(Api as ICoreClientAPI, material, lining, glass, null, tesselator);
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
		{
		MeshData mesh = getMesh(tesselator);

		if (mesh == null) return false;

		string part = Block.LastCodePart( );
		if (part == "up" || part == "down") {
		mesh = mesh.Clone( ).Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0);
		}

		mesher.AddMeshData(mesh);

		return true;
		}
		*/
	}
}

