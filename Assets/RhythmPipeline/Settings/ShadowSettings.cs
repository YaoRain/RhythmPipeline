using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    [Min(0f)] 
    public float maxDistance = 100f;
    
    public enum TextureSize
    {
        _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048
    }
    
    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
    }

    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024
    };

    [System.Serializable]
    public enum ShadowType
    {
        ManyLight, VSM, ESM, PCF
    }

    public ShadowType shadowType = ShadowType.VSM;
}
