using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Runtime.InteropServices;
using System;
using VIOSOWarpBlend;

[Serializable, VolumeComponentMenu("Post-processing/Custom/VIOSOWarpBlendPP")]
public sealed class VIOSOWarpBlendPP : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    const string kShaderName = "Hidden/Shader/VIOSOWarpBlendPP";
    public Material m_Material;

    public bool IsActive() => (m_Material != null);

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > HDRP Default Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;
    // try to disable PPFX in scene view
    public override bool visibleInSceneView => false;

    /// <summary>
    /// setup (onStart)
    /// </summary>
    public void Start()
    {
        Debug.Log("VIOSOWarpBlendPP.Start()");
    }

    public override void Setup()
    {
       // Debug.Log("VIOSOWarpBlendPP.Setup() begin");
        if (Shader.Find(kShaderName) == null)
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume viosoPostProcessVolume is unable to load.");

        m_Material = new Material(Shader.Find(kShaderName));
    }

    /// <summary>
    /// Process function where we update texture with VIOSO API
    /// </summary>
    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        // get matching set to used camera
        if (VIOSOHDRPCamera._warperDict.TryGetValue(camera.camera.name, out VIOSOHDRPCamera.WarperSet s))
        {
            Matrix4x4 mVP = s._ppMatrix;

            m_Material.SetMatrix("matView", mVP);

            try
            { // update
                Warper.VWB_Warper ini = s._warper.Get();
                s._bBorder[1] = ini.bDoNotBlend ? 0 : 1;
                s._bBorder[3] = ini.bBicubic ? 1 : 0;
                m_Material.SetVector("bBorder", s._bBorder);
                s._blackBias[3] = ini.bDoNoBlack ? 0 : 1;
                m_Material.SetVector("blackBias", s._blackBias);

                if (ini.bPartialInput)
                {
                    m_Material.SetVector("offsScale", new Vector4(
                        ini.optimalRect.left / ini.optimalRes.cx,
                        ini.optimalRect.top / ini.optimalRes.cy,
                        (ini.optimalRect.right - ini.optimalRect.left) / ini.optimalRes.cx,
                        (ini.optimalRect.bottom - ini.optimalRect.top) / ini.optimalRes.cy));
                }
                else
                {
                    m_Material.SetVector("offsScale", new Vector4(0, 0, 1, 1));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("VIOSOWarpBlendPP.Render(" + camera.camera.name + ") " + ex.ToString());
            }

            m_Material.SetTexture("_texContent", source);
            m_Material.SetVector("mapSize", s._size);
            m_Material.SetTexture("_texWarp", s._texWarp);
            m_Material.SetTexture("_texBlend", s._texBlend);
            m_Material.SetTexture("_texBlack", s._texBlack);

            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }
    }
}
