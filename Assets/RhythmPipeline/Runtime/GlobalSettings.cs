using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSettings 
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
}
