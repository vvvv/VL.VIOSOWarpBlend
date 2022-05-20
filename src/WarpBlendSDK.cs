using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using VIOSOWarpBlend;
using Stride.Core.Mathematics;
using Stride.Graphics;
using VL.Core;
using VL.Stride;
using Stride.Engine;
using System.IO;

namespace VL.VIOSOWarpBlend
{
    public class WarperSet
    {
        public Warper _warper;
        public Vector4 _size;
        public Texture _texWarp;
        public Texture _texBlend;
        public Texture _texBlack;
        public Vector4 _bBorder;
        public Vector4 _blackBias;
        public Quaternion _orig_rot;
        public Vector3 _orig_pos;
        public Matrix _ppMatrix;
        public WarperSet(Warper warper, Vector4 size, Texture texWarp, Texture texBlend, Texture texBlack, Vector4 bBorder, Vector4 blackBias, Quaternion orig_rot, Vector3 orig_pos)
        {
            _warper = warper;
            _size = size;
            _texWarp = texWarp;
            _texBlend = texBlend;
            _texBlack = texBlack;
            _bBorder = bBorder;
            _blackBias = blackBias;
            _orig_rot = orig_rot;
            _orig_pos = orig_pos;
            _ppMatrix = Matrix.Identity;
        }
    };

    public class WarpBlendSDK: IDisposable
    {
        public WarperSet WarperSet { get; private set; }
        public Matrix ViewMatrix { get; private set; }
        public Matrix ProjectionMatrix { get; private set; }
        public Int2 Resolution { get; private set; }
        public Vector4 OffScale { get; private set; }

