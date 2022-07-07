using UnityEngine;
using UnityEngine.Rendering;

public class RSM
{
    private const string bufferName = "RSM";
    private GISettings _settings = new GISettings();
    private int _rsmSize = 1024;
    private ScriptableRenderContext _context;

    public RSM()
    {
        
    }

    public void SetPerFrame(ref ScriptableRenderContext context, GISettings giSettings, int rsmSize)
    {
        _context = context;
        _settings = giSettings;
        _rsmSize = rsmSize;
    }
    
    
    private static RenderTexture[] _rsmTargets = new RenderTexture[3];
    private static RenderTargetIdentifier[] _rsmTargetIdentifiers = new RenderTargetIdentifier[3];

    private bool _isInit = false;
    private void InitRsmTargets()
    {
        for(int i = 0; i < _rsmTargets.Length; i++) _rsmTargets[i] = new RenderTexture(_rsmSize, _rsmSize, 0, RenderTextureFormat.Default);
        _rsmTargets[0] = new RenderTexture(_rsmSize, _rsmSize, 0, RenderTextureFormat.RGB111110Float);
        _rsmTargets[0].name = "WorldPos";
        _rsmTargets[1] = new RenderTexture(_rsmSize, _rsmSize, 0, RenderTextureFormat.Default);
        _rsmTargets[1].name = "Flux";
        _rsmTargets[2] = new RenderTexture(_rsmSize, _rsmSize, 0, RenderTextureFormat.Default);
        _rsmTargets[2].name = "WorldNormal";
        for (int i = 0; i < _rsmTargets.Length; i++)
        {
            if (!_rsmTargets[i].IsCreated())
            {
                _rsmTargets[i].useMipMap = false;
                _rsmTargets[i].filterMode = FilterMode.Bilinear;
                _rsmTargets[i].Create();
                //texture.Release();
            }

            _rsmTargetIdentifiers[i] = new RenderTargetIdentifier(_rsmTargets[i]);
        }

        _isInit = true;
    }

    private static int _rsmDepth = Shader.PropertyToID("_RsmDepth");
    private void SetRenderTarget()
    {
        CommandBuffer cmd = CommandBufferPool.Get(bufferName);
        cmd.BeginSample("Set RSM Target");
        cmd.GetTemporaryRT(_rsmDepth, _rsmSize, _rsmSize, 32, FilterMode.Bilinear, RenderTextureFormat.Depth);
        cmd.SetRenderTarget(_rsmTargetIdentifiers, _rsmDepth);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.EndSample("Set RSM Target");
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }
    
    private static int _rsmMatrix = Shader.PropertyToID("_RsmMatrix");
    private static Matrix4x4 _rsmMatrixM = new Matrix4x4();
    
    private static int _reflectiveShadowMap = Shader.PropertyToID("_ReflectiveShadowMap");
    //private static int _BaseColor = Shader.PropertyToID()
    private Shader _shader = Shader.Find("RhythmRP/Rhythm_Shadow_Cast");
    private Material _mat;
    private const string cmdName = "RSMCast";

    // 安排在Shadow Map之后，矩阵不需要重新传，用Shadow Map传入的就可以
    public void Render()
    {
        if (!_isInit) InitRsmTargets();
        SetRenderTarget();
        if (_mat == null) _mat = new Material(_shader);
        
        CommandBuffer cmd = CommandBufferPool.Get(bufferName);
        cmd.BeginSample("Render RSM");
        var allRenderers = RhythmPipeline.PipelineManager.RhythmPipeline.AllRenderers;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i].isVisible&&allRenderers[i].shadowCastingMode == ShadowCastingMode.On)
            {
                var color = allRenderers[i].materials[0].GetColor("_BaseColor");
                // Debug.LogError(color);
                CommandBuffer cmdT = CommandBufferPool.Get();
                //allRenderers[i].materials[0].SetColor("_BaseColor", color);
                // cmdT.SetGlobalConstantBuffer("UnityPerMaterial");
                cmdT.SetGlobalTexture("_MainTex", allRenderers[i].materials[0].GetTexture("_BaseMap"));
                CommandHelper.ExcuteButNotRelease(_context, cmdT);
                cmd.DrawMesh(allRenderers[i].GetComponent<MeshFilter>().sharedMesh, allRenderers[i].GetComponent<Transform>().localToWorldMatrix, _mat, 0, 4);
                CommandHelper.ExecuteAndRelese(_context, cmdT);
                //Debug.Log("第"+i+"个"+allRenderers[i].GetComponent<MeshFilter>().mesh.name);
            }
        }
        
        cmd.EndSample("Render RSM");
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }
    
}
