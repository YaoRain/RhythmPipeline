using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class FinalBlit : PostProcessingFeature
{
    public FinalBlit(PostProcessingFeatureType postProcessingFeatureType) : base(postProcessingFeatureType)
    {
        
    }
    public override void Render(ref ScriptableRenderContext context, Camera camera, ref int sourceTexId)
    {
        var cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample(name);
        cmd.Blit(sourceTexId, BuiltinRenderTextureType.CameraTarget);
        cmd.EndSample(name);
        ExcuteTempBuffer(ref context, cmd);
        sourceTexId = -1;
    }
}
