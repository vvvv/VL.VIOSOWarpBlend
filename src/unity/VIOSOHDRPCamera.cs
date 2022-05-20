using System.Collections;
using UnityEngine;
using VIOSOWarpBlend;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Rendering;

public class VIOSOHDRPCamera : MonoBehaviour
{
    public class WarperSet
    {
        public Warper _warper;
        public Vector4 _size;
        public Texture2D _texWarp;
        public Texture2D _texBlend;
        public Texture2D _texBlack;
        public Vector4 _bBorder;
        public Vector4 _blackBias;
        public Quaternion _orig_rot;
        public Vector3 _orig_pos;
        public Matrix4x4 _ppMatrix;
        public WarperSet(Warper warper, Vector4 size, Texture2D texWarp, Texture2D texBlend, Texture2D texBlack, Vector4 bBorder, Vector4 blackBias, Quaternion orig_rot, Vector3 orig_pos)
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
            _ppMatrix = Matrix4x4.identity;
        }
    };

    public static Dictionary<string, WarperSet> _warperDict;

    static VIOSOHDRPCamera()
    {
        _warperDict = new Dictionary<string, WarperSet>();
    }

    public void MyUpdate( ref Camera cam, ref WarperSet s )
    {
        if (s != null)
        {
            Warper.VEC3 pos = new Warper.VEC3(0, 0, 0);
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
            Matrix4x4 mV = Matrix4x4.identity;
            Matrix4x4 mP = Matrix4x4.identity;
            Matrix4x4 mVP = Matrix4x4.identity;
            if (Warper.ERROR.NONE == s._warper.GetViewClip(ref pos, ref rot, ref mVV, ref clip) &&
                 Warper.ERROR.NONE == s._warper.GetShaderVPMatrix(ref mVVP))
            {
                mV.m00 = mVV._11;
                mV.m01 = mVV._12;
                mV.m02 = mVV._13;
                mV.m03 = mVV._14;
                mV.m10 = mVV._21;
                mV.m11 = mVV._22;
                mV.m12 = mVV._23;
                mV.m13 = mVV._24;
                mV.m20 = mVV._31;
                mV.m21 = mVV._32;
                mV.m22 = mVV._33;
                mV.m23 = mVV._34;
                mV.m30 = mVV._41;
                mV.m31 = mVV._42;
                mV.m32 = mVV._43;
                mV.m33 = mVV._44;

                mVP.m00 = mVVP._11;
                mVP.m01 = mVVP._12;
                mVP.m02 = mVVP._13;
                mVP.m03 = mVVP._14;
                mVP.m10 = mVVP._21;
                mVP.m11 = mVVP._22;
                mVP.m12 = mVVP._23;
                mVP.m13 = mVVP._24;
                mVP.m20 = mVVP._31;
                mVP.m21 = mVVP._32;
                mVP.m22 = mVVP._33;
                mVP.m23 = mVVP._34;
                mVP.m30 = mVVP._41;
                mVP.m31 = mVVP._42;
                mVP.m32 = mVVP._43;
                mVP.m33 = mVVP._44;

                s._ppMatrix = mVP.transpose; // update matrix for post process call later

                // manipulate actual camera settings
                //Vector3 p = mV.GetColumn(3);
                mV = mV.transpose;
                //Quaternion q = mV.rotation;
               Quaternion q = Quaternion.Inverse(mV.rotation);
                Vector3 p = mV.GetColumn(3);
                cam.transform.localRotation = s._orig_rot * q;
                cam.transform.localPosition = s._orig_pos + p;

                FrustumPlanes pl = new FrustumPlanes();
                pl.left = -clip.l;
                pl.right = clip.r;
                pl.bottom = -clip.b; // NOTE: maybe swap top and bottom, because of flipped textures
                pl.top = clip.t;
                pl.zNear = clip.n;
                pl.zFar = clip.f;
                mP = Matrix4x4.Frustum(pl);
                cam.projectionMatrix = mP;
            }
        }
    }

    private void Start()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }
    private void OnEnable()
    {
        Camera cam = GetComponent<Camera>();
        WarperSet s;
        if (!_warperDict.TryGetValue(cam.name, out s))
        {
            //Debug.Log("VIOSOWarpBlendPP.Render(), new camera " + cam.name);
            try
            {
                // create a new warper with the name of the camera
                Warper w = new Warper(Warper.DummyDevice, "VIOSOWarpBlend.ini", cam.name);

                // adjust values read from ini
                Warper.VWB_Warper ini = w.Get();

                // do init
                Warper.ERROR err = w.Init();
                if (Warper.ERROR.NONE != err)
                    throw new ArgumentException("Could not initialize Warper. Err:" + err.ToString());

                // get warp file header, to have flags and sizes handy
                Warper.WarpFileHeader4 header;
                w.GetWarpBlendHeader(out header);

                // access warp map in dll's memory
                IntPtr warpmap;
                w.GetWarpMap(out warpmap);
                // the raw data are a 2D texture RGBA32F, get size from header.width and .height
                // warpmap must be present, no need to test
                // to access each pixel's data use
                Texture2D warpTex = null;
                if (IntPtr.Zero != warpmap)
                {
                    // marshal IntPtr to byte[]
                    int szMap = Marshal.SizeOf(typeof(Warper.WARPRECORD)) * (int)header.width * (int)header.height;

                    // create a texture according to data and fill it
                    // NOTE this can only handle textures up to 2GB in size
                    warpTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBAFloat, false);
                    warpTex.LoadRawTextureData(warpmap, szMap);
                    warpTex.Apply();
                }

                IntPtr blendmap;
                w.GetBlendMap(out blendmap);
                // the raw data are 2D texture, depending on header.flags:
                //   if BLENDV3 & header.flags RGBA32F
                //   if BLENDV2 & header.flags RGBA16U
                //   else RGBA8U
                Texture2D blendTex = null;
                if (IntPtr.Zero != blendmap)
                {
                    if ((header.flags & (uint)Warper.FLAGS.BLENDV3) != 0)
                    {
                        blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBAFloat, false);
                        blendTex.LoadRawTextureData(blendmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD3)) * (int)header.width * (int)header.height);
                        blendTex.Apply();
                    }
                    else if ((header.flags & (uint)Warper.FLAGS.BLENDV2) != 0)
                    {
                        blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA64, false);
                        blendTex.LoadRawTextureData(blendmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD2)) * (int)header.width * (int)header.height);
                        blendTex.Apply();
                    }
                    else
                    {
                        blendTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, false);
                        blendTex.LoadRawTextureData(blendmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height);
                        blendTex.Apply();
                    }
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
                Texture2D blackTex = null;
                if (IntPtr.Zero != blackmap)
                {
                    blackTex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, false);
                    blackTex.LoadRawTextureData(blackmap, Marshal.SizeOf(typeof(Warper.BLENDRECORD)) * (int)header.width * (int)header.height);
                    blackTex.Apply();
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

                s = new WarperSet( 
                    w, 
                    new Vector4(header.width, header.height, 1.0f / header.width, 1.0f / header.height),
                    warpTex, blendTex, blackTex,
                    bo, bb, cam.transform.localRotation, cam.transform.localPosition );
                _warperDict.Add(cam.name, s);

                Debug.Log("VIOSOHDRPCamera.Start(), camera " + cam.name + " successfully initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError("VIOSOHDRPCamera.Start(" + cam.name + ") " + ex.ToString());
                throw ex;
            }

        }
        else
        {
            Debug.Log(string.Format("Camera already initialized! Please make sure to name all VIOSO enabled cameras differently"));
        }
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        Camera cam = GetComponent<Camera>();
        WarperSet s;
        if (_warperDict.TryGetValue(cam.name, out s))
            _warperDict.Remove(cam.name);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        Camera cam = GetComponent<Camera>();
        WarperSet s;
        _warperDict.TryGetValue(camera.name, out s);
        MyUpdate(ref camera, ref s);
        
    }
}
