#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public class DependenciesCreator : EditorWindow {
    const string ASSETS = "Assets";
    const string PREFABS_DATA = "PrefabsData";
    const string ITEM = "Item";
    const string COMPRESSED_DATA = "CompressedData";
    const string COMPRESSED_SCALED_DATA = "CompressedScaledData";
    const string PREFABS_PATH = "Assets/Prefabs/ItemsPrefabs/";

    static int currentIndex;
    static float currentProgress;
    static GameObject[] selectedObjs;

    static GameObject itemTemplate;

    string selectedObjPath;


    void OnGUI() {
        TryToDrawLayout();
    }

    void OnInspectorUpdate() {
        Repaint();
    }

    void TryToDrawItemTemplateObjectField() {
        itemTemplate =
            (GameObject) EditorGUILayout.ObjectField("Item template", itemTemplate, typeof(GameObject), true);
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
            EditorUtility.DisplayProgressBar("Processing...",
                $"{(currentProgress * (100)).ToString(CultureInfo.InvariantCulture)} %", currentProgress);
        else
            EditorUtility.ClearProgressBar();
    }

    static void TryToCreateDependenciesForSelection() {
        if (!HasActiveSelection()) return;

        var selectionCount = selectedObjs.Length;

        for (var i = 0; i < selectionCount; i++) {
            RecalculateProgress(i, selectionCount);
            CreateDependencies(selectedObjs[i]);
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

    static void CreateDependencies(GameObject selectedObj) {
        var selectedObjName = selectedObj.name;
        var selectedObjPath = AssetDatabase.GetAssetPath(selectedObj);

        if (!File.Exists(selectedObjPath)) {
            Debug.LogError("[MaterialInstancesCreator] ==> Can't create dependencies from scene selection.");
            return;
        }

        var selectedObjectDirectoryPath = Path.GetDirectoryName(selectedObjPath);


        var assetDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), selectedObjectDirectoryPath);
        var assetDirectoryTexturesPath = Path.Combine(assetDirectoryPath, $"{PREFABS_DATA}_{selectedObjName}");

        CreateDependencies(selectedObj,
            assetDirectoryTexturesPath);
    }

    static void CreateDependencies(GameObject selectedObj, string assetDirectoryTexturesPath) {
        if (!Directory.Exists(assetDirectoryTexturesPath)) {
            Directory.CreateDirectory(assetDirectoryTexturesPath);
            var compressedDataDirectoryPath = Path.Combine(assetDirectoryTexturesPath, COMPRESSED_DATA);
            var compressedScaledDataDirectoryPath =
                Path.Combine(assetDirectoryTexturesPath, COMPRESSED_SCALED_DATA);
            CreateDirectory(compressedDataDirectoryPath);
            CreateDirectory(compressedScaledDataDirectoryPath);

            CreatePrefabAsset(selectedObj, compressedDataDirectoryPath,compressedScaledDataDirectoryPath);
        }

        AssetDatabase.Refresh();
    }

    static void CreatePrefabAsset(GameObject selectedObj, string compressedDataDirPath, string compressedScaledDirPath) {
        
        var tmpTemplate = Instantiate(itemTemplate);
        var templateTransform = tmpTemplate.transform;
        var selectedName = selectedObj.name;
        tmpTemplate.name = selectedName;

        var originalObjCopy = Instantiate(selectedObj, tmpTemplate.transform.GetChild(0), true);
        originalObjCopy.name = $"{selectedName}_orig";

        SetupChildGameObject(selectedObj, templateTransform, 1, "compressed", compressedDataDirPath);
        SetupChildGameObject(selectedObj, templateTransform, 2, "compressedScaled",
            compressedScaledDirPath);

        var preafabAssetPath = Path.Combine(PREFABS_PATH, $"{ITEM}_{selectedName}.prefab");
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(tmpTemplate, preafabAssetPath);
        DestroyImmediate(tmpTemplate);
    }

    static void SetupChildGameObject(GameObject sourceObj, Transform parent, int childIndex, string namePostFix,
        string materialPath) {
        var newChildName = $"{sourceObj.name}_{namePostFix}";

        var newChild = Instantiate(sourceObj, parent.GetChild(childIndex), true);
        newChild.name = newChildName;

        var childRenderers = newChild.GetComponentsInChildren<Renderer>();
        var sourceRenderers = sourceObj.GetComponentsInChildren<Renderer>();
        
        for (var i = 0; i < sourceRenderers.Length; i++) {
            var copiedMaterials = new Material[sourceRenderers[i].sharedMaterials.Length];

            for (var j = 0; j < copiedMaterials.Length; j++) {
                var sharedMat = sourceRenderers[i].sharedMaterials[j];
                copiedMaterials[j] = new Material(sharedMat);
                CreateMaterialAsset(copiedMaterials[j], Path.Combine(materialPath, $"{sharedMat.name}_{namePostFix}.mat"));
            }

            childRenderers[i].materials = copiedMaterials;
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
        var newMaterialPath = GetRelativeDataPath(path);
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(material, newMaterialPath);
    }
}

#endif