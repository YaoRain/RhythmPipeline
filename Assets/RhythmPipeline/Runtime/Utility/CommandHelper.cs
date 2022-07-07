using UnityEngine;
using UnityEngine.Rendering;

public class CommandHelper
{
    public static void ExecuteAndRelese(ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public static void ExcuteButNotRelease(ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
}
