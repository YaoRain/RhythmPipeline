
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GISettings
{
    
    [System.Serializable]
    public enum GIType
    {
        RSM, LPV, DDGI
    }

    public GIType giType = GIType.RSM;
}
