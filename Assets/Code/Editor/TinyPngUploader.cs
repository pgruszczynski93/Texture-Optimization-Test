using System.IO;
using TinyPng;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class TinyPngUploader : EditorWindow {
    static string tinyPngApiKey;
    static string[] texturesGUIDs;

    void OnGUI() {
        DrawAPIKeyField();
        TryToDrawUploadImgLayout();
        TryToFindAllTextures();
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

        Debug.Log(Path.GetDirectoryName(Application.dataPath));
//        texturesGuIDs = AssetDatabase.FindAssets("t: Texture", null);
//
//        foreach (var t in texturesGuIDs) {
//            Debug.Log(AssetDatabase.GUIDToAssetPath(t));
//        }
    }
}

#endif