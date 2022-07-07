using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";
    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private ShadowSettings _setting;
    
    private const int maxShadowedDirectionalLightCount = 1;
    private int _shadowedDirectionalLightCount = 0;
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    private ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    public void SetPerFrame(ref ScriptableRenderContext context, ShadowSettings settings)
    {
        _context = context;
        _setting = settings;
    }
    
    public void SetPerCamera(CullingResults cullingResults)
    {
        _cullingResults = cullingResults;
        _shadowedDirectionalLightCount = 0;
    }

    public void Render()
    {
        if (_shadowedDirectionalLightCount > 0)
        {
            var cmd = CommandBufferPool.Get();
            cmd.name = "Shadows";
            cmd.BeginSample("Render Shadow Map");
            
            // TODO : 有空了重构下shadow的矩阵设置环节
            Shader.DisableKeyword("_MANY_LIGHT");
            Shader.DisableKeyword("_VSM");
            if(_setting.shadowType == ShadowSettings.ShadowType.ManyLight) Shader.EnableKeyword("_MANY_LIGHT");
            RenderDirectionalShadows();
            
            
            if(_setting.shadowType == ShadowSettings.ShadowType.VSM)
            {
                Shader.EnableKeyword("_VSM");
                RenderVarianceShadowMapping(ShadowedDirectionalLights[0], (int)_setting.directional.atlasSize);
            }

            cmd.EndSample("Render Shadow Map");
            CommandHelper.ExecuteAndRelese(_context, cmd);
        }
    }

    private static int _dirShadowMap = Shader.PropertyToID("_DirShadowMap");
    private static int _directionalShadowMatrices = Shader.PropertyToID("_DirectionalShadowMatrices");
    private static Matrix4x4[] _dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];
    private int _shadowMapTileSize = 0;
    void RenderDirectionalShadows()
    {
        int _shadowMapSize = (int)_setting.directional.atlasSize;
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.name = "Shadows";
        cmd.BeginSample("Clear Shadow Map");
        cmd.GetTemporaryRT(_dirShadowMap, _shadowMapSize, _shadowMapSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        cmd.SetRenderTarget(_dirShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.EndSample("Clear Shadow Map");
        CommandHelper.ExecuteAndRelese(_context, cmd);

        cmd = CommandBufferPool.Get();
        cmd.name = "Shadows";
        cmd.BeginSample("Draw Directional Shadow Map");
        for (int i = 0; i < _shadowedDirectionalLightCount; i++)
        {
            // TODO : 实现RSM
            RenderDirectionalShadows(i, _shadowMapTileSize);
        }
        cmd.SetGlobalMatrixArray(_directionalShadowMatrices, _dirShadowMatrices);
        cmd.EndSample("Draw Directional Shadow Map");
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int spilt)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            //m.SetColumn(2, m.GetColumn(2) * -1);
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / _split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index%split, index/split);
        var cmd = CommandBufferPool.Get("Shadows");
        cmd.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        CommandHelper.ExecuteAndRelese(_context, cmd);
        return offset;
    }

    private int _split = 0;
    void RenderDirectionalShadows(int lightIndex, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[lightIndex];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex);
        _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;

        Vector2 offset = SetTileViewport(lightIndex, _split, _shadowMapTileSize);
        _dirShadowMatrices[lightIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, _split);
        var cmd = CommandBufferPool.Get("Shadows");
        cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        CommandHelper.ExecuteAndRelese(_context, cmd);
        
        // 把shadow map按投射阴影的light数量，划分 Tile就行渲染
        cmd = CommandBufferPool.Get("Shadows");
        cmd.BeginSample("Draw Shadow Map");
        cmd.SetGlobalDepthBias(90000, 0);
        cmd.EndSample("Draw Shadow Map");
        CommandHelper.ExecuteAndRelese(_context, cmd);
        _context.DrawShadows(ref shadowSettings);
        cmd = CommandBufferPool.Get("Shadows");
        cmd.SetGlobalDepthBias(0, 0);
        CommandHelper.ExecuteAndRelese(_context, cmd);
        
        if (_setting.shadowType == ShadowSettings.ShadowType.ManyLight)
        {
            
        cmd = CommandBufferPool.Get("Shadows");
        cmd.BeginSample("Draw Shadow Map");
        cmd.SetGlobalDepthBias(90000, 0);
        cmd.EndSample("Draw Shadow Map");
        CommandHelper.ExecuteAndRelese(_context, cmd);
        _context.DrawShadows(ref shadowSettings);
        cmd = CommandBufferPool.Get("Shadows");
        cmd.SetGlobalDepthBias(0, 0);
        CommandHelper.ExecuteAndRelese(_context, cmd);
        }
    }

    // 主光源VSM
    public static int _varianceShadowMapping = Shader.PropertyToID("_VarianceShadowMapping");
    private static int _sourceTex_TexelSize = Shader.PropertyToID("_SourceTex_TexelSize");
    public static int _vsmBlurVDst = Shader.PropertyToID("_VsmBlurVDst");
    private Shader _shader = Shader.Find("RhythmRP/Rhythm_Shadow_Cast");
    private Material _mat;
    private const string cmdName = "VSMCast";
    public static RenderTexture _vsmRT;
    
    void RenderVarianceShadowMapping(ShadowedDirectionalLight mainLight, int shadowMapSize)
    {
        if (_mat == null) _mat = new Material(_shader);
        if (_vsmRT == null)
        {
            _vsmRT = new RenderTexture(shadowMapSize, shadowMapSize, 32, RenderTextureFormat.RGFloat);
            if (!_vsmRT.IsCreated())
            {
                _vsmRT.name = "_VarianceShadowMapping";
                _vsmRT.useMipMap = true;
                _vsmRT.filterMode = FilterMode.Trilinear;
                _vsmRT.anisoLevel = 9;
                _vsmRT.Create();
                //texture.Release();
            }
        }
        
        var cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample("Clear VSM");
        cmd.SetRenderTarget(_vsmRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.EndSample("Clear VSM");
        CommandHelper.ExecuteAndRelese(_context, cmd);

        cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample(bufferName);
        // 获取所有需要投射阴影的物体
        var allRenderers = RhythmPipeline.PipelineManager.RhythmPipeline.AllRenderers;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i].isVisible&&allRenderers[i].shadowCastingMode == ShadowCastingMode.On)
            {
                cmd.DrawMesh(allRenderers[i].GetComponent<MeshFilter>().sharedMesh, allRenderers[i].GetComponent<Transform>().localToWorldMatrix, _mat, 0, 1);
                //Debug.Log("第"+i+"个"+allRenderers[i].GetComponent<MeshFilter>().mesh.name);
            }
        }
        cmd.EndSample(bufferName);
        CommandHelper.ExecuteAndRelese(_context, cmd);

        // VSM pre filter
        cmd=CommandBufferPool.Get(cmdName);
        cmd.GetTemporaryRT(_vsmBlurVDst,shadowMapSize,shadowMapSize,0,FilterMode.Bilinear,RenderTextureFormat.RG32);
        CommandHelper.ExcuteButNotRelease(_context, cmd);

        _mat.SetVector(_sourceTex_TexelSize,  new Vector2(1.0f / shadowMapSize, 1.0f / shadowMapSize));
        cmd.BeginSample("blur vsm");
        cmd.Blit(_vsmRT,_vsmBlurVDst,_mat,2);
        cmd.EndSample("blur vsm");
        CommandHelper.ExcuteButNotRelease(_context, cmd);

        cmd.BeginSample("blur vsm");
        cmd.Blit(_vsmBlurVDst,_vsmRT,_mat,3);
        cmd.EndSample("blur vsm");
        CommandHelper.ExcuteButNotRelease(_context, cmd);
        
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }
    
    void RenderReflectiveShadowMaps(int lightIndex, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[lightIndex];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex);
        _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;

        Vector2 offset = SetTileViewport(lightIndex, _split, _shadowMapTileSize);
        _dirShadowMatrices[lightIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, _split);
        var cmd = CommandBufferPool.Get("Shadows");
        cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        CommandHelper.ExecuteAndRelese(_context, cmd);
        // 把shadow map按投射阴影的light数量，划分 Tile就行渲染
        cmd = CommandBufferPool.Get("Shadows");
        cmd.BeginSample("Draw Shadow Map");
        cmd.SetGlobalDepthBias(90000, 0);
        cmd.EndSample("Draw Shadow Map");
        CommandHelper.ExecuteAndRelese(_context, cmd);
        _context.DrawShadows(ref shadowSettings);
        cmd = CommandBufferPool.Get("Shadows");
        cmd.SetGlobalDepthBias(0, 0);
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }
    
    public void Cleanup()
    {
        var cmd = CommandBufferPool.Get();
        cmd.name = "cleanup shadows";
        cmd.BeginSample(cmd.name);
        cmd.ReleaseTemporaryRT(_dirShadowMap);
        cmd.EndSample(cmd.name);
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }

    private static int _shadowStrength = Shader.PropertyToID("_ShadowStrength");
    public void  ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowedDirectionalLightCount < maxShadowedDirectionalLightCount
            && light.shadows != LightShadows.None && light.shadowStrength > 0f
            && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight
                {visibleLightIndex = visibleLightIndex};
            var cmd = CommandBufferPool.Get("Shadows");
            cmd.SetGlobalFloat(_shadowStrength, light.shadowStrength);
            CommandHelper.ExecuteAndRelese(_context, cmd);
        }

        _split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
        _shadowMapTileSize = (int) _setting.directional.atlasSize / _split;
    }
}
