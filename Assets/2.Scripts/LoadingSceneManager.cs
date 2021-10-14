using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using RESTClient;
using Newtonsoft.Json;

public class LoadingSceneManager : MonoBehaviour
{
    #region Scene name
    public static readonly string loadingScene = "Loading";
    #endregion

    public static bool isUpdate = false;
    public static bool hasResources = false;

    Queue<string> getBlobQ = new Queue<string>(); // 다운받아야할 에셋번들 이름 저장
    AssetBundleHashs newestBundleMgr; // 최신 에셋번들매니저 데이터

    #region Properties
    public static string NextScene { get; private set; }

    #endregion

    [SerializeField] Image progressBar;

    private void Start()
    {
        // 씬뷰에서 시각적으로 ProgressBar를 보여주기 위하여 fillAmount를 1로 설정 후,
        // 로딩씬이 시작되는 순간 0으로 변경하여 Progress 진행상황을 보여줌
        progressBar.fillAmount = 0f;
        StartCoroutine(LoadScene());
    }

    #region Public methods
    public static void LoadScene(string sceneName)
    {
        NextScene = sceneName;
        SceneManager.LoadScene(loadingScene);
    }

    public static void LoadScene(int sceneIndex)
    {
        NextScene = SceneManager.GetSceneAt(sceneIndex).name;
        SceneManager.LoadScene(loadingScene);
    }
    #endregion

    #region AssetBundleDownload
    IEnumerator LoadScene()
    {
        // 에셋번들 리소스 다운로드/로드 로직처리
        // 리소스 중복 업로드를 막기 위해 임시적으로 조건 처리 
        if (isUpdate && !hasResources)
        {
            isUpdate = false;

            yield return StartCoroutine(ResourceDownloadCheck());
            yield return StartCoroutine(ResourceLoad());
        }

        //if (NextScene.Contains(-))
        if(false)
        {
            yield return StartCoroutine(BundleLoadScene());
        }
        else
        {
            yield return StartCoroutine(EditorLoadScene());
        }
    }

    IEnumerator EditorLoadScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(NextScene);
        yield return new WaitForSeconds(2f);
        op.allowSceneActivation = false;

