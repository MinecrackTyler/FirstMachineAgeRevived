using System;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElementalTools
{
	public class PackCarburization_Renderer : IInFirepitRenderer //FirepitContentsRenderer
	{
		private BlockPos pos;
		private ICoreClientAPI clientAPI;
		private ItemStack localStack;
		private bool isInOutputSlot;

		private MeshRef carbpack_MeshRef;
		private Matrixf carbpack_ModelMatrix = new Matrixf( );

		private int textureId;
		private float voxelHeight;
		protected int glowLevel;

		public double RenderOrder {
			get
			{
				return 0.5; 
			}
		}

		public int RenderRange {
			get
			{
				 return 20; 
			}
		}

		public PackCarburization_Renderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
		{
		this.clientAPI = capi;
		this.localStack = stack;
		this.pos = pos;
		this.isInOutputSlot = isInOutputSlot;

		PackCarburization packBlock = clientAPI.World.GetBlock(stack.Collectible.Code) as PackCarburization;

		
		MeshData pack_MeshData;
		//path: "shapes/block/metallurgy/pack_carburization.json"
		var shapePath = packBlock.Shape.Base.CopyWithPath("shapes/" + packBlock.Shape.Base.Path + ".json");//Why append filenames, can't Shape have a type-param?!

		#if DEBUG
		capi.Logger.VerboseDebug("Shape-path: {0}", shapePath);
		#endif
		capi.Tesselator.TesselateShape(packBlock, Shape.TryGet(capi, shapePath), out pack_MeshData);
		
		carbpack_MeshRef = capi.Render.UploadMesh(pack_MeshData);							
		}


		public void Dispose( )
		{
		clientAPI.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		carbpack_MeshRef?.Dispose( );
		}

		public void OnCookingComplete( )
		{
			isInOutputSlot = true;
			//What Else??
		}


		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
		IRenderAPI renderAPI = clientAPI.Render;
		Vec3d camPos = clientAPI.World.Player.Entity.CameraPos;

		renderAPI.GlDisableCullFace( );
		renderAPI.GlToggleBlend(true);

		IStandardShaderProgram shader = renderAPI.PreparedStandardShader(pos.X, pos.Y, pos.Z);

		shader.Tex2D = clientAPI.BlockTextureAtlas.AtlasTextureIds[0];
		shader.DontWarpVertices = 0;
		shader.AddRenderFlags = 0;
		shader.RgbaAmbientIn = renderAPI.AmbientColor;
		shader.RgbaFogIn = renderAPI.FogColor;
		shader.FogMinIn = renderAPI.FogMin;
		shader.FogDensityIn = renderAPI.FogDensity;
		shader.RgbaTint = ColorUtil.WhiteArgbVec;
		shader.NormalShaded = 1;
		shader.ExtraGodray = 0;
		shader.ExtraGlow = glowLevel;
		shader.SsaoAttn = 0;
		shader.AlphaTest = 0.05f;
		shader.OverlayOpacity = 0;

		//TODO: Change constants to work for Carburization pack sizes / offsets...
		shader.ModelMatrix = carbpack_ModelMatrix
			.Identity( )
			.Translate(pos.X - camPos.X + 0.001f, pos.Y - camPos.Y, pos.Z - camPos.Z - 0.001f)
			.Translate(0f, 1 / 16f, 0f)
			.Values;

		shader.ViewMatrix = renderAPI.CameraMatrixOriginf;
		shader.ProjectionMatrix = renderAPI.CurrentProjectionMatrix;

		renderAPI.RenderMesh(carbpack_MeshRef);

		shader.Stop( );
		}


		public void OnUpdate(float temperature)
		{
		//Correct GLOW INCANDESCENT level?
		this.glowLevel = ( int )(temperature / 100);
		}
	}
}

