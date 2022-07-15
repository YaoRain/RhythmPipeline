using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LPV_TEST : MonoBehaviour
{
    public GameObject cube;
    public Vector3 size = new Vector3(100, 30, 100);
    private static Vector3Int cellNum = new Vector3Int(2, 2, 2);
    
    private LpvCell[,,] lpvCells = new LpvCell[cellNum.x,cellNum.y,cellNum.z];
    // Start is called before the first frame update
    void Start()
    {
        var lpvBox = GetComponent<BoxCollider>();
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
        
        // 这里i,j,k作为3D Texture的UV
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Vector3 VecMult(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
    private Vector3 VecDivide(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
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
            Instantiate(cellObj);
        }
    }

    // 给定RSM 上 VPL 的WorldPos，获取它在3D Texture中的索引
    private Vector3Int GetVolumeIndex(Vector3 worldPos)
    {
        Vector3Int ans = new Vector3Int();
        
        return ans;
    }


    private void SampleRsmAndSaveSH(int i, int j, int k)
    {
        // 将cell变换到rsm的space，计算出uv，然后采样周围的点。
        // 对于采样到的每个VPL，计算到cell center的距离，如果在cell内，则加权累计
    }

    // 光照注入
    private void LightInject()
    {
        for (int i = 0; i < cellNum.x; i++)
        {
            for (int j = 0; j < cellNum.y; j++)
            {
                for (int k = 0; k < cellNum.z; k++)
                {
                    SampleRsmAndSaveSH(i, j, k);
                }
            }
        }

    }
    
    // 光照注入的两个角度：1 在Cell位置采样RSM， 2 遍历RSM，计算出每个VPL在Cell中的位置，并贡献到对应Cell
}
