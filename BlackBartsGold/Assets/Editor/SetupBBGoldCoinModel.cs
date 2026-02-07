// SetupBBGoldCoinModel.cs - Black Bart's Gold
// One-time setup: Replace Quad mesh in BBGoldCoin prefab with 3D coin model from FBX.
// Also creates a proper PBR material from the FBX textures (basecolor, metallic, normal, roughness).
// Path: Assets/Editor/SetupBBGoldCoinModel.cs

using UnityEditor;
using UnityEngine;
using System.IO;

public static class SetupBBGoldCoinModel
{
    const string CoinPrefabPath = "Assets/Prefabs/Coins/BBGoldCoin.prefab";
    const string CoinModelPath = "Assets/Models/BBGoldCoin/tripo_convert_b164cab0-7606-4122-b90e-46f216946ee1.fbx";
    const string TextureFolder  = "Assets/Models/BBGoldCoin/tripo_convert_b164cab0-7606-4122-b90e-46f216946ee1.fbm";
    const string CoinMaterialFolder = "Assets/Materials/Coins";
    const string CoinMaterialPath   = "Assets/Materials/Coins/BBGoldCoin3D.mat";

    // ─────────────────────────────────────────────────────────────
    // Menu: Black Barts Gold ▸ Setup BBGoldCoin with 3D Model
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Black Barts Gold/Setup BBGoldCoin with 3D Model")]
    public static void SetupCoinModel()
    {
        Debug.Log("[SetupBBGoldCoinModel] ── BEGIN SETUP ──");

        // ── 1. Load FBX mesh ────────────────────────────────────
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinModelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"[SetupBBGoldCoinModel] Could not load FBX at {CoinModelPath}");
            return;
        }

        Mesh mesh = null;
        MeshFilter mf = modelPrefab.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mesh = mf.sharedMesh;
            Debug.Log($"[SetupBBGoldCoinModel] Found mesh: {mesh.name} ({mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris)");
        }

        if (mesh == null)
        {
            Debug.LogError("[SetupBBGoldCoinModel] Could not find mesh in FBX model.");
            return;
        }

        // ── 2. Create / update material with PBR textures ──────
        Material coinMaterial = CreateCoinMaterial();
        if (coinMaterial == null)
        {
            Debug.LogWarning("[SetupBBGoldCoinModel] Material creation failed – falling back to FBX embedded material.");
            MeshRenderer fbxRenderer = modelPrefab.GetComponentInChildren<MeshRenderer>();
            if (fbxRenderer != null && fbxRenderer.sharedMaterial != null)
                coinMaterial = fbxRenderer.sharedMaterial;
        }

        // ── 3. Open prefab for editing ──────────────────────────
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"[SetupBBGoldCoinModel] Could not load prefab at {CoinPrefabPath}");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Find CoinModel child
        Transform coinModel = prefabRoot.transform.Find("CoinModel");
        if (coinModel == null && prefabRoot.transform.childCount > 0)
            coinModel = prefabRoot.transform.GetChild(0);

        if (coinModel == null)
        {
            Debug.LogError("[SetupBBGoldCoinModel] Could not find CoinModel child in prefab.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // ── 4. Assign mesh & material to CoinModel ─────────────
        MeshFilter prefabMeshFilter = coinModel.GetComponent<MeshFilter>();
        MeshRenderer prefabMeshRenderer = coinModel.GetComponent<MeshRenderer>();
        if (prefabMeshFilter == null) prefabMeshFilter = coinModel.gameObject.AddComponent<MeshFilter>();
        if (prefabMeshRenderer == null) prefabMeshRenderer = coinModel.gameObject.AddComponent<MeshRenderer>();

        prefabMeshFilter.sharedMesh = mesh;

        if (coinMaterial != null)
            prefabMeshRenderer.sharedMaterial = coinMaterial;

        // ── 5. Transform: visible at ~0.3 units ────────────────
        coinModel.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        coinModel.localPosition = Vector3.zero;
        coinModel.localRotation = Quaternion.identity;

        // ── 6. Save ─────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 7. Summary ──────────────────────────────────────────
        Debug.Log($"[SetupBBGoldCoinModel] ✅ BBGoldCoin prefab updated!");
        Debug.Log($"[SetupBBGoldCoinModel]   Mesh: {mesh.name}");
        Debug.Log($"[SetupBBGoldCoinModel]   Material: {(coinMaterial != null ? coinMaterial.name : "null")}");
        Debug.Log($"[SetupBBGoldCoinModel]   MainTex: {(coinMaterial != null && coinMaterial.mainTexture != null ? coinMaterial.mainTexture.name : "null")}");
        Debug.Log("[SetupBBGoldCoinModel] ── SETUP COMPLETE ──");
    }

    // ─────────────────────────────────────────────────────────────
    // Create a Standard-shader material using the FBX's PBR textures
    // ─────────────────────────────────────────────────────────────
    private static Material CreateCoinMaterial()
    {
        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder(CoinMaterialFolder))
        {
            string parent = Path.GetDirectoryName(CoinMaterialFolder).Replace('\\', '/');
            string folder = Path.GetFileName(CoinMaterialFolder);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateFolder(parent, folder);
        }

        // Load textures from the .fbm folder
        Texture2D baseColor  = LoadTexture("BBgoldcoin3dmodel_basecolor.JPEG");
        Texture2D metallic   = LoadTexture("BBgoldcoin3dmodel_metallic.JPEG");
        Texture2D normal     = LoadTexture("BBgoldcoin3dmodel_normal.JPEG");
        Texture2D roughness  = LoadTexture("BBgoldcoin3dmodel_roughness.JPEG");

        Debug.Log($"[SetupBBGoldCoinModel] Textures: base={baseColor != null}, metallic={metallic != null}, normal={normal != null}, roughness={roughness != null}");

        if (baseColor == null)
        {
            Debug.LogWarning("[SetupBBGoldCoinModel] Basecolor texture not found – cannot create textured material.");
            return null;
        }

        // Create or load existing material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(CoinMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, CoinMaterialPath);
            Debug.Log($"[SetupBBGoldCoinModel] Created new material: {CoinMaterialPath}");
        }
        else
        {
            Debug.Log($"[SetupBBGoldCoinModel] Updating existing material: {CoinMaterialPath}");
        }

        // Configure Standard shader properties for a shiny gold coin
        mat.SetTexture("_MainTex", baseColor);
        mat.SetColor("_Color", new Color(1f, 0.84f, 0f, 1f)); // Treasure Gold tint

        // Metallic workflow
        mat.SetFloat("_Metallic", 0.85f);
        mat.SetFloat("_Glossiness", 0.75f);

        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.SetFloat("_Metallic", 1f); // Use texture for metallic values
        }

        if (normal != null)
        {
            // Ensure the normal map import type is set correctly
            SetTextureImportType(normal, TextureImporterType.NormalMap);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Note: Standard shader uses _MetallicGlossMap's alpha for smoothness,
        // not a separate roughness map. Roughness texture skipped unless converted.

        // Enable specular highlights and reflections for that pirate gold shine
        mat.SetFloat("_SpecularHighlights", 1f);
        mat.SetFloat("_GlossyReflections", 1f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SetupBBGoldCoinModel] Material configured: shader={mat.shader.name}, mainTex={mat.mainTexture?.name}");
        return mat;
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: Load a texture from the .fbm folder
    // ─────────────────────────────────────────────────────────────
    private static Texture2D LoadTexture(string fileName)
    {
        string path = $"{TextureFolder}/{fileName}";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
            Debug.LogWarning($"[SetupBBGoldCoinModel] Texture not found: {path}");
        return tex;
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: Set texture import type (e.g. NormalMap) if needed
    // ─────────────────────────────────────────────────────────────
    private static void SetTextureImportType(Texture2D texture, TextureImporterType importType)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) return;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != importType)
        {
            importer.textureType = importType;
            importer.SaveAndReimport();
            Debug.Log($"[SetupBBGoldCoinModel] Set {Path.GetFileName(path)} import type to {importType}");
        }
    }
}
