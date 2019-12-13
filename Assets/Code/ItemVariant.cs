using UnityEngine;

[System.Serializable]
public struct VariantInfo {
    public VariantOption variantOption;
    public float textureSize;
    public Vector2 textureResolution;
}
public enum VariantOption {
    Original,
    Compressed,
    CompressedScaled
}

public class ItemVariant : MonoBehaviour {

    [SerializeField] VariantInfo variantInfo;
    
    public VariantInfo GetVariantInfo() {
        return variantInfo;
    }
}