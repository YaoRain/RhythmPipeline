using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ToneMapping : PostProcessingFeature
{
    
    private static int _toneMappingDstTex = Shader.PropertyToID("_AfterPostprocessingTex");
    private Shader _shader = Shader.Find("RhythmRP/Post/ToneMapping");
    private Material _mat;
    public ToneMapping(PostProcessingFeatureType postProcessingFeatureType) : base(postProcessingFeatureType)
    {
        
    }
    public override void Render(ref ScriptableRenderContext context,  Camera camera, ref int sourceTexId)
    {
        if (_mat == null) _mat = new Material(_shader);
        
        var cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample(name);
        cmd.GetTemporaryRT(_toneMappingDstTex, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        cmd.Blit(sourceTexId, _toneMappingDstTex, _mat, 0);
        cmd.EndSample(name);
        ExcuteTempBuffer(ref context, cmd);
        sourceTexId = _toneMappingDstTex;
    }

}
