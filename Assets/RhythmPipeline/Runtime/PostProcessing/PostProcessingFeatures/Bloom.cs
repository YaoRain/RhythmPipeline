using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Bloom : PostProcessingFeature
{
    
    private static int _bloomDstTex = Shader.PropertyToID("_BloomDstTex");
    private static int _brightAreaTex = Shader.PropertyToID("_BrightAreaTex");
    private static int _blurVDstTex = Shader.PropertyToID("_BlurVDstTex");
    private static int _blurDstTex = Shader.PropertyToID("_BlurDstTex");
    private static int _originalTex = Shader.PropertyToID("_OriginalTex");
    
    private const int BLUR_TIEMS = 5;
    private int[] _lowMips = new int[BLUR_TIEMS];
    private int[] _highMipsSrc = new int[BLUR_TIEMS];
    private int[] _highMipsDst = new int[BLUR_TIEMS];
    
    private Shader _shader = Shader.Find("RhythmRP/Post/Bloom");
    private Material _mat;
    private int _getBrightAreaPass = 0;
    private int _blurVPass = 1;
    private int _blurHPass = 2;
    private int _upsamplePass = 3;
    private int _bloomFinalPass = 4;
    
    private static bool _needInit = true;
    public Bloom(PostProcessingFeatureType postProcessingFeatureType) : base(postProcessingFeatureType)
    {
    }

    private void InitBlurMips(int width, int height , ref ScriptableRenderContext context)
    {
        var cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample(cmdName);

        for (int i = 0; i < BLUR_TIEMS; i++)
        {
            width /= 2;
            height /= 2;
            string highMipNames = "_HighMipDst" + i;
            _highMipsDst[i] = Shader.PropertyToID(highMipNames);
            cmd.GetTemporaryRT(_highMipsDst[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            
            highMipNames = "_HighMipSrc" + i;
            _highMipsSrc[i] = Shader.PropertyToID(highMipNames);
            cmd.GetTemporaryRT(_highMipsSrc[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        }
        cmd.EndSample(cmdName);
        ExcuteTempBuffer(ref context, cmd);
        _needInit = false;
    }
    public override void Render(ref ScriptableRenderContext context,  Camera camera, ref int sourceTexId)
    {
        if (_mat == null) _mat = new Material(_shader);
        InitBlurMips(camera.pixelWidth, camera.pixelHeight, ref context);
        var cmd = CommandBufferPool.Get(cmdName);
        cmd.BeginSample(name);
        cmd.GetTemporaryRT(_brightAreaTex, camera.pixelWidth/2, camera.pixelHeight/2, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        cmd.GetTemporaryRT(_bloomDstTex, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        cmd.GetTemporaryRT(_originalTex, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        cmd.Blit(sourceTexId, _originalTex);
        cmd.EndSample(name);
        ExcuteButNotRelease(ref context, cmd);
        
        cmd.BeginSample(name);
        cmd.Blit(sourceTexId, _highMipsSrc[0], _mat, _getBrightAreaPass);
        cmd.EndSample(name);
        ExcuteButNotRelease(ref context, cmd);
        sourceTexId =  _highMipsSrc[0];
        
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;
        for (int i = 0; i < BLUR_TIEMS; i++)
        {
            width /= 2;
            height /= 2;
            cmd.ReleaseTemporaryRT(_blurVDstTex);
            cmd.GetTemporaryRT(_blurVDstTex, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            ExcuteButNotRelease(ref context, cmd);
             
            cmd.BeginSample(name);
            cmd.Blit(sourceTexId, _blurVDstTex, _mat, _blurVPass);
            cmd.EndSample(name);
            ExcuteButNotRelease(ref context, cmd);
            sourceTexId = _blurVDstTex;

            int blurDst;
            if (i == BLUR_TIEMS - 1)
            {
                cmd.ReleaseTemporaryRT(_blurDstTex);
                cmd.GetTemporaryRT(_blurDstTex, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                ExcuteButNotRelease(ref context, cmd);
                blurDst = _blurDstTex;
            }
            else
            {
                blurDst = _highMipsSrc[i + 1];
            }
            cmd.BeginSample(name);
            cmd.Blit(sourceTexId, blurDst, _mat, _blurHPass);
            cmd.EndSample(name);
            ExcuteButNotRelease(ref context, cmd);
            sourceTexId = blurDst;
        }
        _lowMips[BLUR_TIEMS - 1] = _blurDstTex;

        for (int i = BLUR_TIEMS-1; i >= 0; i--)
        {
            cmd.BeginSample(name);
            cmd.SetGlobalTexture(_blurDstTex, _lowMips[i]);
            cmd.Blit(_highMipsSrc[i], _highMipsDst[i], _mat, _upsamplePass);
            cmd.EndSample(name);
            if(i > 0) _lowMips[i - 1] = _highMipsDst[i];
        }

        cmd.BeginSample(name);
        cmd.SetGlobalTexture(_blurDstTex, _highMipsDst[0]);
        cmd.Blit(_originalTex, _bloomDstTex, _mat, _bloomFinalPass);
        cmd.EndSample(name);
        ExcuteTempBuffer(ref context, cmd);
        sourceTexId = _bloomDstTex;
    }

    void ExcuteButNotRelease(ref ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    
}
