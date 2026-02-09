// SetupColorBBCoin.cs - Black Bart's Gold
// Adds Color BB coin and can set it as the default.
// Path: Assets/Editor/SetupColorBBCoin.cs

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class SetupColorBBCoin
{
    const string FbxName = "tripo_convert_2eeb013c-4c27-44b0-aa9f-558cea4c4122.fbx";
    const string CoinModelPath = "Assets/Models/ColorBB/tripo_convert_2eeb013c-4c27-44b0-aa9f-558cea4c4122.fbx";
    const string TextureFolder = "Assets/Models/ColorBB/tripo_convert_2eeb013c-4c27-44b0-aa9f-558cea4c4122.fbm";
    const string CoinMaterialFolder = "Assets/Materials/Coins";
    const string CoinMaterialPath = "Assets/Materials/Coins/ColorBBCoin3D.mat";
    const string SourcePrefabPath = "Assets/Prefabs/Coins/BBGoldCoin.prefab";
    const string ColorBBPrefabPath = "Assets/Prefabs/Coins/ColorBBCoin.prefab";
    const string ARHuntScenePath = "Assets/Scenes/ARHunt.unity/ARHunt.unity";

    [MenuItem("Black Barts Gold/Setup Color BB Coin + Set as Default")]
    public static void SetupAndSetDefault()
    {
        SetupColorBBCoinModel();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColorBBPrefabPath);
        if (prefab != null)
            SetDefaultCoinInScene(prefab);
    }

    [MenuItem("Black Barts Gold/Setup Color BB Coin (create prefab + material)")]
    public static void SetupColorBBCoinModel()
    {
        Debug.Log("[SetupColorBBCoin] ── BEGIN SETUP ──");

        if (!File.Exists(Path.Combine(Application.dataPath, "Models", "ColorBB", FbxName)))
        {
            Debug.LogError("[SetupColorBBCoin] FBX not found. Ensure Color BB model is in Assets/Models/ColorBB/.");
            return;
        }

        AssetDatabase.Refresh();

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinModelPath);
        if (modelPrefab == null)
        {
            Debug.LogError("[SetupColorBBCoin] Could not load FBX at " + CoinModelPath);
            return;
        }

        Mesh mesh = null;
        MeshFilter mf = modelPrefab.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mesh = mf.sharedMesh;
            Debug.Log("[SetupColorBBCoin] Mesh: " + mesh.name + " (" + mesh.vertexCount + " verts)");
        }
        if (mesh == null)
        {
            Debug.LogError("[SetupColorBBCoin] No mesh in FBX.");
            return;
        }

        Material coinMaterial = CreateColorBBMaterial();
        if (coinMaterial == null)
        {
            Debug.LogError("[SetupColorBBCoin] Material creation failed.");
            return;
        }

        if (!AssetDatabase.CopyAsset(SourcePrefabPath, ColorBBPrefabPath))
        { }
        AssetDatabase.Refresh();

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ColorBBPrefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError("[SetupColorBBCoin] Could not load prefab at " + ColorBBPrefabPath);
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        Transform coinModel = prefabRoot.transform.Find("CoinModel");
        if (coinModel == null && prefabRoot.transform.childCount > 0)
            coinModel = prefabRoot.transform.GetChild(0);
        if (coinModel == null)
        {
            Debug.LogError("[SetupColorBBCoin] No CoinModel child in prefab.");
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

        prefabRoot.name = "ColorBBCoin";
        prefabRoot.transform.GetChild(0).gameObject.name = "CoinModel";

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupColorBBCoin] Color BB Coin prefab ready at " + ColorBBPrefabPath);
        Debug.Log("[SetupColorBBCoin] ── SETUP COMPLETE ──");
    }

    [MenuItem("Black Barts Gold/Set default coin to Color BB (ARHunt)")]
    public static void SetDefaultCoinToColorBB()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColorBBPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[SetupColorBBCoin] Run 'Setup Color BB Coin' first.");
            return;
        }
        SetDefaultCoinInScene(prefab);
    }

    static void SetDefaultCoinInScene(GameObject prefab)
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "ARHunt.unity", "ARHunt.unity")))
        {
            Debug.LogError("[SetupColorBBCoin] ARHunt scene not found.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ARHuntScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[SetupColorBBCoin] Could not open ARHunt scene.");
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (var cm in root.GetComponentsInChildren<BlackBartsGold.AR.CoinManager>(true))
            {
                SerializedObject so = new SerializedObject(cm);
                SerializedProperty prop = so.FindProperty("coinPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[SetupColorBBCoin] Default coin set to: " + prefab.name);
                    return;
                }
            }
        }

        Debug.LogWarning("[SetupColorBBCoin] CoinManager not found in ARHunt scene.");
    }

    static Material CreateColorBBMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CoinMaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Coins");

        Texture2D baseColor = LoadTexture("coin3dmodel_basecolor.JPEG");
        Texture2D metallic = LoadTexture("coin3dmodel_metallic.JPEG");
        Texture2D normal = LoadTexture("coin3dmodel_normal.JPEG");

        if (baseColor == null)
        {
            Debug.LogWarning("[SetupColorBBCoin] Basecolor texture not found.");
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
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + fileName);
    }

    static void SetTextureImportType(Texture2D texture, TextureImporterType importType)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) return;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != importType)
        {
            importer.textureType = importType;
            importer.SaveAndReimport();
        }
    }
}
