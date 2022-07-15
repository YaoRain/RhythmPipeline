using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class LPV
{
    private const string bufferName = "LPV";
    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = bufferName
    };

    
    private ScriptableRenderContext _context;
    public GameObject cube = GameObject.Find("LpvCube");
    public Vector3 size = new Vector3(100, 30, 100);
    private static Vector3Int cellNum = new Vector3Int(2, 2, 2);
    
    private LpvCell[,,] lpvCells = new LpvCell[cellNum.x,cellNum.y,cellNum.z];
    
    public void SetPerFrame(ref ScriptableRenderContext context)
    {
        _context = context;
    }
    
    // Start is called before the first frame update
    

    private bool _isInit = false;
    public void CreateLPV()
    {
        if(!_isInit) Init();
        LightInject();
        
    }

    public ComputeShader lightInject;
    public static int injectKernel;

    private  RenderTexture lpvTex;
    // LightInject, 使用Compute Shader, 每个Cell一个线程，采样cell内的RSM，把结果投影到SH。
    private void LightInject()
    {
        if (lpvTex == null)
        {
            RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor();
            rtDescriptor.dimension = TextureDimension.Tex3D;
            rtDescriptor.width = cellNum.x;
            rtDescriptor.height = cellNum.y;
            rtDescriptor.volumeDepth = cellNum.z;
            rtDescriptor.graphicsFormat = GraphicsFormat.R8G8B8_UNorm;
            rtDescriptor.colorFormat = RenderTextureFormat.Default;
            rtDescriptor.msaaSamples = 1;
            lpvTex = new RenderTexture(rtDescriptor);
        }
        
        lpvTex.enableRandomWrite = true;
        if (!lpvTex.IsCreated()) lpvTex.Create();
        
        lightInject = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RhythmPipeline/Shaders/GI/LightInject.compute");
        injectKernel = lightInject.FindKernel("LIGHT_INJECT");
        
        var cmd = CommandBufferPool.Get(bufferName);
        cmd.BeginSample("LightInject");
        cmd.SetComputeTextureParam(lightInject, injectKernel, "_WorldPos", RSM._rsmTargets[0]);
        cmd.SetComputeTextureParam(lightInject, injectKernel, "_Flux", RSM._rsmTargets[1]);
        cmd.SetComputeTextureParam(lightInject, injectKernel, "_WorldNormal", RSM._rsmTargets[2]);
        cmd.SetComputeTextureParam(lightInject, injectKernel, "LpvTex", lpvTex);
        cmd.DispatchCompute(lightInject, injectKernel, cellNum.x, cellNum.y, cellNum.z);
        cmd.EndSample("LightInject");
        CommandHelper.ExecuteAndRelese(_context, cmd);
    }
    private Vector3 VecMult(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
    private Vector3 VecDivide(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
    }

    
    private void Init()
    {
        var lpvBox = RhythmPipeline.PipelineManager.RhythmPipeline.LPVBOX.GetComponent<BoxCollider>();
        size = lpvBox.size;
        var aabbMin = lpvBox.bounds.min;
        
        LpvCell.size = VecDivide(size, cellNum);
        cube.transform.localScale = LpvCell.size;
        
        for (int i = 0; i < cellNum.x; i++)
        {
            for (int j = 0; j < cellNum.y; j++)
            {
                for (int k = 0; k < cellNum.z; k++)
                {
                    Vector3 cPos = aabbMin + LpvCell.size / 2f + VecMult(LpvCell.size, new Vector3(i, j, k));
                    lpvCells[i, j, k] = new LpvCell(cPos);
                    lpvCells[i, j, k].SetCellObj(cube);
                }
            }
        }
        _isInit = true;
        // 这里i,j,k作为3D Texture的UV
    }
    private class LpvCell
    {
        public Vector3 centerPos;
        public static Vector3 size;

        public GameObject cellObj;
        public LpvCell(Vector3 cPos)
        {
            centerPos = cPos;
        }

        public void SetCellObj(GameObject obj)
        {
            cellObj = obj;
            cellObj.transform.localScale = size;
            cellObj.transform.position = centerPos;
            // GameObject.Instantiate(cellObj);
        }
    }
}
