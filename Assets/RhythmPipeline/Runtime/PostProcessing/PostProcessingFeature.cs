using UnityEngine;
using UnityEngine.Rendering;

public abstract class PostProcessingFeature 
{
    public string name;
    public int order = 3000;
    protected const string cmdName = "Post Processing";
    protected PostProcessingFeature(PostProcessingFeatureType postProcessingFeatureType)
    {
        name = postProcessingFeatureType.ToString();
        order = (int)postProcessingFeatureType;
    }

    public abstract void Render(ref ScriptableRenderContext context, Camera camera, ref int sourceTexId);
    protected void ExcuteTempBuffer(ref ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
