using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmPipeline.Runtime
{
    public class ForwardRenderer
    {
        private ScriptableRenderContext _context;
        private static int _cameraFrameBuffer = Shader.PropertyToID("_CameraFrameBuffer");
        private Camera _camera;

        CullingResults _cullingResults;
        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId pbrShaderTagId = new ShaderTagId("RhythmPBR");
        
        private bool _useDynamicBatching = false;
        private bool _useGPUInstancing = false;
        private ShadowSettings _shadowSettings;

        private PostProcessingFramwork _processing = new PostProcessingFramwork();
        public void RenderSingleCamera()
        {
#if UNITY_EDITOR
            PrepareForSceneWindow();
#endif
            //if (!Cull()) return;
            SetRenderState();
            DrawOpaque();
            DrawSkybox();
            DrawTransparent();
            _processing.RenderPostList();
#if UNITY_EDITOR
            DrawUnsupportedShaders();
            DrawGizmos();
#endif
            _context.Submit();
        }

        bool Cull()
        {
            if(_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                _cullingResults = _context.Cull(ref p);
                return true;
            }
            else return false;
        }

        private void SetRenderState()
        {
            var cmd = CommandBufferPool.Get(_camera.name);
            cmd.BeginSample("Set Render State");
            Shader.SetGlobalTexture(Shadows._varianceShadowMapping, Shadows._vsmRT);
            Shader.SetGlobalTexture(RSM._worldPos, RSM._rsmTargets[0]);
            Shader.SetGlobalTexture(RSM._flux, RSM._rsmTargets[1]);
            Shader.SetGlobalTexture(RSM._worldNormal, RSM._rsmTargets[2]);
            cmd.EndSample("Set Render State");
        }
        private void DrawOpaque()
        {
            var cmd = CommandBufferPool.Get(_camera.name);
            cmd.BeginSample("Opaque");
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _useDynamicBatching,
                enableInstancing = _useGPUInstancing
            };
            drawingSettings.SetShaderPassName(1, pbrShaderTagId);
            
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            cmd.EndSample("Opaque");
            ExecuteTempBuffer(cmd);
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void DrawSkybox()
        {
            var cmd = CommandBufferPool.Get(_camera.name);
            cmd.BeginSample("Skybox");
            _context.DrawSkybox(_camera);
            cmd.EndSample("skybox");
        }

        private void DrawTransparent()
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };

            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _useDynamicBatching,
                enableInstancing = _useGPUInstancing
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        public void SetPerFrame(ref ScriptableRenderContext context, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            _context = context;
            _useDynamicBatching = useDynamicBatching;
            _useGPUInstancing = useGPUInstancing;
            _shadowSettings = shadowSettings;
            _processing.SetPerFrame(ref context);
        }

        public void SetPerCamera(Camera camera, CullingResults cullingResults, PostProcessingSettings processingSettings)
        {
            _camera = camera;
            _context.SetupCameraProperties(_camera);
            _cullingResults = cullingResults;
            _processing.SetPerCamera(_camera, _cameraFrameBuffer, processingSettings);
            CameraClearFlags flags = _camera.clearFlags;
            
            var cmd = CommandBufferPool.Get(_camera.name);
            cmd.BeginSample("Set Framebuffer");
            cmd.GetTemporaryRT(_cameraFrameBuffer, _camera.pixelWidth, _camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.SetRenderTarget(_cameraFrameBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(flags <= CameraClearFlags.Depth, 
                flags <= CameraClearFlags.Color, 
                flags == CameraClearFlags.Color? _camera.backgroundColor.linear:Color.clear);
            cmd.EndSample("Set Framebuffer");
            ExecuteTempBuffer(cmd);
        }
        
        void ExecuteTempBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_EDITOR
        static ShaderTagId[] notsupportShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };
        static Material errorMat;
        void DrawUnsupportedShaders()
        {
            if(errorMat == null)
            {
                errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            var sortingSettings = new SortingSettings(_camera);
            var drawingSettings = new DrawingSettings(notsupportShaderTagIds[0], sortingSettings);
            drawingSettings.overrideMaterial = errorMat;

            for(int i = 1; i < notsupportShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, notsupportShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        void DrawGizmos()
        {
            if(Handles.ShouldRenderGizmos())
            {
                _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }
        }
        void PrepareForSceneWindow()
        {
            if(_camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
            }
        }
#endif

    }
}
