// ============================================================================
// BBGSpritesBaker.cs
// Black Bart's Gold — Editor Utility to Bake Procedural Sprites as PNGs
// Path: Assets/Editor/BBGSpritesBaker.cs
// ============================================================================
// Opens a window where you can preview all procedural sprites and bake them
// to PNG files in Resources/UI/Sprites/. Baked sprites load faster at runtime
// and can be hand-edited in Photoshop/Krita afterward.
//
// Menu: Black Bart's Gold → Bake UI Sprites
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using BlackBartsGold.UI;

namespace BlackBartsGold.Editor
{
    public class BBGSpritesBaker : EditorWindow
    {
        private const string OutputFolder = "Assets/Resources/UI/Sprites";
        private Vector2 _scrollPos;

        [MenuItem("Black Bart's Gold/Bake UI Sprites")]
        public static void ShowWindow()
        {
            var window = GetWindow<BBGSpritesBaker>("BBG Sprite Baker");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("BBG Procedural Sprite Baker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Preview procedurally generated sprites and bake them as PNG files.\n" +
                "Baked sprites load faster and can be hand-edited afterward.\n" +
                $"Output: {OutputFolder}/",
                MessageType.Info);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("🔥 Bake All Sprites to PNG", GUILayout.Height(36)))
            {
                BakeAll();
            }

            if (GUILayout.Button("🔄 Reload Theme + Regenerate", GUILayout.Height(28)))
            {
                BBGThemeProvider.Reload();
                BBGSprites.Reload();
                Repaint();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawSpritePreview("Button Leather", BBGSprites.ButtonLeather);
            DrawSpritePreview("Button Brass Border", BBGSprites.ButtonBrassBorder);
            DrawSpritePreview("Panel Wood", BBGSprites.PanelWood);
            DrawSpritePreview("Panel Parchment", BBGSprites.PanelParchment);
            DrawSpritePreview("Glow Soft", BBGSprites.GlowSoft);
            DrawSpritePreview("Glow Rect", BBGSprites.GlowRect);
            DrawSpritePreview("Divider Brass", BBGSprites.DividerBrass);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSpritePreview(string label, Sprite sprite)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            if (sprite != null && sprite.texture != null)
            {
                float previewSize = 96f;
                float aspect = sprite.texture.width / (float)sprite.texture.height;
                Rect rect = GUILayoutUtility.GetRect(previewSize * aspect, previewSize);

                EditorGUI.DrawTextureTransparent(rect, sprite.texture, ScaleMode.ScaleToFit);

                EditorGUILayout.LabelField(
                    $"{sprite.texture.width}×{sprite.texture.height}  |  Border: {sprite.border}",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("(not generated yet)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void BakeAll()
        {
            EnsureDirectory(OutputFolder);

            string[] names = BBGSprites.AllSpriteNames;
            int baked = 0;

            foreach (string name in names)
            {
                Texture2D tex = BBGSprites.GetTexture(name);
                if (tex == null)
                {
                    Debug.LogWarning($"[BBGSpritesBaker] Skipping '{name}' — texture is null.");
                    continue;
                }

                string path = $"{OutputFolder}/{name}.png";
                byte[] png = tex.EncodeToPNG();

                if (png == null)
                {
                    Debug.LogWarning($"[BBGSpritesBaker] Failed to encode '{name}' to PNG.");
                    continue;
                }

                File.WriteAllBytes(path, png);
                baked++;
                Debug.Log($"[BBGSpritesBaker] ✅ Baked: {path} ({png.Length / 1024f:F1} KB)");
            }

            AssetDatabase.Refresh();

            foreach (string name in names)
            {
                string path = $"{OutputFolder}/{name}.png";
                ConfigureSpriteImporter(path, name);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[BBGSpritesBaker] 🔥 Done! Baked {baked}/{names.Length} sprites to {OutputFolder}/");
            EditorUtility.DisplayDialog("BBG Sprite Baker",
                $"Baked {baked} sprites to:\n{OutputFolder}/\n\nSprite import settings configured automatically.",
                "Nice!");
        }

        private static void ConfigureSpriteImporter(string assetPath, string spriteName)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            bool isNineSlice = spriteName.StartsWith("btn-") || spriteName.StartsWith("panel-") || spriteName == "glow-rect";
            if (isNineSlice)
            {
                float border = spriteName.StartsWith("btn-") ? 16f :
                               spriteName == "glow-rect" ? 20f : 24f;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteBorder = new Vector4(border, border, border, border);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }

            importer.SaveAndReimport();
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
#endif
