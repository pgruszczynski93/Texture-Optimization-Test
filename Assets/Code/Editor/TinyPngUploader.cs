using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TinyPng;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public enum CompressionType {
    DefaultCompression,
    CompressedAndScaled_1024_1024,
}

public class TinyPngUploader : EditorWindow {

    static bool compressAllFiles = true;
    
    static int texturesSelectedToUpload;
    static int foundTexturesCount;
    static int compressFromFileStartIndex;
    static int compressFileRange;
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
        DrawCompressAllToggle();
        SetImgResolution();
        saveTexturesPath = EditorGUILayout.TextField("Save path:", saveTexturesPath);
    }

    void DrawCompressAllToggle() {
        compressAllFiles = EditorGUILayout.Toggle("Compress all textures?: ", compressAllFiles);
        if (!compressAllFiles) {
            compressFromFileStartIndex = EditorGUILayout.IntField("Start from index: ", compressFromFileStartIndex);
            compressFileRange = EditorGUILayout.IntField("How many items to compress (from given index): ", compressFileRange);
        }
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
            EditorGUILayout.BeginScrollView(scrollViewPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        isTexturesFoldoutClicked = EditorGUILayout.Foldout(isTexturesFoldoutClicked, "Texture asset paths:");
        if (isTexturesFoldoutClicked) {
            GUILayout.Label(texturePathsStr);
        }

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
        DrawStatistics();
    }

    void DrawStatistics() {
        var vertLayoutRect = EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"Textures found: {foundTexturesCount}");
        progressValue = downloadProgress / texturesSelectedToUpload;
        EditorGUILayout.Space();
        EditorGUI.ProgressBar(vertLayoutRect, progressValue,
            progressText);
         EditorGUILayout.EndVertical();
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
            strBuilder.Append($"{i+1}:\t{absolutePath}\n");
            ++foundTexturesCount;
        }

        return strBuilder.ToString();
    }

    string GetAbsoluteTexturesPath(string filePath) {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
    }

    void TryToDrawUploadImgLayout() {
        if (!GUILayout.Button("Upload & compress textures"))
            return;

        FindAllTextures();

        if (texturesAbsolutePaths == null || texturesAbsolutePaths.Count == 0) {
            Debug.LogError("Nothing to process!");
            return;
        }
        
        DrawSaveDialog();

        TryToCompressImages();
    }

    void DrawSaveDialog() {
        var defaultName = compressionType == CompressionType.DefaultCompression
            ? "compressed2k_"
            : "compressedScaled1k_";
        saveTexturesPath = EditorUtility.SaveFilePanel("Select texture save destination", "", defaultName, "");
    }

    void TryToCompressImages() {
        if (string.IsNullOrEmpty(tinyPngApiKey)) {
            Debug.LogError("API Key is required");
            return;
        }
        
        downloadProgress = 0;

        if (compressAllFiles) {
            CompressImages(texturesAbsolutePaths);
        }
        else {
            var selectedFiles = texturesAbsolutePaths.GetRange(compressFromFileStartIndex, compressFileRange);
            CompressImages(selectedFiles);
        }
    }

    async void CompressImages(IList<string> selectedFiles) {
        texturesSelectedToUpload = selectedFiles.Count;
        
        using (var png = new TinyPngClient(tinyPngApiKey)) {
            
            for (var i = 0; i < texturesSelectedToUpload; i++) {
                ++downloadProgress;

                var compressImageTask =
                    png.Compress(selectedFiles[i]);

                progressText = $"File {downloadProgress} of {texturesSelectedToUpload} is processing.";

                var compressedImage = await compressImageTask.Download();
                await compressedImage.SaveImageToDisk($"C:\\Users\\range\\Desktop\\Textures_Test\\test{i}.png");
            }

            progressText = "Compression completed";
        }
    }
}

#endif