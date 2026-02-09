// SetupPrizeRaceCoin.cs - Black Bart's Gold
// Adds Prize Race coin as a second coin graphic type and can set it as the default.
// Path: Assets/Editor/SetupPrizeRaceCoin.cs

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class SetupPrizeRaceCoin
{
    const string ModelFolder   = "Assets/Models/PrizeRaceCoin";
    const string FbxName        = "tripo_convert_7ad6845f-9604-4cbe-bfc6-47cc2e4923f4.fbx";
    const string FbmName        = "tripo_convert_7ad6845f-9604-4cbe-bfc6-47cc2e4923f4.fbm";
    const string CoinModelPath  = "Assets/Models/PrizeRaceCoin/tripo_convert_7ad6845f-9604-4cbe-bfc6-47cc2e4923f4.fbx";
    const string TextureFolder  = "Assets/Models/PrizeRaceCoin/tripo_convert_7ad6845f-9604-4cbe-bfc6-47cc2e4923f4.fbm";
    const string CoinMaterialFolder = "Assets/Materials/Coins";
    const string CoinMaterialPath   = "Assets/Materials/Coins/PrizeRaceCoin3D.mat";
    const string SourcePrefabPath   = "Assets/Prefabs/Coins/BBGoldCoin.prefab";
    const string PrizeRacePrefabPath = "Assets/Prefabs/Coins/PrizeRaceCoin.prefab";
    const string ARHuntScenePath     = "Assets/Scenes/ARHunt.unity/ARHunt.unity";

    [MenuItem("Black Barts Gold/Setup Prize Race Coin + Set as Default")]
    public static void SetupAndSetDefault()
    {
        SetupPrizeRaceCoinModel();
        GameObject prizeRacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrizeRacePrefabPath);
        if (prizeRacePrefab != null)
            SetDefaultCoinInScene(prizeRacePrefab);
    }

    [MenuItem("Black Barts Gold/Setup Prize Race Coin (create prefab + material)")]
    public static void SetupPrizeRaceCoinModel()
    {
        Debug.Log("[SetupPrizeRaceCoin] ── BEGIN SETUP ──");

        if (!File.Exists(Path.Combine(Application.dataPath, "Models", "PrizeRaceCoin", FbxName)))
        {
            Debug.LogError($"[SetupPrizeRaceCoin] FBX not found at {CoinModelPath}. Ensure the Prize Race model is in Assets/Models/PrizeRaceCoin/.");
            return;
        }

        AssetDatabase.Refresh();

        // 1. Load mesh from FBX
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinModelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"[SetupPrizeRaceCoin] Could not load FBX at {CoinModelPath}");
            return;
        }

        Mesh mesh = null;
        MeshFilter mf = modelPrefab.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mesh = mf.sharedMesh;
            Debug.Log($"[SetupPrizeRaceCoin] Mesh: {mesh.name} ({mesh.vertexCount} verts)");
        }
        if (mesh == null)
        {
            Debug.LogError("[SetupPrizeRaceCoin] No mesh in FBX.");
            return;
        }

        // 2. Create material (rich gold values matching ARCoinRenderer)
        Material coinMaterial = CreatePrizeRaceMaterial();
        if (coinMaterial == null)
        {
            Debug.LogError("[SetupPrizeRaceCoin] Material creation failed.");
            return;
        }

        // 3. Create or update PrizeRaceCoin prefab (copy of BBGoldCoin, then swap mesh + material)
        if (!AssetDatabase.CopyAsset(SourcePrefabPath, PrizeRacePrefabPath))
        {
            // May already exist; load and update
        }
        AssetDatabase.Refresh();

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrizeRacePrefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"[SetupPrizeRaceCoin] Could not load prefab at {PrizeRacePrefabPath}");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        Transform coinModel = prefabRoot.transform.Find("CoinModel");
        if (coinModel == null && prefabRoot.transform.childCount > 0)
            coinModel = prefabRoot.transform.GetChild(0);
        if (coinModel == null)
        {
            Debug.LogError("[SetupPrizeRaceCoin] No CoinModel child in prefab.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        MeshFilter pf = coinModel.GetComponent<MeshFilter>();
        MeshRenderer pr = coinModel.GetComponent<MeshRenderer>();
        if (pf == null) pf = coinModel.gameObject.AddComponent<MeshFilter>();
        if (pr == null) pr = coinModel.gameObject.AddComponent<MeshRenderer>();
        pf.sharedMesh = mesh;
        pr.sharedMaterial = coinMaterial;

        coinModel.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        coinModel.localPosition = Vector3.zero;
        coinModel.localRotation = Quaternion.identity;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SetupPrizeRaceCoin] ✅ PrizeRaceCoin prefab ready at {PrizeRacePrefabPath}");
        Debug.Log("[SetupPrizeRaceCoin] ── SETUP COMPLETE ──");
    }

    [MenuItem("Black Barts Gold/Set default coin to Prize Race (ARHunt)")]
    public static void SetDefaultCoinToPrizeRace()
    {
        GameObject prizeRacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrizeRacePrefabPath);
        if (prizeRacePrefab == null)
        {
            Debug.LogWarning("[SetupPrizeRaceCoin] Run 'Setup Prize Race Coin' first.");
            return;
        }
        SetDefaultCoinInScene(prizeRacePrefab);
    }

    [MenuItem("Black Barts Gold/Set default coin to BB Gold Coin (ARHunt)")]
    public static void SetDefaultCoinToBBGoldCoin()
    {
        GameObject bbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (bbPrefab == null)
        {
            Debug.LogError("[SetupPrizeRaceCoin] BBGoldCoin prefab not found.");
            return;
        }
        SetDefaultCoinInScene(bbPrefab);
    }

    static void SetDefaultCoinInScene(GameObject prefab)
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "ARHunt.unity", "ARHunt.unity")))
        {
            Debug.LogError("[SetupPrizeRaceCoin] ARHunt scene not found.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ARHuntScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[SetupPrizeRaceCoin] Could not open ARHunt scene.");
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            var managers = root.GetComponentsInChildren<BlackBartsGold.AR.CoinManager>(true);
            foreach (var cm in managers)
            {
                SerializedObject so = new SerializedObject(cm);
                SerializedProperty prop = so.FindProperty("coinPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[SetupPrizeRaceCoin] Default coin set to: {prefab.name}");
                    return;
                }
            }
        }

        Debug.LogWarning("[SetupPrizeRaceCoin] CoinManager not found in ARHunt scene.");
    }

    static Material CreatePrizeRaceMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CoinMaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Coins");

        Texture2D baseColor  = LoadTexture("PrizeRacecoin3dmodel_basecolor.JPEG");
        Texture2D metallic   = LoadTexture("PrizeRacecoin3dmodel_metallic.JPEG");
        Texture2D normal     = LoadTexture("PrizeRacecoin3dmodel_normal.JPEG");

        if (baseColor == null)
        {
            Debug.LogWarning("[SetupPrizeRaceCoin] Basecolor texture not found.");
            return null;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(CoinMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, CoinMaterialPath);
        }

        mat.SetTexture("_MainTex", baseColor);
        mat.SetColor("_Color", new Color(1f, 0.82f, 0.28f, 1f));
        mat.SetFloat("_Metallic", 0.4f);
        mat.SetFloat("_Glossiness", 0.65f);
        if (metallic != null)
            mat.SetTexture("_MetallicGlossMap", metallic);
        if (normal != null)
        {
            SetTextureImportType(normal, TextureImporterType.NormalMap);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.98f, 0.76f, 0.2f, 1f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mat.EnableKeyword("_METALLICGLOSSMAP");
        mat.SetFloat("_SpecularHighlights", 1f);
        mat.SetFloat("_GlossyReflections", 1f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static Texture2D LoadTexture(string fileName)
    {
        string path = $"{TextureFolder}/{fileName}";
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void SetTextureImportType(Texture2D texture, TextureImporterType importType)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) return;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != importType)
        {
            importer.textureType = importType;
            importer.SaveAndReimport();
        }
    }
}
