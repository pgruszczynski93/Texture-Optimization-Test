﻿#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public class MaterialInstancesCreator : EditorWindow {
    const string COMPRESSED_MAT = "compressed_mat.mat";
    const string COMPRESSED_SCALED_MAT = "compressed_scaled_mat.mat";
    const string ASSETS = "Assets";
    const string TEXTURES = "Textures";
    const string COMPRESSED_TEXTURES = "CompressedData";
    const string COMPRESSED_SCALED_TEXTURES = "CompressedScaledData";

    string selectedObjPath;

    [MenuItem("Assets/Material Instance Creator")]
    public static void TryToCreateMaterialsForSelection() {
        var selectedObjs = Selection.gameObjects;
        if (selectedObjs == null || selectedObjs.Length == 0) {
            Debug.LogError("No objects created.");
            return;
        }

        var shaderSource = Shader.Find("Standard");

        for (var i = 0; i < selectedObjs.Length; i++) {
            CreateDependencies(selectedObjs[i], shaderSource);
        }
    }

    static void CreateDependencies(GameObject selectedObj, Shader shaderSource) {
        var selectedObjName = selectedObj.name;
        var selectedCompressedMatName = ($"{selectedObjName}_{COMPRESSED_MAT}");
        var selectedCompressedScaledMatName = ($"{selectedObjName}_{COMPRESSED_SCALED_MAT}");
        var selectedObjPath = AssetDatabase.GetAssetPath(selectedObj);
        var selectedObjectDirectoryPath = Path.GetDirectoryName(selectedObjPath);

        var materialForCompressedTexture = new Material(shaderSource);
        var materialForCompressedScaledTexture = new Material(shaderSource);

        var assetDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), selectedObjectDirectoryPath);
        var assetDirectoryTexturesPath = Path.Combine(assetDirectoryPath, $"{TEXTURES}_{selectedObjName}");

        CreateData(assetDirectoryTexturesPath, materialForCompressedTexture, selectedCompressedMatName, materialForCompressedScaledTexture, selectedCompressedScaledMatName);

    }

    static void CreateData(string assetDirectoryTexturesPath, Material materialForCompressedTexture,
        string selectedCompressedMatName, Material materialForCompressedScaledTexture,
        string selectedCompressedScaledMatName) {
        
        if (!Directory.Exists(assetDirectoryTexturesPath)) {
            Directory.CreateDirectory(assetDirectoryTexturesPath);
            var compressedTextureDirectoryPath = Path.Combine(assetDirectoryTexturesPath, COMPRESSED_TEXTURES);
            var compressedScaledTextureDirectoryPath =
                Path.Combine(assetDirectoryTexturesPath, COMPRESSED_SCALED_TEXTURES);
            CreateDirectory(compressedTextureDirectoryPath);
            CreateDirectory(compressedScaledTextureDirectoryPath);

            CreateMaterial(materialForCompressedTexture,
                Path.Combine(compressedTextureDirectoryPath, selectedCompressedMatName));
            CreateMaterial(materialForCompressedScaledTexture,
                Path.Combine(compressedScaledTextureDirectoryPath, selectedCompressedScaledMatName));
        }
        
        AssetDatabase.Refresh();
    }

    static string GetRelativeDataPath(string path) {
        return ASSETS + path.Substring(Application.dataPath.Length);
    }

    static void CreateDirectory(string path) {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static void CreateMaterial(Material material, string path) {
        var materialPath = GetRelativeDataPath(path);
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(material, materialPath);
    }
}

#endif