using System;
using Newtonsoft.Json;
using System.Collections.Generic;

[Serializable]
public class AssetBundleHash
{
    [JsonProperty("name")] public string name;
    [JsonProperty("hash")] public string hash;

    public AssetBundleHash(string name, string hash)
    {
        this.name = name;
        this.hash = hash;
    }
}

[Serializable]
public class AssetBundleHashs
{
    [JsonProperty("version")] public string version;
    [JsonProperty("target")] public string target;
    [JsonProperty("hashs")] public List<AssetBundleHash> hashs = new List<AssetBundleHash>();
}