        public WarpBlendSDK()
        {
            
        }
        public void LoadFile(string iniFilename, string channelName)
        {
            var gameHandle = ServiceRegistry.Current.GetGameHandle();
            var game = gameHandle.Resource;
            var graphicsDevice = game.GraphicsDevice;
            var commandList = gameHandle.Resource.GraphicsContext.CommandList;

            //Debug.Log("VIOSOWarpBlendPP.Render(), new camera " + cam.name);
            try
            {
                // create a new warper with the name of the camera
                var w = new Warper(Warper.DummyDevice, iniFilename, channelName);

                // adjust values read from ini
                Warper.VWB_Warper ini = w.Get();

                if (!Path.IsPathRooted(ini.calibFile))
                    ini.calibFile = Path.Combine(Path.GetDirectoryName(iniFilename), ini.calibFile);
                // update
                w.Set(ref ini);

                // do init
                Warper.ERROR err = w.Init();
                if (Warper.ERROR.NONE != err)
                    throw new ArgumentException("Could not initialize Warper. Err:" + err.ToString());

                if (ini.bPartialInput)
                {
                    OffScale = new Vector4(ini.optimalRect.left / ini.optimalRes.cx,
                                           ini.optimalRect.top / ini.optimalRes.cy,
                                           (ini.optimalRect.right - ini.optimalRect.left) / ini.optimalRes.cx,
                                           (ini.optimalRect.bottom - ini.optimalRect.top) / ini.optimalRes.cy);
                }
                else
                {
                    OffScale = new Vector4(0, 0, 1, 1);
                }

                // get warp file header, to have flags and sizes handy
                Warper.WarpFileHeader4 header;
                w.GetWarpBlendHeader(out header);

                Resolution = new Int2((int)header.width, (int)header.height);

                // access warp map in dll's memory
                IntPtr warpmap;
                w.GetWarpMap(out warpmap);
                // the raw data are a 2D texture RGBA32F, get size from header.width and .height
                // warpmap must be present, no need to test
                // to access each pixel's data use
                Texture warpTex = null;
                if (IntPtr.Zero != warpmap)
                {
                    // marshal IntPtr to byte[]

                    // create a texture according to data and fill it
                    // NOTE this can only handle textures up to 2GB in size
                    //warpTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBAFloat, false);
                    warpTex = Texture.New2D(graphicsDevice, (int)header.width, (int)header.height, PixelFormat.R32G32B32A32_Float);
                    var szMap = Marshal.SizeOf(typeof(Warper.WARPRECORD)) * (int)header.width * (int)header.height;
                    var dp = new DataPointer(warpmap, szMap);
                    warpTex.SetData(commandList, dp);
                    //warpTex.LoadRawTextureData(warpmap, szMap);
                    //warpTex.Apply();
                }

                IntPtr blendmap;
                w.GetBlendMap(out blendmap);
                // the raw data are 2D texture, depending on header.flags:
                //   if BLENDV3 & header.flags RGBA32F
                //   if BLENDV2 & header.flags RGBA16U
                //   else RGBA8U
                Texture blendTex = null;
                if (IntPtr.Zero != blendmap)
                {
                    DataPointer dp;
                    int szMap;
                    if ((header.flags & (uint)Warper.FLAGS.BLENDV3) != 0)
                    {
                        //blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBAFloat, false);
                        blendTex = Texture.New2D(graphicsDevice, (int)header.width, (int)header.height, PixelFormat.R32G32B32A32_Float);
                        szMap = Marshal.SizeOf(typeof(Warper.BLENDRECORD3)) * (int)header.width * (int)header.height;
                        //blendTex.LoadRawTextureData(blendmap, );
                        //blendTex.Apply();
                    }
                    else if ((header.flags & (uint)Warper.FLAGS.BLENDV2) != 0)
                    {
                        //blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA64, false);
                        blendTex = Texture.New2D(graphicsDevice, (int)header.width, (int)header.height, PixelFormat.R16G16B16A16_UNorm);
                        szMap = Marshal.SizeOf(typeof(Warper.BLENDRECORD2)) * (int)header.width * (int)header.height;
                        //blendTex.LoadRawTextureData(blendmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD2)) * (int)header.width * (int)header.height);
                        //blendTex.Apply();
                    }
                    else
                    {
                        //blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, false);
                        blendTex = Texture.New2D(graphicsDevice, (int)header.width, (int)header.height, PixelFormat.R8G8B8A8_UNorm);
                        szMap = Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height;
                        //blendTex.LoadRawTextureData(blendmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height);
                        //blendTex.Apply();
                    }
                    dp = new DataPointer(blendmap, szMap);
                    blendTex.SetData(commandList, dp);
                }

                IntPtr blackmap;
                w.GetBlackMap(out blackmap);
                // the raw data are a 2D texture RGBA8U, get size from header.width and .height
                // NOTE: the black map is scaled
                // black = samBlack.sample( tex ) * header.blackScale;
                // out+= header.blackDark * black;
                // out*= float4(1, 1, 1, 1) - header.blackDark * header.blackBright * black; // scale down to avoid clipping } vOut
                // out = max(out, black); // do lower clamp to stay above common black, upper is done anyways
                // to access each pixel's data use
                Texture blackTex = null;
                if (IntPtr.Zero != blackmap)
                {
                    //blackTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, false);
                    blackTex = Texture.New2D(graphicsDevice, (int)header.width, (int)header.height, PixelFormat.R8G8B8A8_UNorm);
                    var szMap = Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height;
                    var dp = new DataPointer(blackmap, szMap);
                    blackTex.SetData(commandList, dp);
                    //blackTex.LoadRawTextureData(blackmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height);
                    //blackTex.Apply();
                }

                // white is not existing atm.
                //IntPtr whitemap;
                //w.GetWhiteMap(out whitemap);

                Vector4 bo = new Vector4(
                    0 != (header.flags & (uint)Warper.FLAGS.BORDER) ? 1 : 0,
                    ini.bDoNotBlend ? 0 : 1,
                    0 != (header.flags & (uint)Warper.FLAGS.IS3D) ? 1 : 0,
                    ini.bBicubic ? 1 : 0);

                Vector4 bb = new Vector4(header.blackScale, header.blackDark, header.blackDark * header.blackBright, ini.bDoNoBlack ? 0 : 1);

                WarperSet = new WarperSet( 
                    w, 
                    new Vector4(header.width, header.height, 1.0f / header.width, 1.0f / header.height),
                    warpTex, blendTex, blackTex,
                    bo, bb, Quaternion.Identity, new Vector3());

                //Debug.Log("VIOSOHDRPCamera.Start(), camera " + cam.name + " successfully initialized.");
            }
            catch (Exception ex)
            {
                //Debug.LogError("VIOSOHDRPCamera.Start(" + cam.name + ") " + ex.ToString());
                throw ex;
            }
        }

