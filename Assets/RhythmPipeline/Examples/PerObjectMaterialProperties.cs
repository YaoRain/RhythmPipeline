using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] 
    Color _baseColor = Color.green;

    private static MaterialPropertyBlock mbp;

    private void OnValidate()
    {
        if (mbp == null)
        {
            mbp = new MaterialPropertyBlock();
        }
        mbp.SetColor(baseColorId, _baseColor);
        
        GetComponent<Renderer>().SetPropertyBlock(mbp);
    }

    private void Awake()
    {
        // OnValidate();
    }
}
