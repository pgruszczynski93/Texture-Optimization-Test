using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TinyPng;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

#if UNITY_EDITOR

public enum CompressionType {
    DefaultCompression,
    CompressedAndScaled_1024_1024,
}

public class TinyPngUploader : EditorWindow {
    static int texturesSelectedToUpload;
    static int foundTexturesCount;
    static float downloadProgress;
    static float progressValue;

    static CompressionType compressionType;

    static string tinyPngApiKey;
    static string texturePathsStr;
    static string progressText;
    static string saveTexturesPath;
    static string[] texturesGUIDs;
    static List<string> texturesAbsolutePaths;

    static bool isTexturesFoldoutClicked;
    static Vector2 scrollViewPos;
    static Vector2 imgResolution;

    void OnGUI() {
        DrawLayout();
    }

    void OnInspectorUpdate() {
        Repaint();
    }

    [MenuItem("Testing/TinyPNG Uploader")]
    static void DrawWindow() {
        var window = GetWindow<TinyPngUploader>();
        window.Show();
    }

    void SetImgResolution() {
        switch (compressionType) {
            case CompressionType.CompressedAndScaled_1024_1024:
                imgResolution = new Vector2(1024, 1024);
                break;
            case CompressionType.DefaultCompression:
                imgResolution = new Vector2(2048, 2048);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void DrawInputFields() {
        tinyPngApiKey = EditorGUILayout.TextField("Tiny PNG API key", tinyPngApiKey);
        tinyPngApiKey = "Dh3sqdPbnmTgvXkxx7l1c1kPrTRg2c0S"; // hardcoded key
        
        compressionType = (CompressionType) EditorGUILayout.EnumPopup("Compression type: ", compressionType);
        imgResolution = EditorGUILayout.Vector2Field("Width & height", imgResolution);
        SetImgResolution();
        saveTexturesPath = EditorGUILayout.TextField("Save path:", saveTexturesPath);
    }

    void DrawLayout() {
        EditorGUILayout.BeginVertical();
        DrawInputFields();
        DrawTexturePathsFoldout();
        TryToFindAllTextures();
        TryToDrawUploadImgLayout();
        EditorGUILayout.EndVertical();
    }

    void DrawTexturePathsFoldout() {
        scrollViewPos =
            EditorGUILayout.BeginScrollView(scrollViewPos, GUILayout.ExpandWidth(true), GUILayout.Height(250));
        isTexturesFoldoutClicked = EditorGUILayout.Foldout(isTexturesFoldoutClicked, "Texture asset paths:");
        if (isTexturesFoldoutClicked) {
            GUILayout.Label(texturePathsStr);
        }

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
        DrawStatistics();
    }

    void DrawStatistics() {
        DrawSpaces(6);
        EditorGUILayout.LabelField($"Textures found: {foundTexturesCount}");
        progressValue = downloadProgress / texturesSelectedToUpload;
        EditorGUILayout.Space();
        EditorGUI.ProgressBar(new Rect(3, 310, position.width - 6, 20), progressValue,
            progressText);
    }

    void DrawSpaces(int spaces) {
        for (var i = 0; i < spaces; i++) {
            EditorGUILayout.Space();
        }
    }

    void TryToFindAllTextures() {
        if (!GUILayout.Button("Find all textures GUIDs"))
            return;

        FindAllTextures();
    }

    void FindAllTextures() {
        foundTexturesCount = 0;
        texturesAbsolutePaths = new List<string>();
        texturesGUIDs = AssetDatabase.FindAssets("t: Texture");
        texturePathsStr = GetTexturesPathsString();
    }
    
    string GetTexturesPathsString() {
        if (texturesGUIDs == null || texturesGUIDs.Length == 0)
            return "No texture paths.";

        var strBuilder = new StringBuilder();
        for (var i = 0; i < texturesGUIDs.Length; i++) {
            var filePath = AssetDatabase.GUIDToAssetPath(texturesGUIDs[i]);
            if (!filePath.Contains(".jpg") && !filePath.Contains(".png"))
                continue;

            var absolutePath = GetAbsoluteTexturesPath(filePath);
            texturesAbsolutePaths.Add(absolutePath);
            strBuilder.Append($"{absolutePath}\n");
            ++foundTexturesCount;
        }

        return strBuilder.ToString();
    }

    string GetAbsoluteTexturesPath(string filePath) {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
    }

    void TryToDrawUploadImgLayout() {
        if (!GUILayout.Button("Upload textures"))
            return;

        FindAllTextures();

        if (texturesAbsolutePaths == null || texturesAbsolutePaths.Count == 0) {
            Debug.LogError("Nothing to upload!");
            return;
        }
        
        DrawSaveDialog();

        UploadImage();
    }

    void DrawSaveDialog() {
        saveTexturesPath = EditorUtility.SaveFilePanel("Select texture save destination", "", "", "");
    }

    async void UploadImage() {
        if (string.IsNullOrEmpty(tinyPngApiKey)) {
            Debug.LogError("API Key is required");
            return;
        }
        
        downloadProgress = 0;
        var testList = texturesAbsolutePaths.GetRange(3, 4);
        texturesSelectedToUpload = 4;

        using (var png = new TinyPngClient(tinyPngApiKey)) {
            for (var i = 0; i < testList.Count; i++) {
                ++downloadProgress;

                var compressImageTask =
                    png.Compress(testList[i]);

                progressText = $"File {downloadProgress} of {texturesSelectedToUpload} is processing.";

                var compressedImage = await compressImageTask.Download();
                await compressedImage.SaveImageToDisk($"C:\\Users\\range\\Desktop\\Textures_Test\\test{i}.png");
            }

            progressText = "Compression completed";
        }
    }
}

#endif