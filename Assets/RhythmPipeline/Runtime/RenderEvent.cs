using UnityEngine;
using UnityEngine.Rendering;
public abstract class RenderEvent
{
    public string name;
    public int order = 0;
    protected const string cmdName = "Post Processing";
    protected RenderEvent(PostProcessingFeatureType postProcessingFeatureType)
    {
        name = postProcessingFeatureType.ToString();
        order = (int)postProcessingFeatureType;
    }

    public abstract void SetPerFrame(ref ScriptableRenderContext context, ShadowSettings shadowSettings);

    public void SetPerCamera(CullingResults cullingResults)
    {
    }

    protected RenderEvent()
    {
        
    }
    
    public abstract void Render(ref ScriptableRenderContext context, Camera camera, ref int sourceTexId);
    protected void ExcuteTempBuffer(ref ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
