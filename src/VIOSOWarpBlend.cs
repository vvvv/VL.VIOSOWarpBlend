using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

// use with VIOSOWarpBlend 1.6.9 or newer

// make sure to set x64 as build target
namespace VIOSOWarpBlend
{
    public class Warper
    {

        public const int MAX_PATH = 260;


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VEC2
        {
            public float u, v;
            public VEC2(float _u, float _v)
            {
                u = _u;
                v = _v;
            }
        };

        public struct VEC3
        {
            public float x, y, z;
            public VEC3(float _x, float _y, float _z)
            {
                x = _x;
                y = _y;
                z = _z;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VEC4
        {
            public float x, y, z, w;
            public VEC4(float _x, float _y, float _z, float _w)
            {
                x = _x;
                y = _y;
                z = _z;
                w = _w;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct CLIP
        {
            public float l, t, r, b, n, f; // bigger fov is positive! left, top, right, bottom, near, far
            public CLIP(float _l, float _t, float _r, float _b, float _n, float _f)
            {
                l = _l;
                t = _t;
                r = _r;
                b = _b;
                n = _n;
                f = _f;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MAT4X4
        {
            public float _11, _12, _13, _14;
            public float _21, _22, _23, _24;
            public float _31, _32, _33, _34;
            public float _41, _42, _43, _44;

            public MAT4X4(
                float v11, float v12, float v13, float v14,
                float v21, float v22, float v23, float v24,
                float v31, float v32, float v33, float v34,
                float v41, float v42, float v43, float v44
                )
            {
                _11 = v11; _12 = v12; _13 = v13; _14 = v14;
                _21 = v21; _22 = v22; _23 = v23; _24 = v24;
                _31 = v31; _32 = v32; _33 = v33; _34 = v34;
                _41 = v41; _42 = v42; _43 = v43; _44 = v44;
            }
        };

        public struct SIZE
        {
            public Int32 cx, cy;
            SIZE(Int32 vcx, Int32 vcy)
            {
                cx = vcx;
                cy = vcy;
            }
        };

        public struct RECT
        {
            public Int32 left, top, right, bottom;
            RECT(Int32 l, Int32 t, Int32 r, Int32 b)
            {
                left = l;
                top = t;
                right = r;
                bottom = b;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        public struct VWB_Warper
        {
            /// this is where this ini file is, this defaults to ""
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String path;

            /// this is the channel name, it defaults to "channel 1"
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String channel;

            /// this is the path to the warp map, a .vwf file, it defaults to "vioso.vwf"
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12 * MAX_PATH)]
            public String calibFile;

            /// the calibration index in mapping file, defaults to 0,
            /// you also might set this to negated display number, to search for a certain display:
            public Int32 calibIndex;

            /// set to true to make the world turn and move with view direction and eye position, this is the case if the viewer gets
            /// moved by a motion platform, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bTurnWithView;

            /// set to true to render without blending enabled, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bDoNotBlend;

            /// a path to a external dynamic library to provide the eye parameters, see EyePointProvider.h
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String eyeProvider;

            /// a parameter string, to initialize the eye-point provider
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32768)]
            public String eyeProviderParam;

            /// eye to pivot position coordinate, usually {0,0,0}, defaults to {0,0,0}
            public VEC3 eye;

            /// the near plane distance
            /// NOTE: the corresponding .ini-value is "near", defaults to 0.1
            public float nearDist;

            /// the far plane distance, note: these values are used to create the projection matrix
            /// farDist/nearDist should be as small as possible, to have a good z-buffer resolution
            /// defaults to 200.0
            /// NOTE: the corresponding .ini-value is "far"
            public float farDist;

            /// set to true to enable bicubic sampling from source texture, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bBicubic;

            /// swivel and negate eye parameters, defaults to 0
            /// bitfield:	0x00000001 change sign of pitch, 0x00000002 use input yaw as pitch, 0x00000004 use input roll as pitch
            ///				0x00000010 change sign of yaw, 0x00000020 use input pitch as yaw, 0x00000040 use input roll as yaw
            ///				0x00000100 change sign of roll, 0x00000200 use input pitch as roll, 0x00000400 use input yaw as roll
            ///				0x00010000 change sign of x movement, 0x00020000 use input y as x, 0x00040000 use input z as x
            ///				0x00100000 change sign of y movement, 0x00200000 use input x as y, 0x00400000 use input z as y
            ///				0x01000000 change sign of z movement, 0x02000000 use input x as z, 0x04000000 use input y as z
            public Int32 splice;

            /// the transformation matrix to go from VIOSO coordinates to IG coordinates, defaults to indentity
            /// note VIOSO maps are always right-handed, to use with a left-handed world like DirectX, invert the z!
            public MAT4X4 trans;

            /// set a gamma correction value. This is only useful, if you changed the projector's gamma setting after calibration,
            /// as the gamma is already calculated inside blend map, or to fine-tune, defaults to 1 (no change)
            public float gamma;

            /// set a moving range. This applies only for 3D mappings and dynamic eye point.
            /// This is a factor applied to the projector mapped MIN(width,height)/2
            /// The view plane is widened to cope with a movement to all sides, defaults to 1
            /// Check borderFit in log: 1 means all points stay on unwidened viewplane, 2 means, we had to double it.
            public float autoViewC;

            /// set to true to calculate view parameters while creating warper, defaults to false
            /// All further values are calculated/overwritten, if bAutoView is set.
            [MarshalAs(UnmanagedType.I1)]
            public bool bAutoView;

            /// [0] = x = pitch, [1] = y = yaw, [2] = z = roll, rotation order is yaw first, then pitch, last roll, defaults to {0,0,0}
            /// positive yaw turns right, positive pitch turns up and positive roll turns clockwise
            public VEC3 dir;

            /// the fields of view in degree, [0] = left, [1] = top, [2] = right, [3] = bottom, defaults to {35,30,35,30}
            public VEC4 fov;

            /// the screen distance this is where the render plane is, defaults to 1
            public float screenDist;

            /// set to some value, this is returned by VWB_GetOptimalRes, defaults to {0,0}, set by VWB_AutoView to have a projector pixel to content pixel ratio of 1
            public SIZE optimalRes;

            /// set to some value, this is returned by VWB_GetOptimalRes, defaults to {0,0,0,0}, set by VWB_AutoView to indicate, only a parttial rect of the whole image is needed
            public RECT optimalRect;

            // set TCP port the plugin is listening to, default is 0 which means network support is switched off
            public UInt16 port;

            /// set to IPv4 address to listen on a specific one, defaults to "0.0.0.0", which means we listen to all local IPs
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String addr;

            /// set to true to use OpenGL shader version 1.1 with fixed pipeline, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bUseGL110;

            /// set to true if your input texture is only the optimal rect part, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bPartialInput;

            /// Bitfield; only valid in Windows build
            /// 1 rendering of mouse cursor
            /// 2 disable system cursor over window 
            /// defaults to 0
            public Int32 mouseMode;

            /// set to true to flip directX 11 texture v, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bFlipDXVs;

            /// set to true to disable black level offset, defaults to false
            [MarshalAs(UnmanagedType.I1)]
            public bool bDoNoBlack;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WARPRECORD
        {
            public float x, y, z, w;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDRECORD
        {
            public byte r, g, b, a;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct BLENDRECORD2
        {
            public UInt16 r, g, b, a;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct BLENDRECORD3
        {
            public float r, g, b, a;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        public struct WarpFileHeader5
        {
            public UInt32 magicNumber;                            ///<   "vwf0"
            public UInt32 szHdr;                                 ///<   used to communicate the size of this header struct
            public UInt32 flags;                                 ///<   additional informations
                                                                 ///<  @see FLAGS for details
            public UInt32 hMonitor;                              ///<   set to the HMONITOR of the treated display
            public UInt32 size;                                  ///<   actual size of the following data block; the size of the raw data can be calculated from dimensions
            public UInt32 width;                                  ///<   count of warp records per row
            public UInt32 height;                                 ///<   count of rows of warp records
            public VEC4 white;                             ///<   white point of that projector; set to { 1.0f, 1.0f, 1.0f, 1.0f }
            public VEC4 black;                             ///<   black point of that projector; set to { 0.0f, 0.0f, 0.0f, 1.0f }
            public float splitRowIndex;                            ///<   [ 0] => row index
            public float splitColumnIndex;                     ///<   [ 1] => column index
            public float splitRows;                                ///<   [ 2] => number of rows
            public float splitColumns;                         ///<   [ 3] => number of columns
            public float splitTotalWidth;                      ///<   [ 4] => original display width
            public float splitTotalHeight;                     ///<   [ 5] => original display height
            public float typeCalib;                                ///<   [ 6] => type to define the calibration type the information based on
            public float offsetX;                              ///<   [ 7] => original desktop display offset x
            public float offsetY;                              ///<   [ 8] => original desktop display offset y
            public float blackScale;                               ///<   [ 9] => blacklevel correction texture scale factor
            public float blackDark;                                ///<   [10] => blacklevel dark value maintain factor; 
            public float blackBright;                          ///<   [11] => blacklevel bright value maintain factor
            public float compoundID;                               ///<   [12] => ///<   identifier for a compound display, static cast to int, set if greater than 0, all screend/displays with same compound id c bilupshould use same content space alas source rect
            public VEC3 vReserved;                         ///<	reserved for future use
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String name;                             ///<   optional, human readable name for that mapping
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4096)]
            public String ident;                           ///<   optional, xml identification for that mapping derived from pdi code from pictureall, only filled if nvapi is appliable
            public UInt64 tmIdent;                                ///<   used to communicate a time stamp to identify the warp information
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public float[] vCntDispPx;                            ///<   optional, used to store informations about the content position on handled display
                                                                  ///<   [0] => minimum covered display pixel column  (l)
                                                                  ///<   [1] => minimum covered display pixel row     (t)
                                                                  ///<   [2] => maximum covered display pixel column  (r)
                                                                  ///<   [3] => maximum covered display pixel row     (b)
                                                                  ///<   [4] => content to display pixel ratio in x direction
                                                                  ///<   [5] => content to display pixel ratio in y direction
                                                                  ///<   [6] => quantum of used display pixel
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public float[] vPartialCnt;
            ///<   [0] => minimum relative content position in x direction (l)
            ///<   [1] => minimum relative content position in y direction (t)
            ///<   [2] => maximum relative content position in x direction (r)
            ///<   [3] => maximum relative content position in y direction (b)
            ///<   [4] => optional aspect ratio of the content space
            ///<   [5] => optional relative content position transform offset in x direction
            ///<   [6] => optional relative content position transform offset in y direction
            ///<   [7] => optional relative content position transform scale in x direction
            ///<   [8] => optional relative content position transform scale in y direction
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String primName;     ///<   optional, human readable name of the compound or super compound
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] vReserved2;                           ///<   used to define additional informations in further versions
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] displayID;   ///<   Windows display identifier, use EnumDisplayDevices using EDD_GET_DEVICE_INTERFACE_NAME flag to find it
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String hostname; ///<   network name or IP in dotted decimal
            public UInt32 szKey;                                 ///<   the key size, 0 if not used
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] key;                              ///<   some PGP encrypted key
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public String keyIdent;                          ///<   describes the used encryption method
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String keyDesc;                          ///<   human readable description of used         
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        public struct WarpBlendHeader
        {
            public WarpFileHeader5 header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String path;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        public struct Vertex
        {
            public float x; // position
            public float y;
            public float z;
            public float u; // texcoord
            public float v;
            public float r; // blend
            public float g;
            public float b;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        struct MarshalMesh
        {
            public UInt32 nVtx;
            public IntPtr vtx;
            public UInt32 nIdx;
            public IntPtr idx;
            public SIZE dim;
        }

        public struct Mesh
        {
            public Vertex[] vtx; // the vertex list
            public UInt32[] idx; // the index list
            public SIZE res; // the display resolution
        }

        public enum ERROR
        {
            NONE = 0,         /// No error, we succeeded
            GENERIC = -1,     /// a generic error, this might be anything, check log file
            PARAMETER = -2,   /// a parameter error, provided parameter are missing or inappropriate
            INI_LOAD = -3,    /// ini could notbe loaded
            BLEND = -4,       /// blend invalid or coud not be loaded to graphic hardware, check log file
            WARP = -5,        /// warp invalid or could not be loaded to graphic hardware, check log file
            SHADER = -6,      /// shader program failed to load, usually because of not supported hardware, check log file
            VWF_LOAD = -7,    /// mappings file broken or version mismatch
            VWF_FILE_NOT_FOUND = -8, /// cannot find mapping file
            NOT_IMPLEMENTED = -9,     /// Not implemented, this function is yet to come
            NETWORK = -10,        /// Network could not be initialized
            FALSE = -16,		/// No error, but nothing has been done
        };
        public enum FLAGS
        {
            NONE = 0x0,                                        ///<   no flags set
            DATA_ZIP = 0x1,                                    ///<   zip compressed data
            DISPLAY_SPLIT = 0x2,                               ///<   data describes a splited display, reserved[0..5] contains splits position ( iRow, iCol, qRow, qCol, org. Display width, org. Display height)
            CALIBRATION_BASE_TYP = 0x4,                        ///<   the calibration typ is specified, reserved[6] @see ESPCalibrationBaseTyp for details
            BORDER = 0x8,                                      ///<   border was set; the content is boxed by 1%
            OFFSET = 0x10,                                 ///<   desktop monitor offset is valid in reserved[7] reserved[8]
            BLACKLEVEL_CORR = 0x20,                            ///<   the blacklevel correction values are set
            IS3D = 0x40,                                     ///<   the warp definition contains 3D points instead of uv-mapping
            DISPLAYID = 0x80,                                  ///<   the displayID of that screen is valid
            BLENDV2 = 0x100,                                   ///<   we are using VWB_BlendRecord2
            BLENDV3 = 0x200,                                   ///<   we are using VWB_BlendRecord3
            ALL =                                          ///<   all available flags
                OFFSET |
                BORDER |
                DATA_ZIP |
                DISPLAY_SPLIT |
                CALIBRATION_BASE_TYP |
                BLACKLEVEL_CORR |
                IS3D |
                DISPLAYID |
                BLENDV2 |
                BLENDV3
        };

        public static IntPtr DummyDevice;
        public static IntPtr cryptoKeyMem;

        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_CreateA", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_Create(IntPtr dxDevice, [MarshalAs(UnmanagedType.LPStr)] String szCnfigFile, [MarshalAs(UnmanagedType.LPStr)] String szChannelName, out IntPtr warper, Int32 logLevel, [MarshalAs(UnmanagedType.LPStr)] String szLogFile);

        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_Init", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_Init(IntPtr warper);

        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_Destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_Destroy(IntPtr warper);

        /** set the new dynamic eye position and direction
        * @param [IN]			pWarper	a valid warper
        * @param [IN,OUT,OPT]	pEye	sets the new eye position, if a eye point provider is present, the value is getted from there, pEye is updated
        * @param [IN,OUT,OPT]	pRot	the new rotation in radian, if a eye point provider is present, the value is getted from there, pRot is updated
        * @param [OUT]			pView	it gets the updated view matrix to translate and rotate into the viewer's perspective
        * @param [OUT]			pProj	it gets the updated projection matrix
        * @param [OUT]			pClip	it gets the updated clip planes: left, top, right, bottom, near, far, where all components are usually positive
        * @param [OUT]			pPos    it gets the updated relative position: x,y,z
        * @param [OUT]			pDir	it gets the updated relative direction: euler angles around x,y,z rotation order is y,x,z
        * @param [IN,OPT]		symmetric set to true, to force symmetric frustum, NOTE: symmetric frusta are worse in quality, as the image plane is no longer parallel to the screen plane
        * @param [IN,OPT]		aspect	set to some ratio, to force clip planes to form a rect with this ratio, if set to 0, optimal aspect ratio is used. NOTE: Non-optimal aspect ratia are worse in quality, as part of the input image is rendered but not used.
        * @return VWB_ERROR_NONE on success, VWB_ERROR_GENERIC otherwise
        * @remarks If EyePointProvider is used, the eye point is set by calling it's getEye function. eye and rot are set to that if not NULL.
        * Else, if eye and rot are not NULL, values taken from here.
        * Else the eye and rot are set to 0-vectors.
        * The internal view and projection matrices are calculated to render. You should set pView and pProj to get these matrices for rendering, if updated.
        * positive rotation means turning right, up and clockwise.
        * Calculate view Matrix from pDir and pPos for left handed direct X
        XMMATRIX R = XMMatrixRotationRollPitchYaw( -pDir[0], pDir[1], pDir[2] );
        XMMATRIX T = XMMatrixTranslation( pPos[0], pPos[1], pPos[2] );
        XMMATRIX V = XMMatrixMultiply( R, T );
        * for right handed openGL
        V = glm::transpose( glm::yawPitchRoll( -dir[1],  dir[0], -dir[2] ) );
        V[0].w = pos[0];
        V[1].w = pos[1];
        V[2].w = pos[2];

        Use pClip this way to get the same matrix like from VWB_getViewProjection in left handed DX:
        P = XMMatrixPerspectiveOffCenterLH( -pClip[0], pClip[2], -pClip[3], pClip[1], pClip[4], pClip[5] );
        * for right handed openGL
        P = glm::frustum( -pClip[0], pClip[2], -pClip[3], pClip[1], pClip[4], pClip[5] );
        */
        //VWB_getViewProj, ( VWB_Warper* pWarper, VWB_float* pEye, VWB_float* pRot, VWB_float* pView, VWB_float* pProj));
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getViewProj", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getViewProj(IntPtr warper, ref VEC3 eye, ref VEC3 dir, ref MAT4X4 view, ref MAT4X4 proj);

        //VWB_getViewClip, ( VWB_Warper* pWarper, VWB_float* pEye, VWB_float* pRot, VWB_float* pView, VWB_float* pClip ) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getViewClip", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getViewClip(IntPtr warper, ref VEC3 eye, ref VEC3 dir, ref MAT4X4 view, ref CLIP clip);

        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_getPosDirClip, (VWB_Warper* pWarper, VWB_float* pEye, VWB_float* pRot, VWB_float* pPos, VWB_float* pDir, VWB_float* pClip, bool symmetric, VWB_float aspect) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getPosDirClip", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getPosDirClip(IntPtr warper, ref VEC3 eye, ref VEC3 dir, ref VEC3 rot, ref VEC3 pos, ref CLIP clip);

        /** query the corners of the screen plane
        * @param [IN]			pWarper	a valid warper 
        * @param [OUT]			pTL	it gets the top-left corner
        * @param [OUT]			pTR	it gets the top-right corner
        * @param [OUT]			pBL	it gets the bottom-left corner
        * @param [OUT]			pBR	it gets the bottom-right corner
        * @return VWB_ERROR_NONE on success, VWB_ERROR_GENERIC otherwise 
        NOTE: This is static to a mapping, just call once a session. */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_getScreenplane, (VWB_Warper* pWarper, VWB_float* pTL, VWB_float* pTR, VWB_float* pBL, VWB_float* pBR) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getScreenplane", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getScreenplane(IntPtr warper, ref VEC3 tl, ref VEC3 tr, ref VEC3 bl, ref VEC3 br);


        /** render a warped and blended source texture into the current back buffer
        * @remark for DirectX 9, call inside BeginScene/EndScene statements
        * @param [IN]			pWarper	a valid warper
        * @param [IN,OPT]		pSrc    the source texture, a IDirect3DTexture9*, ID3D10Texture2D*, ID3D11Texture2D*, VWB_D3D12_RENDERINPUT* or a GLint texture index; 
        * if current backbuffer must be read, set to NULL in any DX mode except 12 or to -1 in OpenGL mode
        * in case of directX 12 you need to provide a @see VWB_D3D12_RENDERINPUT as parameter.
        * @param [IN,OPT]		stateMask @see VWB_STATEMASK enumeration, default is 0 to restore usual stuff
        * In D3D12 all flags except VWB_STATEMASK_CLEARBACKBUFFER are ignored.
        * The application is required to set inputs and shader in each term anyway.
        * @return VWB_ERROR_NONE on success, VWB_ERROR_GENERIC otherwise */
        //VIOSOWARPBLEND_API( VWB_ERROR, VWB_render, ( VWB_Warper* pWarper, VWB_param src, VWB_uint stateMask ) );  
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_render", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_render(IntPtr warper, IntPtr src, UInt32 stateMask);

        /** get info about .vwf, reads all warp headers
        * @param [IN]			path	the file name or a comma separated list of filenames, set to NULL to release data from a previous set
        * @param [OUT]			set		a warp blend header set
        * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAM if fname is not set or empty, VWB_ERROR_VWF_FILE_NOT_FOUND if path did not resolve, VWB_ERROR_GENERIC otherwise
        * @remarks The list is empied and all found headers are appended. */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_vwfInfo, (char const* path, VWB_WarpBlendHeaderSet* set ) );  
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_vwfInfoC", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_vwfInfo([MarshalAs(UnmanagedType.LPStr)] String path, IntPtr set, ref uint size);

        /** fills a float[16] with the currently set internally used matrix for render shader. Warper needs to be initialized as VWB_DUMMYDEVICE.
         * @param [IN]			pWarper	a valid warper
         * @param [OUT]			pMPV	the internal view-projection matrix
         * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range, VWB_ERROR_GENERIC otherwise */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_getShaderVPMatrix, (VWB_Warper* pWarper, VWB_float* pMPV) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getShaderVPMatrix", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getShaderVPMatrix(IntPtr warper, ref MAT4X4 mat);

        /** fills a VWB_WarpBlend from currently loaded data. Warper needs to be initialized as VWB_DUMMYDEVICE.
        * @param [IN]			pWarper	a valid warper
        * @param [OUT]			mesh	the resulting mesh, the mesh will be emptied before filled
        * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range, VWB_ERROR_GENERIC otherwise */
        //VIOSOWARPBLEND_API( VWB_ERROR, VWB_getWarpBlend, ( VWB_Warper* pWarper, VWB_WarpBlend src, VWB_uint stateMask ) );  
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getWarpBlend", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getWarpBlend(IntPtr warper, out IntPtr wb);

        /** fills a VWB_WarpBlendMesh from currently loaded data. It uses cols * rows vertices or less depending on how .vwf is filled. Warper needs to be initialized as VWB_DUMMYDEVICE.
        * @param [IN]			pWarper	a valid warper
        * @param [IN]			cols	sets the number of columns
        * @param [IN]			rows	sets the number of rows
        * @param [OUT]			mesh	the resulting mesh, the mesh will be emptied before filled
        * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range, VWB_ERROR_GENERIC otherwise */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_getWarpBlendMeshC, (VWB_Warper* pWarper, VWB_int cols, VWB_int rows, VWB_WarpBlendMesh* mesh) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getWarpBlendMeshC", CallingConvention = CallingConvention.Cdecl)]
        static extern int VWB_getWarpBlendMesh(IntPtr warper, Int32 cols, Int32 rows, ref MarshalMesh mesh);

        /** destroys a VWB_WarpBlendMesh .
         * @param [IN]			pWarper	a valid warper
         * @param [INOUT]		mesh	the mesh to be destroyed
         * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range, VWB_ERROR_GENERIC otherwise */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_destroyWarpBlendMeshC, (VWB_Warper* pWarper, VWB_WarpBlendMesh* mesh) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_destroyWarpBlendMeshC", CallingConvention = CallingConvention.Cdecl)]
        static extern int VWB_destroyWarpBlendMesh(IntPtr warper, ref MarshalMesh mesh);

        /* get the version of the API
        * @param[OUT]			major	major version
        * @param[OUT]			minor	minor version
        * @param[OUT]			maintenance	maintenance revision
        * @param[OUT]			build	build number
        * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range */
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_getVersion, (VWB_int* major, VWB_int* minor, VWB_int* maintenance, VWB_int* build) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_getVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_getVersion(IntPtr warper, out Int32 major, out Int32 minor, out Int32 maintenance, out Int32 build);

        /* set a 128bit AES key for encryption and decryption of mappings
        * @param[IN]			key	    a key of 16 bytes (128 bit) provided for encryption/decryption of AES128 algorythm loading and saving vwf files, set to nullptr to disable encryption/decryption
        * @return VWB_ERROR_NONE on success, VWB_ERROR_PARAMETER, if parameters are out of range 
        * @remark Internally we keep only the pointer to your buffer. Make sure to keep the key there as long as it is needed. It will be used during VWB_init VWB_initExt */
        // use Marshal.AllocCoTaskMem to allocate memory (16 bytes) for the key and Marshal.copy from a string or byte array
        //VIOSOWARPBLEND_API(VWB_ERROR, VWB_setCryptoKey, (uint8_t const* key ) );
        [DllImport("VIOSOWarpBlend64.dll", EntryPoint = "VWB_setCryptoKey", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VWB_setCryptoKey(IntPtr pKey);


        IntPtr _warper = IntPtr.Zero;// = Marshal.AllocHGlobal(sizeof(Int64));

        static Warper()
        {
            DummyDevice = new IntPtr(1);
        }
        public Warper(System.IntPtr dx, String iniFile, String channelName)
        {
            try
            {
                int err = VWB_Create(dx, iniFile, channelName, out _warper, 2, "");
                if (ERROR.NONE != (ERROR)err || IntPtr.Zero == _warper)
                    throw new System.FieldAccessException("Could not create Warper. Err:" + err.ToString());
            }
            catch (Exception)
            {
                _warper = IntPtr.Zero;
                throw;
            }
        }

        ~Warper()
        {
            if (IntPtr.Zero != _warper)
                VWB_Destroy(_warper);
        }

        public ERROR Init()
        {
            return (ERROR)VWB_Init(_warper);
        }

        public ERROR Render(IntPtr src, UInt32 stateMask)
        {
            return (ERROR)VWB_render(_warper, src, stateMask);
        }

        static public ERROR GetVwfInfo(String path, out WarpBlendHeader[] headers)
        {
            headers = new WarpBlendHeader[0];
            uint len = 0;
            IntPtr ob = IntPtr.Zero;
            ERROR err = (ERROR)VWB_vwfInfo(path, ob, ref len );
            if (ERROR.NONE == err )
            {
                headers = new WarpBlendHeader[len];
                int sz = Marshal.SizeOf(headers[0]);
                ob = Marshal.AllocHGlobal((int)len * sz);
                err = (ERROR)VWB_vwfInfo(path, ob, ref len);
                if( ERROR.NONE == err )
                {
                    IntPtr p = ob;
                    for (uint i = 0; i != len; i++)
                    {
                        headers[i] = Marshal.PtrToStructure<WarpBlendHeader>(p);
                        p += sz;
                    }
                }
                Marshal.FreeHGlobal(ob);
            }
            return err;
        }
        public ERROR GetViewProj(ref MAT4X4 view, ref MAT4X4 proj)
        {
            VEC3 eye = new VEC3(0, 0, 0);
            VEC3 dir = new VEC3(0, 0, 0);

            return (ERROR)VWB_getViewProj(_warper, ref eye, ref dir, ref view, ref proj);
        }
        public ERROR GetViewProj(ref VEC3 eye, ref VEC3 dir, ref MAT4X4 view, ref MAT4X4 proj)
        {
            return (ERROR)VWB_getViewProj(_warper, ref eye, ref dir, ref view, ref proj);
        }
        public ERROR GetViewClip(ref MAT4X4 view, ref CLIP clip)
        {
            VEC3 eye = new VEC3(0, 0, 0);
            VEC3 dir = new VEC3(0, 0, 0);

            return (ERROR)VWB_getViewClip(_warper, ref eye, ref dir, ref view, ref clip);
        }
        public ERROR GetViewClip(ref VEC3 eye, ref VEC3 dir, ref MAT4X4 view, ref CLIP clip)
        {
            return (ERROR)VWB_getViewClip(_warper, ref eye, ref dir, ref view, ref clip);
        }
        public ERROR GetPosDirClip(ref VEC3 eye, ref VEC3 dir, ref VEC3 rot, ref VEC3 pos, ref CLIP clip)
        {
            return (ERROR)VWB_getPosDirClip(_warper, ref eye, ref dir, ref rot, ref pos, ref clip);
        }

        public ERROR GetShaderVPMatrix(ref MAT4X4 mat)
        {
            return (ERROR)VWB_getShaderVPMatrix(_warper, ref mat);
        }

        public ERROR GetWarpBlendHeader(out WarpFileHeader5 header)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
            return err;
        }
        public ERROR GetMappingFilePath(out String path)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            if (ERROR.NONE == err)
            {
                WarpFileHeader5 header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
                IntPtr ipo = new IntPtr(wb.ToInt64() + header.szHdr);
                path = Marshal.PtrToStringAnsi(ipo, 260);
            }
            else
                path = "";
            return err;
        }
        public ERROR GetWarpMap(out IntPtr warp)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            if (ERROR.NONE == err)
            {
                WarpFileHeader5 header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
                IntPtr ipo = new IntPtr(wb.ToInt64() + header.szHdr + 260 /*MAX_PATH*/);
                warp = Marshal.PtrToStructure<IntPtr>(ipo);
            }
            else
                warp = IntPtr.Zero;
            return err;
        }
        public ERROR GetBlendMap(out IntPtr blend)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            if (ERROR.NONE == err)
            {
                WarpFileHeader5 header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
                IntPtr ipo = new IntPtr(wb.ToInt64() + header.szHdr + 260 /*MAX_PATH*/ + IntPtr.Size);
                blend = Marshal.PtrToStructure<IntPtr>(ipo);
            }
            else
                blend = IntPtr.Zero;
            return err;
        }
        public ERROR GetBlackMap(out IntPtr black)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            if (ERROR.NONE == err)
            {
                WarpFileHeader5 header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
                IntPtr ipo = new IntPtr(wb.ToInt64() + header.szHdr + 260 /*MAX_PATH*/ + 2 * IntPtr.Size);
                black = Marshal.PtrToStructure<IntPtr>(ipo);
            }
            else
                black = IntPtr.Zero;
            return err;
        }
        public ERROR GetWhiteMap(out IntPtr white)
        {
            IntPtr wb;
            ERROR err = (ERROR)VWB_getWarpBlend(_warper, out wb);
            if (ERROR.NONE == err)
            {
                WarpFileHeader5 header = (WarpFileHeader5)Marshal.PtrToStructure(wb, typeof(WarpFileHeader5));
                IntPtr ipo = new IntPtr(wb.ToInt64() + header.szHdr + 260 /*MAX_PATH*/ + 3 * IntPtr.Size);
                white = Marshal.PtrToStructure<IntPtr>(ipo);
            }
            else
                white = IntPtr.Zero;
            return err;
        }

        public ERROR GetWarpBlendMesh( Int32 cols, Int32 rows, out Mesh mesh )
        {
            mesh = new Mesh();
            MarshalMesh m = new MarshalMesh();
            ERROR res = (ERROR)VWB_getWarpBlendMesh(_warper, cols, rows, ref m);
            if( ERROR.NONE == res )
            {
                mesh.vtx = new Vertex[m.nVtx];
                mesh.idx = new UInt32[m.nIdx];
                IntPtr p = m.vtx;
                for (UInt32 i = 0; i != m.nVtx; i++)
                {
                    mesh.vtx[i] = Marshal.PtrToStructure<Vertex>(p);
                    p += Marshal.SizeOf(mesh.vtx[i]);
                }
                p = m.idx;
                for (UInt32 i = 0; i != m.nIdx; i++)
                {
                    mesh.idx[i] = Marshal.PtrToStructure<UInt32>(p);
                    p += Marshal.SizeOf(mesh.idx[i]);
                }
                mesh.res = m.dim;
                VWB_destroyWarpBlendMesh(_warper, ref m);
            }
            return res;
        }

        static public ERROR SetCryptoKey(byte[] key)
        {
            if (null == key)
            {
                if (null != cryptoKeyMem)
                {
                    Marshal.FreeHGlobal(cryptoKeyMem);
                }
                return (ERROR)VWB_setCryptoKey(IntPtr.Zero);
            }
            else
            {
                if (key.Length < 16)
                    return ERROR.PARAMETER;

                if (cryptoKeyMem == null)
                {
                    cryptoKeyMem = Marshal.AllocHGlobal(16);
                }
                Marshal.Copy(key, 0, cryptoKeyMem, 16);
                return (ERROR)VWB_setCryptoKey(cryptoKeyMem); ;
            }
        }

        public VWB_Warper Get()
        {
            return (VWB_Warper)Marshal.PtrToStructure(_warper, typeof(VWB_Warper));
        }
        public void Set(ref VWB_Warper warper)
        {
            Marshal.StructureToPtr(warper, _warper, false);
        }
    }
}