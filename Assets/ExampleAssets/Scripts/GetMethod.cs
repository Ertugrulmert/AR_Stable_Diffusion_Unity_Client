using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net.Http;
using System.Net;

public class GetMethod : MonoBehaviour
{
    InputField outputArea;

    private bool meshReceived;

    private static List<string> mesh_filepaths = new List<string>();

    void Start()
    {
        meshReceived = false;
    }

    bool isMeshReceived()
    {
        return meshReceived;
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log("Error: " + webRequest.error);
            } else
            {
                Debug.Log(webRequest.downloadHandler.text);
                meshReceived = true;
            }
        }
    }
}