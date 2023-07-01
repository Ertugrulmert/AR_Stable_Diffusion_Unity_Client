using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using  Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class BoolEvent : UnityEvent<bool> { }

public class PostMethod : MonoBehaviour
{
    public static string ipv4Address = "";
    static string portNumber = "5000/";

    static string serverUrl = "";

    // REST API communication system state flags
    private bool meshReceived;
    private bool matReceived;
    private bool textureReceived;

    // whether server connection is available
    private bool serverRunning;

    public BoolEvent onServerFound = new BoolEvent();

    private RenderTexture renderTexture;
    public int resolutionWidth;
    public int resolutionHeight;
    public int bytesPerPixel;
    private byte[] rawByteData;
    private Texture2D texture2D;

    [SerializeField]
    public InputField serverURIInput;
    [SerializeField]
    public Button uriButton;
    [SerializeField]
    public Text messageText;

    private static List<string> mesh_filepaths = new List<string>();
    private static List<string> mat_filepaths = new List<string>();


    void Start()
    {
        uriButton.onClick.AddListener(setIPAddress);

        meshReceived = false;
        matReceived = false;
        textureReceived = false;

        serverRunning = false;
    }

    void Awake()
    {
        meshReceived = false;
        matReceived = false;
        textureReceived = false;

        serverRunning = false;
    }

    public void setIPAddress()
    {
        ipv4Address = serverURIInput.text;
        Debug.Log($"Set ip address: {ipv4Address}");
        serverUrl = "http://" + ipv4Address + ":" + portNumber;
        messageText.enabled = true;
        StartCoroutine(testConnection());
        serverURIInput.gameObject.SetActive(false);
        uriButton.gameObject.SetActive(false);
    }

    public bool isMeshReceived()
    {
        return meshReceived && matReceived && textureReceived;
    }

    public string getNewMeshPath()
    {
        return mesh_filepaths.Last();
    }

    public string getNewMatPath()
    {
        return mat_filepaths.Last();
    }

    public bool isServerRunning(bool retry)
    {
        if (!serverRunning)
        {
            Debug.Log("Server down or no conenction attempt was made. Attempting to connect... ");
            if (retry)
            {
                StartCoroutine(testConnection());
            }
            return serverRunning;  
        }
        return serverRunning;
    }

    // sending user data to the server to initatie generation - confidence maps are currently diabled

    public IEnumerator Upload(byte[] rgbImage, byte[] depthImage, //byte[] confidenceImage, 
        string prompt, long timestamp, bool isGenerative, string camIntrinsics,
        int _imageWidth, int _imageHeight, int _depthWidth, int _depthHeight, Vector3 camRotation )//, int _confidenceWidth, int _confidenceHeight)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("rgbImage", rgbImage, timestamp.ToString() + "_rgbImage.jpg", "image/*jpg"));
        formData.Add(new MultipartFormFileSection("depthImage", depthImage, timestamp.ToString() + "_depthImage.txt", "text/plain"));
        //formData.Add(new MultipartFormFileSection("confidenceImage", confidenceImage, timestamp.ToString() + "_confidenceImage.txt", "text/plain"));
        //formData.Add(new MultipartFormFileSection("projectionMatrix", time + "_projectionMatrix.txt", "text/plain"));
        //formData.Add(new MultipartFormFileSection("viewMatrix", time + "_viewMatrix.txt", "text/plain"));
        formData.Add(new MultipartFormDataSection("timestamp", timestamp.ToString(), "text/plain"));
        if (prompt != "")
        {
            formData.Add(new MultipartFormDataSection("prompt", prompt, "text/plain"));
        }
        formData.Add(new MultipartFormDataSection("isGenerative", isGenerative ? "true" : "false", "text/plain"));
        formData.Add(new MultipartFormDataSection("camIntrinsics", camIntrinsics, "text/plain"));
        formData.Add(new MultipartFormDataSection("imageWidth", _imageWidth.ToString(), "text/plain"));
        formData.Add(new MultipartFormDataSection("imageHeight", _imageHeight.ToString(), "text/plain"));
        formData.Add(new MultipartFormDataSection("depthWidth", _depthWidth.ToString(), "text/plain"));
        formData.Add(new MultipartFormDataSection("depthHeight", _depthHeight.ToString(), "text/plain"));
        formData.Add(new MultipartFormDataSection("camRotation", $"{camRotation.x},{camRotation.y},{camRotation.z}", "text/plain"));
        //formData.Add(new MultipartFormDataSection("confidenceWidth", _confidenceWidth.ToString(), "text/plain"));
        //formData.Add(new MultipartFormDataSection("confidenceHeight", _confidenceHeight.ToString(), "text/plain"));
        Debug.Log($"camRotation: {camRotation.x},{camRotation.y},{camRotation.z}");

        UnityWebRequest www = UnityWebRequest.Post(serverUrl, formData);

        www.timeout = 60*20;

        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");

            string responseText = www.downloadHandler.text;
            Debug.Log("Response Text:" + responseText);

            var json = JObject.Parse(responseText);

            // Requesting mesh file 

            string mesh_filename = json["mesh_path"].ToString();

            string mesh_path = serverUrl + "/mesh/" + mesh_filename;

            yield return StartCoroutine(GetRequest(mesh_path, mesh_filename, "mesh"));
            
            // Requesting material file 

            string mat_filename = json["material_path"].ToString();

            string mat_path = serverUrl + "/mat/" + mat_filename;

            yield return StartCoroutine(GetRequest(mat_path, mat_filename, "mat"));

            // Requesting material file 

            string texture_filename = json["texture_path"].ToString();

            string texture_path = serverUrl + "/texture/" + texture_filename;

            yield return StartCoroutine(GetRequest(texture_path, texture_filename, "texture"));

        }
    }

    public IEnumerator testConnection()
    {

        String postBodyText = "Attempting to connect...";
        UnityWebRequest www = UnityWebRequest.Post(serverUrl, postBodyText);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Server connection FAILED");
            Debug.Log(www.error);
            serverRunning = false;
            serverURIInput.gameObject.SetActive(true);
            uriButton.gameObject.SetActive(true);
            messageText.text = "Server connection failed, please retry...";
        }
        else
        {
            Debug.Log("Server connection successful!");
            serverRunning = true;
            messageText.enabled = false;
            onServerFound.Invoke(true);
        }
    }

    public IEnumerator GetRequest(string uri, string filename, string file_type)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log(webRequest.downloadHandler.text);
                

                byte[] results = webRequest.downloadHandler.data;


                string filePath = Application.persistentDataPath + "/" + filename;

                File.WriteAllBytes(filePath, results);

                if (file_type.Equals("mesh"))
                {
                   
                    mesh_filepaths.Add(filePath);
                    meshReceived = true;
                    Debug.Log($"mesh_filepaths.Add(filePath); PPPP {filePath}");
                }
                else if (file_type.Equals("mat"))
                {
                
                    mat_filepaths.Add(filePath);
                    matReceived = true;
                    Debug.Log($"mat_filepaths.Add(filePath); PPPP {filePath}");
                }
                else if (file_type.Equals("texture"))
                {
                    textureReceived = true;
                    Debug.Log($"texture received {filePath}");
                }
            }
        }
    }

}