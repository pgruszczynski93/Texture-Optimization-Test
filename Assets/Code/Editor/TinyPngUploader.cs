using System.Collections.Generic;
using System.IO;
using System.Text;
using TinyPng;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

#if UNITY_EDITOR

public class TinyPngUploader : EditorWindow {
    static string tinyPngApiKey;
    static string texturePathsStr;
    static string[] texturesGUIDs;
    static List<string> texturesAbsolutePaths;

    static bool isTexturesFoldoutClicked;
    static bool areTexturesFound;
    static int foundTexturesCount;
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

    void DrawInputFields() {
        
        tinyPngApiKey = EditorGUILayout.TextField("Tiny PNG API key", tinyPngApiKey);
        tinyPngApiKey = "Dh3sqdPbnmTgvXkxx7l1c1kPrTRg2c0S"; // hardcoded key
        imgResolution = EditorGUILayout.Vector2Field("Width & height", imgResolution);
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
        scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, GUILayout.ExpandWidth(true), GUILayout.Height(300));
        isTexturesFoldoutClicked = EditorGUILayout.Foldout(isTexturesFoldoutClicked, "Texture asset paths:");
        if (isTexturesFoldoutClicked) {
            GUILayout.Label(texturePathsStr);
        }
        EditorGUILayout.LabelField($"Textures found: {foundTexturesCount}");
        EditorGUILayout.EndScrollView();
    }

    void TryToFindAllTextures() {
        if (!GUILayout.Button("Find all textures GUIDs"))
            return;

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

        areTexturesFound = true;

        return strBuilder.ToString();
    }

    string GetAbsoluteTexturesPath(string filePath) {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
    }
    
    void TryToDrawUploadImgLayout() {
        if (!GUILayout.Button("Upload textures"))
            return;

        if (texturesAbsolutePaths == null || texturesAbsolutePaths.Count == 0) {
            Debug.LogError("Nothing to upload!");
            return;
        }
        
        UploadImage();
    }

    async void UploadImage() {
        if (string.IsNullOrEmpty(tinyPngApiKey)) {
            Debug.LogError("API Key is required");
            return;
        }

        using (var png = new TinyPngClient(tinyPngApiKey)) {
            var compressImageTask =
                png.Compress("C:\\Users\\range\\Desktop\\Textures_Test\\Originals\\FotelGamingowy_red tex albedo.png");

            var compressedImage = await compressImageTask.Download();
            await compressedImage.SaveImageToDisk("C:\\Users\\range\\Desktop\\Textures_Test\\test2.png");

        }
    }
}

#endif