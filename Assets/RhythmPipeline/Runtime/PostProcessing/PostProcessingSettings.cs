using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum PostProcessingFeatureType
{
    ToneMapping = 100,
    Bloom = 2,
    FinalBlit = 1000
};

[CreateAssetMenu(menuName = "Rendering/Custom Post Processing Settings")]
public class PostProcessingSettings : ScriptableObject
{
    [SerializeField] 
    public List<PostProcessingFeatureType> postFeatures = new List<PostProcessingFeatureType>();
    
    public PostProcessingFeature PostProcessingFeatureCreater(PostProcessingFeatureType featureTpye)
    {
        switch (featureTpye)
        {
            case PostProcessingFeatureType.FinalBlit:
                return new FinalBlit(featureTpye);
            case PostProcessingFeatureType.ToneMapping:
                return new ToneMapping(featureTpye);
            case PostProcessingFeatureType.Bloom:
                return new Bloom(featureTpye);
            default:
                return null;
        }
    }
}
