using System.Collections;
using System.Collections.Generic;
using RhythmPipeline.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessingFramwork
{
    private ScriptableRenderContext _context;
    private PostProcessingSettings _postProcessingSettings;
    private Camera _camera;
    private bool IsActive => _postProcessingSettings != null;

    private int _lastRenderTex;
    private List<PostProcessingFeature> _postProcessingFeatures = new List<PostProcessingFeature>();
    
    public void SetPerFrame(ref ScriptableRenderContext context)
    {
        _context = context;
    }
    
    public void SetPerCamera(Camera camera, int cameraRT, PostProcessingSettings postProcessingSettings)
    {
        _camera = camera;
        _postProcessingSettings = postProcessingSettings;
        _lastRenderTex = cameraRT;
        ChangePostState();
    }

    bool IsFeatureExist(PostProcessingFeature feature)
    {
        foreach (var existFeature in _postProcessingFeatures)
        {
            if (feature.name == existFeature.name) return true;
        }
        return false;
    }
    
    // 填充并排序Post Features
    void ChangePostState()
    {
        _postProcessingFeatures.Clear();
        foreach (var postFeature in _postProcessingSettings.postFeatures)
        {
            PostProcessingFeature feature = _postProcessingSettings.PostProcessingFeatureCreater(postFeature);
            if(feature != null&& !IsFeatureExist(feature)) _postProcessingFeatures.Add(feature);
            else Debug.LogError("can't create post processing : " + postFeature);
        }
        _postProcessingFeatures.Sort((x, y) =>
        {
            return x.order.CompareTo(y.order);
        });
    }
    
    public void RenderPostList()
    {
        for (int i = 0; i < _postProcessingFeatures.Count; i++)
        {
            _postProcessingFeatures[i].Render(ref _context, _camera,ref _lastRenderTex);
        }
    }

    public void Cleanup()
    {
        var cmd = CommandBufferPool.Get();
        cmd.name = "cleanup shadows";
        cmd.BeginSample(cmd.name);
        //cmd.ReleaseTemporaryRT(_dirShadowMap);
        cmd.EndSample(cmd.name);
        ExecuteTempBuffer(cmd);
    }
    
    void ExecuteTempBuffer(CommandBuffer cmd)
    {
        _context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
