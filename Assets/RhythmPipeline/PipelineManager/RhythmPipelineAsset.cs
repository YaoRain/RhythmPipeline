using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace RhythmPipeline.PipelineManager
{
    [CreateAssetMenu(menuName = "Rendering/CreateRhythmPipeline")]
    public class RhythmPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] 
        public bool useDynamicBatching = false;
        [SerializeField] 
        public bool useGPUInstancing = false;
        [SerializeField] 
        public bool useSRPBatching = false;
        [SerializeField]
        public ShadowSettings shadowSettings = default;
        [SerializeField]
        public PostProcessingSettings postProcessingSettings = default;
        
        protected override RenderPipeline CreatePipeline()
        {
            //return new RhythmPipeline(useDynamicBatching, useGPUInstancing, useSRPBatching, shadowSettings);
            return new RhythmPipeline(this);
        }
    }
}
