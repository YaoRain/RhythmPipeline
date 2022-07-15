using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private Shadows _shadows = new Shadows();
    private RSM _rsm = new RSM();
    private LPV _lpv = new LPV();
    
    private const string bufferName = "Lighting";
    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private const int maxDirLightCount = 4;
    

    private static int _DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount");
    private static int _DirectionalLightRadiantItensitys = Shader.PropertyToID("_DirectionalLightRadiantItensitys");
    private static int _DirectionalLightDirections = Shader.PropertyToID("_DirectionalLightDirections");

    private static Vector4[] _dirLightRadiantItensitys = new Vector4[maxDirLightCount];
    private static Vector4[] _dirLightDirections = new Vector4[maxDirLightCount];

    public void SetPerFrame(ref ScriptableRenderContext context, ShadowSettings shadowSettings, GISettings giSettings )
    {
        _context = context;
        _shadows.SetPerFrame(ref context, shadowSettings);
        _rsm.SetPerFrame(ref context, giSettings, (int)shadowSettings.directional.atlasSize);
        _lpv.SetPerFrame(ref context);
    }
    
    public void SetPerCamera(CullingResults cullingResults)
    {
        _cullingResults = cullingResults;
        _shadows.SetPerCamera(cullingResults);

        var cmd = CommandBufferPool.Get(bufferName);
        cmd.BeginSample("Set Per Camera");
        SetupDirectionalLights();
        _shadows.Render();
        _rsm.Render();
        _lpv.CreateLPV();
        cmd.EndSample("Set Per Camera");
        ExecuteTempBuffer(cmd);
        _context.Submit();
    }

    public void Cleanup()
    {
        _shadows.Cleanup();
        _context.Submit();
    }
    
    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
    void ExecuteTempBuffer(CommandBuffer cmd)
    {
        _context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    private void SetupDirectionalLights()
    {
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
        int dirLightCnt = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            var visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                _dirLightRadiantItensitys[i] = visibleLight.finalColor;
                _dirLightDirections[i] = -visibleLight.localToWorldMatrix.GetColumn(2);
                dirLightCnt++;
                _shadows.ReserveDirectionalShadows(visibleLight.light, i);
            }
            if(dirLightCnt >= maxDirLightCount) break;
        }

        var cmd = CommandBufferPool.Get();
        cmd.name = "Light";
        cmd.BeginSample("Setup Directional Lights");
        cmd.SetGlobalInt(_DirectionalLightCount, dirLightCnt);
        cmd.SetGlobalVectorArray(_DirectionalLightRadiantItensitys, _dirLightRadiantItensitys);
        cmd.SetGlobalVectorArray(_DirectionalLightDirections, _dirLightDirections);
        cmd.EndSample("Setup Directional Lights");
        ExecuteTempBuffer(cmd);
    }
}
