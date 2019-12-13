using System.IO;
using System.Text;
using TinyPng;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class TinyPngUploader : EditorWindow {
    static string tinyPngApiKey;
    static string[] texturesGUIDs;

    static bool isTexturesFoldoutClicked;
    static Vector2 scrollViewPos;

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

    void DrawAPIKeyField() {
        
        tinyPngApiKey = EditorGUILayout.TextField("Tiny PNG API key", tinyPngApiKey);
        tinyPngApiKey = "Dh3sqdPbnmTgvXkxx7l1c1kPrTRg2c0S"; // hardcoded key
    }

    void DrawLayout() {

        EditorGUILayout.BeginVertical();
        DrawAPIKeyField();
        DrawTexturePathsFoldout();
        TryToDrawUploadImgLayout();
        TryToFindAllTextures();
        EditorGUILayout.EndVertical();

    }

    void DrawTexturePathsFoldout() {
        scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
        isTexturesFoldoutClicked = EditorGUILayout.Foldout(isTexturesFoldoutClicked, "Texture asset paths:");
        if (isTexturesFoldoutClicked) {
            GUILayout.Label(GetTexturesPathsString());
        }
        EditorGUILayout.EndScrollView();
    }

    string GetTexturesPathsString() {
        if (texturesGUIDs == null || texturesGUIDs.Length == 0)
            return "No texture paths.";

        var strBuilder = new StringBuilder();
        for (var i = 0; i < texturesGUIDs.Length; i++) {
            strBuilder.Append($"{AssetDatabase.GUIDToAssetPath(texturesGUIDs[i])}\n");
        }

        return strBuilder.ToString();
    }
    
    void TryToDrawUploadImgLayout() {
        if (!GUILayout.Button("Upload img"))
            return;

        UploadImage();
    }

    async void UploadImage() {
        if (string.IsNullOrEmpty(tinyPngApiKey)) {
            Debug.LogError("API Key is required");
            return;
        }

        using (var png = new TinyPngClient(tinyPngApiKey)) {
            //Create a task to compress an image.
            //this gives you the information about your image as stored by TinyPNG
            //they don't give you the actual bits (as you may want to chain this with a resize
            //operation without caring for the originally sized image).
            var compressImageTask =
                png.Compress("C:\\Users\\range\\Desktop\\Textures_Test\\Originals\\FotelGamingowy_red tex albedo.png");

            //If you want to actually save this compressed image off
            //it will need to be downloaded 
            var compressedImage = await compressImageTask.Download();

//            //you can then get the bytes
//            var bytes = await compressedImage.GetImageByteData();
//
//            //get a stream instead
//            var stream = await compressedImage.GetImageByteData();

            //or just save to disk
            await compressedImage.SaveImageToDisk("C:\\Users\\range\\Desktop\\Textures_Test\\test2.png");

            //Putting it all together
//            await png.Compress("path")
//                .Download()
//                .SaveImageToDisk("savedPath");
        }
    }

    void TryToFindAllTextures() {
        if (!GUILayout.Button("Find all textures GUIDs"))
            return;

        texturesGUIDs = AssetDatabase.FindAssets("t: Texture");
    }
}

#endif