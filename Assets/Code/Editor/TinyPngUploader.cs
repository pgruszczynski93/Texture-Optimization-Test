using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TinyPng;
using TinyPng.ResizeOperations;
using TinyPng.Responses;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public enum CompressionType {
    DefaultCompression,
    CompressedAndScaled_1024_1024,
}

public class TinyPngUploader : EditorWindow {
    const string DEFAULT_SAVE_DIR_NAME = "SavedTextures";
    const string COMPRESSED = "Compressed2k";
    const string COMPRESSED_SCALED = "CompressedScaled1k";
    const string defaultSearchFilter = "t: Texture";
    const string defaultTinyPngApiKey = "Dh3sqdPbnmTgvXkxx7l1c1kPrTRg2c0S";
    const string defaultSearchDirectory = "Assets";

    bool compressAllFiles;

    int texturesSelectedToUpload;
    int foundTexturesCount;
    int compressFromFileStartIndex;
    int compressFileRange;
    float downloadProgress;
    float progressValue;

    CompressionType compressionType;

    string tinyPngApiKey;
    string texturePathsStr;
    string progressText;
    string searchFilter;
    string saveTexturesRootPath;
    string defaultSaveDestination;
    string[] searchDirectories;
    string[] texturesGUIDs;
    List<string> texturesAbsolutePaths;

    bool isTexturesFoldoutClicked;
    Vector2 scrollViewPos;
    Vector2Int imgResolution;

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
                imgResolution = new Vector2Int(1024, 1024);
                break;
            case CompressionType.DefaultCompression:
                imgResolution = new Vector2Int(2048, 2048);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void DrawInputFields() {
        tinyPngApiKey = EditorGUILayout.TextField("Tiny PNG API key",
            string.IsNullOrEmpty(tinyPngApiKey) ? defaultTinyPngApiKey : tinyPngApiKey);

        compressionType = (CompressionType) EditorGUILayout.EnumPopup("Compression type: ", compressionType);
        imgResolution = EditorGUILayout.Vector2IntField("Width & height", imgResolution);
        searchFilter = EditorGUILayout.TextField("Search Filter (default directory is \'Assets\': ",
            string.IsNullOrEmpty(searchFilter) ? defaultSearchFilter : searchFilter);
        DrawArrayOfTextureDirs();
        DrawCompressAllToggle();
        SetImgResolution();
        saveTexturesRootPath = EditorGUILayout.TextField("Save path:", saveTexturesRootPath);
    }

    void DrawArrayOfTextureDirs() { }

    void DrawCompressAllToggle() {
        compressAllFiles = EditorGUILayout.Toggle("Compress all textures?: ", compressAllFiles);
        if (!compressAllFiles) {
            compressFromFileStartIndex = EditorGUILayout.IntField("Start from index: ", compressFromFileStartIndex);
            compressFileRange =
                EditorGUILayout.IntField("How many items to compress (from given index): ", compressFileRange);
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

    void TryToFindAllTextures() {
        if (!GUILayout.Button("Find all textures GUIDs"))
            return;

        FindAllTextures();
    }

    void FindAllTextures() {
        foundTexturesCount = 0;
        texturesAbsolutePaths = new List<string>();
        texturesGUIDs = AssetDatabase.FindAssets(searchFilter,
            (searchDirectories == null || searchDirectories.Length == 0) ? new[] {defaultSearchDirectory} : searchDirectories);
        texturePathsStr = GetTexturesPathsString();
    }

    string GetTexturesPathsString() {
        if (texturesGUIDs == null || texturesGUIDs.Length == 0)
            return "No texture paths.";

        var strBuilder = new StringBuilder();
        for (var i = 0; i < texturesGUIDs.Length; i++) {
            var filePath = AssetDatabase.GUIDToAssetPath(texturesGUIDs[i]);
            if (!IsTexturePathValid(filePath))
                continue;

            var absolutePath = GetAbsoluteTexturesPath(filePath);
            texturesAbsolutePaths.Add(absolutePath);
            strBuilder.Append($"{i + 1}:\t{absolutePath}\n");
            ++foundTexturesCount;
        }

        return strBuilder.ToString();
    }

    bool IsTexturePathValid(string filepath) {
        return filepath.Contains(".jpg") || filepath.Contains(".png");
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

        TryToSaveAndCompress();
    }

    void TryToSaveAndCompress() {
        saveTexturesRootPath = Path.Combine(Application.persistentDataPath, DEFAULT_SAVE_DIR_NAME);
        if (!File.Exists(saveTexturesRootPath))
            Directory.CreateDirectory(saveTexturesRootPath);

        saveTexturesRootPath = EditorUtility.SaveFilePanel("Select texture save destination",
            Application.persistentDataPath, "SKIP_THIS_FIELD", "");

        if (string.IsNullOrEmpty(saveTexturesRootPath)) {
            Debug.LogError("Invalid save path - compression cancelled");
            return;
        }

        TryToCompressImages();
    }

    void TryToCompressImages() {
        if (string.IsNullOrEmpty(tinyPngApiKey)) {
            Debug.LogError("API Key is required");
            return;
        }

        downloadProgress = 0;

        if (compressAllFiles) {
            CompressImagesAsync(texturesAbsolutePaths);
        }
        else {
            if (compressFromFileStartIndex == 0 || compressFileRange == 0) {
                Debug.LogError("Invalid start index & range - compression cancelled");
                return;
            }

            var selectedFiles = texturesAbsolutePaths.GetRange(compressFromFileStartIndex, compressFileRange);
            CompressImagesAsync(selectedFiles);
        }
    }

    string GetPrefix() {
        return compressionType == CompressionType.DefaultCompression
            ? COMPRESSED
            : COMPRESSED_SCALED;
    }

    async void CompressImagesAsync(IList<string> selectedFiles) {
        using (var png = new TinyPngClient(tinyPngApiKey)) {
            string currentFilePath;
            string saveTextureFullPath;

            var prefix = GetPrefix();
            var fitOperation = new FitResizeOperation(imgResolution.x, imgResolution.y);

            saveTexturesRootPath = Path.GetDirectoryName(saveTexturesRootPath);
            texturesSelectedToUpload = selectedFiles.Count;

            for (var i = 0; i < texturesSelectedToUpload; i++) {
                ++downloadProgress;

                currentFilePath = selectedFiles[i];

                progressText = $"File {downloadProgress} of {texturesSelectedToUpload} is processing.";
                saveTextureFullPath =
                    Path.Combine(saveTexturesRootPath, $"{prefix}_{Path.GetFileName(currentFilePath)}");

                var compressImageTask =
                    png.Compress(currentFilePath);

                if (compressionType == CompressionType.DefaultCompression) {
                    await DownloadAndSaveTask(compressImageTask, saveTextureFullPath);
                }
                else {
                    await ResizeAndSaveTask(compressImageTask, saveTextureFullPath, fitOperation);
                }
            }

            progressText = "Compression completed";
        }
    }

    async Task ResizeAndSaveTask(Task<TinyPngCompressResponse> compressImageTask, string saveTexturePath,
        FitResizeOperation resizeOperation) {
        var compressedImage = await compressImageTask.Resize(resizeOperation);
        await compressedImage.SaveImageToDisk(saveTexturePath);
    }

    async Task DownloadAndSaveTask(Task<TinyPngCompressResponse> compressImageTask, string saveTexturePath) {
        var compressedImage = await compressImageTask.Download();
        await compressedImage.SaveImageToDisk(saveTexturePath);
    }
}

#endif