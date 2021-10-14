using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Azure.StorageServices;

public class AzureManager : MonoBehaviour
{
    public static AzureManager instance;
    
    // 컨테이너 이름 표시방법 고민중
    readonly string versionContainer = "version";
    readonly string userContainer = "user";

    private StorageServiceClient client;
    private BlobService blobService;

    public BlobService BlobService
    {
        get { return blobService; }
    }

    #region UnityEventFuncs
    private void Awake()
    {
        // 싱글톤
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        client = StorageServiceClient.Create(AzureKey.storageAccount, AzureKey.accessKey);  //Azure 스토리지 클라이언트 설정
        blobService = client.GetBlobService();
    }
    #endregion
}
