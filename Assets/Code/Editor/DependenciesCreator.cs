#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public class DependenciesCreator : EditorWindow {
    const string COMPRESSED_MAT = "compressed_mat.mat";
    const string COMPRESSED_SCALED_MAT = "compressed_scaled_mat.mat";
    const string ASSETS = "Assets";
    const string TEXTURES = "Textures";
    const string ITEM = "Item";
    const string COMPRESSED_TEXTURES = "CompressedData";
    const string COMPRESSED_SCALED_TEXTURES = "CompressedScaledData";
    const string PREFABS_PATH = "Assets/Prefabs/ItemsPrefabs/";

    static int currentIndex;
    static float currentProgress;
    static GameObject[] selectedObjs;

    static GameObject itemTemplate;
    
    string selectedObjPath;
    
    
    void OnGUI() {
        TryToDrawLayout();
    }
    
    void OnInspectorUpdate()
    {
        Repaint();
    }

    void TryToDrawItemTemplateObjectField() {
        itemTemplate = (GameObject)EditorGUILayout.ObjectField("Item template", itemTemplate, typeof(GameObject), true);
    }
    
    [MenuItem("Testing/Material Instance Creator")]
    static void DrawWindow() {
        var window = GetWindow<DependenciesCreator>();
        window.Show();
    }

    void TryToDrawLayout() {
        TryToDrawItemTemplateObjectField();
        
        EditorGUILayout.LabelField($"Selected prefabs: {Selection.gameObjects.Length}");

        if (!GUILayout.Button("Create dependencies for prefabs.")) 
            return;
        
        if (!HasActiveSelection()) 
            return;

        TryToCreateDependenciesForSelection();
    }

    static void TryToUpdateProgressBar() {
        if (currentProgress < 1.0f)
            EditorUtility.DisplayProgressBar("Processing...", $"{(currentProgress * (100)).ToString(CultureInfo.InvariantCulture)} %", currentProgress);
        else
            EditorUtility.ClearProgressBar();
    }

    static void TryToCreateDependenciesForSelection() {
        if (!HasActiveSelection()) return;

        var shaderSource = Shader.Find("Standard");
        var selectionCount = selectedObjs.Length;
        
        for (var i = 0; i < selectionCount; i++) {
            RecalculateProgress(i, selectionCount);
            CreateDependencies(selectedObjs[i], shaderSource);
        }
    }

    static void RecalculateProgress(int i, int selectionCount) {
        currentIndex = i + 1;
        currentProgress = (float) currentIndex / selectionCount;
        TryToUpdateProgressBar();
    }

    static bool HasActiveSelection() {
        selectedObjs = Selection.gameObjects;
        if (selectedObjs != null && selectedObjs.Length != 0)
            return true;
        
        Debug.LogError("No objects created.");
        return false;

    }

    static void CreateDependencies(GameObject selectedObj, Shader shaderSource) {
        var selectedObjName = selectedObj.name;
        var selectedCompressedMatName = ($"{selectedObjName}_{COMPRESSED_MAT}");
        var selectedCompressedScaledMatName = ($"{selectedObjName}_{COMPRESSED_SCALED_MAT}");
        var selectedObjPath = AssetDatabase.GetAssetPath(selectedObj);
        
        if (!File.Exists(selectedObjPath)) {
            Debug.LogError("[MaterialInstancesCreator] ==> Can't create dependencies from scene selection.");
            return;
        }
        var selectedObjectDirectoryPath = Path.GetDirectoryName(selectedObjPath);

        var materialForCompressedTexture = new Material(shaderSource);
        var materialForCompressedScaledTexture = new Material(shaderSource);

        var assetDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), selectedObjectDirectoryPath);
        var assetDirectoryTexturesPath = Path.Combine(assetDirectoryPath, $"{TEXTURES}_{selectedObjName}");

        CreateDependencies(selectedObj, 
            assetDirectoryTexturesPath,
            materialForCompressedTexture, 
            selectedCompressedMatName, 
            materialForCompressedScaledTexture,
            selectedCompressedScaledMatName);

    }

    static void CreateDependencies(GameObject selectedObj, string assetDirectoryTexturesPath, Material materialForCompressedTexture,
        string selectedCompressedMatName, Material materialForCompressedScaledTexture,
        string selectedCompressedScaledMatName) {
        
        if (!Directory.Exists(assetDirectoryTexturesPath)) {
            Directory.CreateDirectory(assetDirectoryTexturesPath);
            var compressedTextureDirectoryPath = Path.Combine(assetDirectoryTexturesPath, COMPRESSED_TEXTURES);
            var compressedScaledTextureDirectoryPath =
                Path.Combine(assetDirectoryTexturesPath, COMPRESSED_SCALED_TEXTURES);
            CreateDirectory(compressedTextureDirectoryPath);
            CreateDirectory(compressedScaledTextureDirectoryPath);

            CreateMaterialAsset(materialForCompressedTexture,
                Path.Combine(compressedTextureDirectoryPath, selectedCompressedMatName));
            CreateMaterialAsset(materialForCompressedScaledTexture,
                Path.Combine(compressedScaledTextureDirectoryPath, selectedCompressedScaledMatName));
            
            CreatePrefabAsset(selectedObj, materialForCompressedTexture, materialForCompressedScaledTexture);
        }
        
        AssetDatabase.Refresh();
    }

    static void CreatePrefabAsset(GameObject selectedObj, Material materialForCompresed, Material materialForCompressedScaled) {
        var tmpGO = Instantiate(itemTemplate);
        var tmpTransform = tmpGO.transform;
        var selectedName = selectedObj.name;
        
        var originalChild = Instantiate(selectedObj, tmpGO.transform.GetChild(0), true);
        originalChild.name = $"{selectedName}_orig";
        
        SetupChildGameObject(selectedObj, tmpTransform, materialForCompresed, 1, "compressed");
        SetupChildGameObject(selectedObj, tmpTransform, materialForCompressedScaled, 2, "compressedScaled");
        
        var preafabAssetPath = Path.Combine(PREFABS_PATH, $"{ITEM}_{selectedName}.prefab");
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(tmpGO, preafabAssetPath);
        DestroyImmediate(tmpGO);
    }

    static void SetupChildGameObject(GameObject sourceObj, Transform parent, Material newMaterial, int childIndex, string namePostFix) {
        var newChildName = $"{sourceObj.name}_{namePostFix}";
        
        var newChild = Instantiate(sourceObj,  parent.GetChild(childIndex), true);
        newChild.name = newChildName;

        var allRenderers = newChild.GetComponentsInChildren<Renderer>();
        for (var i = 0; i < allRenderers.Length; i++) {
            var materials = allRenderers[i].sharedMaterials;

            for (var j = 0; j < materials.Length; j++) {
                materials[j] = newMaterial;
            }

            allRenderers[i].materials = materials;
        }

    }
    
    
    static string GetRelativeDataPath(string path) {
        return ASSETS + path.Substring(Application.dataPath.Length);
    }

    static void CreateDirectory(string path) {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static void CreateMaterialAsset(Material material, string path) {
        var materialPath = GetRelativeDataPath(path);
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(material, materialPath);
    }
}

#endif