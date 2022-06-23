using System.Collections.Generic;
using RhythmPipeline.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmPipeline.PipelineManager
{
    public class RhythmPipeline : RenderPipeline
    {
        private ForwardRenderer _forwardRender = new ForwardRenderer();
        private Lighting _lighting = new Lighting();
        
        private bool _useDynamicBatching;
        private bool _useGPUInstancing;
        private ShadowSettings _shadowSettings;
        private PostProcessingSettings _postProcessingSettings;

        public static Renderer[] AllRenderers;
        public RhythmPipeline(RhythmPipelineAsset pipelineAsset)
        {
            _useDynamicBatching = pipelineAsset.useDynamicBatching;
            _useGPUInstancing = pipelineAsset.useGPUInstancing;
            _shadowSettings = pipelineAsset.shadowSettings;
            _postProcessingSettings = pipelineAsset.postProcessingSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = pipelineAsset.useSRPBatching;
            GraphicsSettings.lightsUseLinearIntensity = true;
            AllRenderers = GameObject.FindObjectsOfType<Renderer>();
        }
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            _forwardRender.SetPerFrame(ref context, _useDynamicBatching, _useGPUInstancing, _shadowSettings);
            _lighting.SetPerFrame(ref context, _shadowSettings);
            
            // notice: 不允许使用不同相机渲染同一个frame buffer
            for(int i = 0; i < cameras.Length; i++)
            {
                CullingResults cullingResults = new CullingResults();
                if (Cull(ref context, cameras[i], _shadowSettings.maxDistance,ref cullingResults))
                {
                    _lighting.SetPerCamera(cullingResults);
                    // TODO : 多相机场景，global post 的设置作为相机的属性
                    _forwardRender.SetPerCamera(cameras[i], cullingResults, _postProcessingSettings);
                    _forwardRender.RenderSingleCamera();
                    _lighting.Cleanup();
                }
            }
        }
        
        bool Cull(ref ScriptableRenderContext context, Camera camera, float maxShadowDistance, ref CullingResults cullingResults)
        {
            if(camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref p);
                return true;
            }
            else return false;
        }
    }
}