        public void Update(Vector3 eye)
        {
            if (WarperSet != null)
            {
                Warper.VEC3 pos = new Warper.VEC3(eye.X, eye.Y, eye.Z);
                Warper.VEC3 rot = new Warper.VEC3(0, 0, 0);
                Warper.MAT4X4 mVV = new Warper.MAT4X4(
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
                Warper.MAT4X4 mVVP = new Warper.MAT4X4(
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
                Warper.CLIP clip = new Warper.CLIP();
                Matrix mV = Matrix.Identity;
                Matrix mP = Matrix.Identity;
                Matrix mVP = Matrix.Identity;
                if (Warper.ERROR.NONE == WarperSet._warper.GetViewClip(ref pos, ref rot, ref mVV, ref clip) &&
                     Warper.ERROR.NONE == WarperSet._warper.GetShaderVPMatrix(ref mVVP))
                {
                    mV.M11 = mVV._11;
                    mV.M12 = mVV._12;
                    mV.M13 = mVV._13;
                    mV.M14 = mVV._14;
                    mV.M21 = mVV._21;
                    mV.M22 = mVV._22;
                    mV.M23 = mVV._23;
                    mV.M24 = mVV._24;
                    mV.M31 = mVV._31;
                    mV.M32 = mVV._32;
                    mV.M33 = mVV._33;
                    mV.M34 = mVV._34;
                    mV.M41 = mVV._41;
                    mV.M42 = mVV._42;
                    mV.M43 = mVV._43;
                    mV.M44 = mVV._44;

                    mVP.M11 = mVVP._11;
                    mVP.M12 = mVVP._12;
                    mVP.M13 = mVVP._13;
                    mVP.M14 = mVVP._14;
                    mVP.M21 = mVVP._21;
                    mVP.M22 = mVVP._22;
                    mVP.M23 = mVVP._23;
                    mVP.M24 = mVVP._24;
                    mVP.M31 = mVVP._31;
                    mVP.M32 = mVVP._32;
                    mVP.M33 = mVVP._33;
                    mVP.M34 = mVVP._34;
                    mVP.M41 = mVVP._41;
                    mVP.M42 = mVVP._42;
                    mVP.M43 = mVVP._43;
                    mVP.M44 = mVVP._44;
                
                    mVP.Transpose();

                    WarperSet._ppMatrix = mVP; // update matrix for post process call later

                    // manipulate actual camera settings
                    ////Vector3 p = mV.GetColumn(3);
                    mV.Transpose();
                    ////Quaternion q = mV.rotation;
                    mV.Decompose(out var scale, out Quaternion rotation, out var translation);
                    //Quaternion q = Quaternion.Inverse(mV.rotation);
                    var q = Quaternion.Invert(rotation);
                    //cam.transform.localRotation = s._orig_rot * q;
                    var localRot = WarperSet._orig_rot * q;
                    //Vector3 p = mV.GetColumn(3);
                    //cam.transform.localPosition = s._orig_pos + p;
                    var localPos = WarperSet._orig_pos + translation;
                    var m = new Matrix();
                    m.TranslationVector = localPos;
                    var qm = Matrix.RotationQuaternion(localRot);
                    ViewMatrix = m * qm;
                    
                    //FrustumPlanes pl = new FrustumPlanes();
                    //pl.left = -clip.l;
                    //pl.right = clip.r;
                    //pl.bottom = -clip.b; // NOTE: maybe swap top and bottom, because of flipped textures
                    //pl.top = clip.t;
                    //pl.zNear = clip.n;
                    //pl.zFar = clip.f;
                    //mP = Matrix4x4.Frustum(pl);
                    Matrix mp;
                    PerspectiveOffCenterDistanceRH(-clip.l, clip.r, -clip.b, clip.t, clip.n, clip.f, 1, out mp);
                    ProjectionMatrix = mp;
                }
            }
        }

        private static void PerspectiveOffCenterDistanceRH(float left, float right, float bottom, float top, float znear, float zfar, float zdist, out Matrix result)
        {
            float zRange = zfar / (znear - zfar);
            float rml = right - left;
            float tmb = top - bottom;

            result = new Matrix();
            result.M11 = 2.0f * zdist / rml;
            result.M22 = 2.0f * zdist / tmb;
            result.M31 = (left + right) / rml;
            result.M32 = (top + bottom) / tmb;
            result.M33 = zRange;
            result.M34 = -1.0f;
            result.M43 = znear * zRange;
        }

        public void Dispose()
        {
            WarperSet._texWarp.Dispose();
            WarperSet._texBlack.Dispose();
            WarperSet._texBlack.Dispose();
            WarperSet._warper = null;
        }
    }
}