        float timer = 0.0f;
        while (!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;
            if (op.progress < 0.9f)
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, op.progress, timer);
                if (progressBar.fillAmount >= op.progress) { timer = 0f; }
            }
            else
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, 1f, timer);
                if (progressBar.fillAmount == 1.0f) { op.allowSceneActivation = true; yield break; }
            }
        }
    }

    static AssetBundle sceneBundle;

    IEnumerator BundleLoadScene()
    {
        float timer = 0.0f;
        if (sceneBundle == null)
        {
            //sceneBundle.Unload(false);
            // 저장한 에셋 번들로부터 씬 에셋 불러오기
            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(Application.persistentDataPath + "/gamescene");

            while (!bundleLoadRequest.isDone)
            {
                yield return null;

                timer += Time.deltaTime;
                if (bundleLoadRequest.progress < 0.9f)
                {
                    progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, bundleLoadRequest.progress, timer);
                    if (progressBar.fillAmount >= bundleLoadRequest.progress) { timer = 0f; }
                }
                else
                {
                    progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, 1f, timer);
                    if (progressBar.fillAmount == 1.0f) { bundleLoadRequest.allowSceneActivation = true; yield break; }
                }
            }
            sceneBundle = bundleLoadRequest.assetBundle;
        }

        // 에셋 번들 내에 존재하는 씬의 경로를 모두 가져오기
        string[] scenes = sceneBundle.GetAllScenePaths();
        string loadScenePath = null;

        foreach (string sname in scenes)
        {
            if (sname.Contains(NextScene))
            {
                loadScenePath = sname;
            }
        }
        if (loadScenePath == null) loadScenePath = "";

        AsyncOperation op = SceneManager.LoadSceneAsync(loadScenePath);
        op.allowSceneActivation = false;

        timer = 0.0f;
        progressBar.fillAmount = 0f;
        while (!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;
            if (op.progress < 0.9f)
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, op.progress, timer);
                if (progressBar.fillAmount >= op.progress) { timer = 0f; }
            }
            else
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, 1f, timer);
                if (progressBar.fillAmount == 1.0f) { op.allowSceneActivation = true; yield break; }
            }
        }
    }

    IEnumerator ResourceDownloadCheck()
    {
        // 로컬의 매니저를 업데이트할 필요가 있는지 체크
        bool isManagerUpdate = false;
        // 로컬에 json이 있는지 판단하기 위해 
        var jsonPath = Application.persistentDataPath + "/AssetBundleManager.json";
        // 서버에서 최신 managerJson 다운로드
        yield return AzureManager.instance.BlobService.GetJsonBlob<AssetBundleHashs>(DownloadBlobVersionMangerJson, "version/AssetBundleManager.json");

        // 에셋번들 매니저가 있으면 버전 비교
        if (File.Exists(jsonPath))
        {
            var texts = File.ReadAllText(jsonPath);
            var currBundleMgr = JsonUtility.FromJson<AssetBundleHashs>(texts);

            // 같으면 스킵 다르면 해쉬값들 비교해서 다운로드
            if(newestBundleMgr.version != currBundleMgr.version)
            {
                isManagerUpdate = true;
                var currHashsDict = new Dictionary<string, string>();
                foreach(var tmp in currBundleMgr.hashs)
                    currHashsDict.Add(tmp.name, tmp.hash);

                var newestHashsDict = new Dictionary<string, string>();
                foreach (var tmp in newestBundleMgr.hashs)
                    newestHashsDict.Add(tmp.name, tmp.hash);

                // 최신 데이터를 기준으로 동일한 에셋번들을 가지고 있는지 검사
                foreach (var hashdata in newestBundleMgr.hashs)
                {
                    bool containKey = currHashsDict.ContainsKey(hashdata.name);
                    if (!containKey || (containKey && currHashsDict[hashdata.name] != hashdata.hash))
                    {
                        // getBlobQ.Enqueue(hashdata.name);
                    }
                }
            }
        }
        // 에셋번들 매니저가 없으면 최신버전 전체 다운로드
        else
        {
            foreach (var hashdata in newestBundleMgr.hashs)
            {
                // getBlobQ.Enqueue(hashdata.name);
            }
            getBlobQ.Enqueue("-"); // 임시 에셋번들 이름
            yield return DownloadResources();
            isManagerUpdate = true;
        }

        if (isManagerUpdate)
        {
            Debug.Log("에셋번들 매니저를 업데이트 하였습니다");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(newestBundleMgr));
        }
    }

    IEnumerator DownloadResources()
    {
        // yield return StartCoroutine(ServerManager.Instance.BlobService.GetBlob(DownloadAssetBundleComplete, "version/-"));
        yield return null;
    }

    private void DownloadBlobVersionMangerJson(IRestResponse<AssetBundleHashs> response)
    {
        newestBundleMgr = response.Data;
    }

    private void DownloadAssetBundleComplete(IRestResponse<byte[]> response)
    {
        using (FileStream fileStream = new FileStream(Application.persistentDataPath + "/" + getBlobQ.Dequeue(), FileMode.Create))
        {
            var dataArray = response.Data;
            // Write the data to the file, byte by byte.
            for (int i = 0; i < dataArray.Length; i++)
            {
                fileStream.WriteByte(dataArray[i]);
            }
            // Set the stream position to the beginning of the file.
            fileStream.Seek(0, SeekOrigin.Begin);
        }
    }

    IEnumerator LoadFromMemoryAsync(string path)
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes(Application.persistentDataPath + "/" + path));
        yield return request;
        AssetBundle bundle = request.assetBundle;

        var bundles = bundle.LoadAllAssets<GameObject>();
        foreach (var obj in bundles)
        {
            if (path.Contains("-"));
            // GameManager.instance.planetObjJig.-(obj, true);
            else;
               //  GameManager.instance.planetObjJig.-(obj, false);
        }
    }

    IEnumerator ResourceLoad()
    {
        hasResources = true;
        foreach (var hashdata in newestBundleMgr.hashs)
        {
            //yield return StartCoroutine(LoadFromMemoryAsync(hashdata.name));
        }
        yield return StartCoroutine(LoadFromMemoryAsync("-"));
        // 리소스 로드
    }
    #endregion
}