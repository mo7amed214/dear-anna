using System.IO;
using UnityEditor;
using UnityEngine;

// Menu-driven entry point for dropping an image into a picture frame/photo object, via a file
// picker instead of dragging an asset into the FramePicture component's Inspector field. Both
// paths end up calling FramePictureCore.PlaceImage, which does the actual measuring/fitting.
public static class FramePictureTool
{
    [MenuItem("Tools/Frame Picture/Place Image On Selected Frame")]
    public static void PlaceImageOnSelected()
    {
        GameObject frame = Selection.activeGameObject;
        if (frame == null)
        {
            Debug.LogError("Select a frame/painting/photo GameObject in the scene first.");
            return;
        }

        string sourcePath = EditorUtility.OpenFilePanel("Choose image for " + frame.name, "", "jpg,jpeg,png,webp");
        if (string.IsNullOrEmpty(sourcePath)) return;

        string ext = Path.GetExtension(sourcePath);
        if (ext.ToLower() == ".webp")
        {
            Debug.LogError("Unity can't import .webp directly -- convert it to .png or .jpg first, then run this again.");
            return;
        }

        string destDir = "Assets/Textures/DreamExhibits";
        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
        string destPath = destDir + "/" + frame.name + "_Image" + ext;
        File.Copy(sourcePath, destPath, true);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
        if (importer != null)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
        if (tex == null)
        {
            Debug.LogError("Failed to import " + destPath + " as a texture.");
            return;
        }

        var fp = frame.GetComponent<FramePicture>();
        if (fp == null) fp = frame.AddComponent<FramePicture>();
        fp.image = tex;
        fp.Apply();
    }
}
