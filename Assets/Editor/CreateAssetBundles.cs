using UnityEditor;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class CreateAssetBundles
{
    #region MenuItems
    [MenuItem("AssetBundle/Build AssetBundles Window(Test)")]
    static void BuildAllAssetBundleWindow() =>
        BuildAllAssetBundles(BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

    [MenuItem("AssetBundle/Build AssetBundles Android")]
    static void BuildAllAssetBundleAndroid() =>
        BuildAllAssetBundles(BuildAssetBundleOptions.None, BuildTarget.Android);

    [MenuItem("AssetBundle/Make AssetBundleManager Window(Test)")]
    static void MakeAssetBundleManagerWindow() =>
        MakeAssetBundleManager(BuildTarget.StandaloneWindows);

    [MenuItem("AssetBundle/Make AssetBundleManager Android")]
    static void MakeAssetBundleManagerAndroid() =>
        MakeAssetBundleManager(BuildTarget.Android);
    #endregion

    static string assetBundleDirectory = "Assets/AssetBundles";

    static void BuildAllAssetBundles(BuildAssetBundleOptions option, BuildTarget target) { 
        // 에셋번들 저장
        if (!Directory.Exists(assetBundleDirectory)) { 
            Directory.CreateDirectory(assetBundleDirectory);
        } 
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, option, target);
    }

    static void MakeAssetBundleManager(BuildTarget target)
    {
        // 에셋번들 매니저 생성
        var bundleManager = new AssetBundleHashs();

        bundleManager.version = Application.version;
        bundleManager.target = GetTargetText(target);

        Debug.Log("Version: " + bundleManager.version);
        Debug.Log("Target : " + bundleManager.target);

        // 번들목록 저장하기
        const int lenAssetFolderName = 6;
        string assetBundlesPath = assetBundleDirectory.Substring(lenAssetFolderName) + "/AssetBundles";
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.dataPath + assetBundlesPath);
        var manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        foreach (var bundle in manifest.GetAllAssetBundles())
        {
            Hash128 characterHash = manifest.GetAssetBundleHash(bundle);
            bundleManager.hashs.Add(new AssetBundleHash(bundle, characterHash.ToString()));
        }

        File.WriteAllText(assetBundleDirectory + "/AssetBundleManager.json", JsonConvert.SerializeObject(bundleManager));
        Debug.Log(assetBundleDirectory + "/AssetBundleManager.json");
    }

    #region Utilities
    static string GetTargetText(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
                return "Window";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            default:
                return "Unknown";
        }
    }
    #endregion
}
