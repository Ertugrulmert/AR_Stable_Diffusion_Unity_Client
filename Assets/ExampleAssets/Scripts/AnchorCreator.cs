using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections;

// This class is responsible for handling user input and placing generated meshes in the scene
public class AnchorCreator : MonoBehaviour 
{
    // flags used to determine the current state of the application
    bool isObjectPlaced;
    bool isExpectingObject;
    bool isObjectLoaded;
    bool isRGBready;
    bool isDepthReady;
    bool isSendingImages;

    bool captureGUIOn;

    // toggle Generative mode on and off
    bool isGenerative;

    // Variables for drawing Uı elements
    [SerializeField]
    Texture2D crosshairTexture;
    Rect crosshairPosition;
    Rect leftRect;
    Rect rightRect;
    Rect topRect;
    Rect bottomRect;
    float thickness = 0.02f;
    float padding = 0.1f;
    Texture2D frameTexture;
    GUIStyle frameStyle;

    Vector3 target;
    Vector3 worldUp;

    // Anchor generation data structures
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    ARRaycastHit m_Hit;
    List<ARAnchor> m_AnchorPoints = new List<ARAnchor>();
    ARAnchor m_Anchor;

    // Connections to system components, AR Foudnation components
    XRCameraIntrinsics camIntrinsics;

    [SerializeField]
    ARRaycastManager m_RaycastManager;

    [SerializeField]
    ARAnchorManager m_AnchorManager;

    [SerializeField]
    ARPlaneManager m_PlaneManager;

    [SerializeField]
    ARCameraManager m_CameraManager;

    [SerializeField]
    AROcclusionManager _occlusionManager;

    [SerializeField]
    Camera _camera;

    ObjectLoader loader;

    [SerializeField]
    PostMethod postMethod;

    // UI Components

    [SerializeField]
    public Button startButton;

    [SerializeField]
    public Button toggleGenerative;

    [SerializeField]
    public InputField promptInput;

    [SerializeField]
    public Text messageText;


    // This is the prefab that will appear every time an anchor is created.
    [SerializeField]
    GameObject spawnedObject; 

    //GameObject spawnedObject;
    Texture2D m_RGBTexture;

    // Buffers to save data for sending to the server
    private byte[] _depthBuffer = new byte[0];
    private byte[] _confidenceBuffer = new byte[0]; //confidence value currently not used
    private byte[] _imageBuffer = new byte[0];
    Vector3 camRotation;

    private int _depthHeight = 0;
    private int _depthWidth = 0;
    private int _imageWidth = 0;
    private int _imageHeight = 0;

    public GameObject AnchorPrefab
    {
        get => spawnedObject;
        set => spawnedObject = value; 
    }
    private UnityEngine.Compass compass;


    void Start()
    {
        // normalize scale of standard anchor mesh
        spawnedObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        Debug.Log("Start()");

        // initalize system state flags
        isObjectPlaced = false;
        isExpectingObject = false;
        isSendingImages = false;
        isObjectLoaded = false;
        isGenerative = true;
        isRGBready = false;
        isDepthReady = false;
        captureGUIOn = false;

        // preparing GUI elements

        crosshairPosition = new Rect((Screen.width - crosshairTexture.width) / 2, (Screen.height -  crosshairTexture.height) / 2, crosshairTexture.width, crosshairTexture.height);

        float rectThickness = thickness * Screen.width;

        topRect = new Rect(Screen.width * padding, Screen.height * padding, Screen.width * (1- 2* padding), rectThickness);
        bottomRect = new Rect(Screen.width * padding, Screen.height * (1- padding) - rectThickness, Screen.width * (1 - 2 * padding), rectThickness);

        leftRect = new Rect(Screen.width * padding, Screen.height * padding, rectThickness, Screen.height * (1 - 2*padding));
        rightRect = new Rect(Screen.width * (1-padding) - rectThickness, Screen.height * padding, rectThickness, Screen.height * (1 - 2 * padding));

        frameTexture = new Texture2D(1, 1);
        frameTexture.SetPixel(0, 0, new Color(0.7f, 0.0f, 0.0f, 0.5f));
        frameTexture.Apply();

        frameStyle = new GUIStyle();
        frameStyle.normal.background = frameTexture;


        // initialize connections to system components

        _occlusionManager = GetComponent<AROcclusionManager>();
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        m_PlaneManager = GetComponent<ARPlaneManager>();

        postMethod = GetComponent<PostMethod>();

        _camera.usePhysicalProperties = true;

        compass = Input.compass;
        compass.enabled = true;
        if (!compass.enabled)
        {
            throw new Exception("Device does not support Compass");
        }

        if (!m_CameraManager.TryGetIntrinsics(out camIntrinsics))
        {
            throw new Exception("Camera intrinsics could not be acquired!");
        }

        // wiring UI elements and components
        startButton.onClick.AddListener(OnStartProcessing);
        postMethod.onServerFound.AddListener(onServerFound);
        toggleGenerative.onClick.AddListener(onGenerativeToggled);
    }

    protected void onServerFound(bool b)
    {
        captureGUIOn = true;
    }

    protected void onGenerativeToggled()
    {
        isGenerative = !isGenerative;
        if (isGenerative)
        {
            toggleGenerative.GetComponentInChildren<Text>().text = "Turn Off \n Generative \n Mode";
        }
        else
        {
            toggleGenerative.GetComponentInChildren<Text>().text = "Turn On \n Generative \n Mode";
        }
    }


    void Awake()
    {

        isObjectPlaced = false;
        isExpectingObject = false;
        isSendingImages = false;
        isObjectLoaded = false;
        isRGBready = false;
        isDepthReady = false;
        captureGUIOn = false;

        _occlusionManager = GetComponent<AROcclusionManager>();
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        m_PlaneManager = GetComponent<ARPlaneManager>();

        postMethod = GetComponent<PostMethod>();

        postMethod.onServerFound.AddListener(onServerFound);

        if (m_AnchorManager.subsystem == null)
        {
            enabled = false;
            Debug.LogWarning($"No active XRAnchorSubsystem is available, so {typeof(AnchorCreator).FullName} will not be enabled.");
        }

        RemoveAllAnchors();
        startButton.onClick.AddListener(OnStartProcessing);
    }

    // Removes all the anchors that have been created.
    public void RemoveAllAnchors()
    {
        try
        {
            foreach (var anchor in m_AnchorPoints)
            {
                Destroy(anchor.gameObject);
                Destroy(anchor);
            }
            m_AnchorPoints.Clear();
            var planes = m_PlaneManager.trackables;
            foreach (var arPlane in planes)
            {
                Destroy(arPlane.gameObject);
                Destroy(arPlane);
            }

        }
        catch (Exception e)
        {
            Debug.Log("exception at  RemoveAllAnchors()");
            Debug.Log(e);
        }
        isObjectPlaced = false;
        isExpectingObject = false;
        isSendingImages = false;
        isObjectLoaded = false;

        isRGBready = false;
        isDepthReady = false;
         
    }

    // Upon pressing the "Start" button the function attempts to create an anchor, capture data and send data to the server
    ARAnchor CreateAnchor(in ARRaycastHit hit)
    {
        target = hit.pose.position + _camera.transform.rotation * Vector3.forward;
        worldUp = _camera.transform.rotation * Vector3.up;

        ARAnchor anchor = null;

        // create anchor at the hit pose
        Debug.Log("Creating regular anchor.");
        if (spawnedObject != null) //if (m_AnchorPrefab != null)
        {
            spawnedObject = Instantiate(spawnedObject, hit.pose.position, hit.pose.rotation);
            spawnedObject.transform.LookAt(target, worldUp);

            anchor = ComponentUtils.GetOrAddIf<ARAnchor>(spawnedObject, true);
            anchor.transform.LookAt(target, worldUp);
        }
        else
        {
            spawnedObject = new GameObject("Anchor");
            spawnedObject.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
            anchor = spawnedObject.AddComponent<ARAnchor>();
        }
        spawnedObject.transform.LookAt(target, worldUp);
        captureGUIOn = false;

        Debug.Log("created anchor.");
        return anchor;
    }

    void OnStartProcessing()
    {
        // Raycast against planes and feature points
        const TrackableType trackableTypes =
            TrackableType.FeaturePoint |
            TrackableType.PlaneWithinPolygon;

        if (!isExpectingObject && !isObjectPlaced && !isObjectLoaded && !isSendingImages)
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Perform the raycast
            if (m_RaycastManager.Raycast(ray, s_Hits, trackableTypes))
            {
                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hit = s_Hits[0];

                m_Hit = hit;

                messageText.enabled = true;
                messageText.text = "Scene registered, processing ...";

                // Create a new anchor
                var anchor = CreateAnchor(hit);
                if (anchor != null)
                {
                    m_Anchor = anchor;
                    // Remember the anchor so we can remove it later.
                    m_AnchorPoints.Add(anchor);
                    AsynchronousConversion();
                }
                else
                {
                    Debug.Log("Error creating anchor");
                }
            }
            else
            {
                messageText.enabled = true;
                messageText.text = "Could not register scene, \n please try again...";
            }
        }
    }

    private float normalizedYAngle(Quaternion q)
    {
        Vector3 eulers = q.eulerAngles;
        float yAngle = eulers.y;
        if (yAngle >= 180f)
        {
            //ex: 182 = 182 - 360 = -178
            yAngle -= 360;
        }
        return Mathf.Abs(yAngle);
    }

    Quaternion GyroModifyCamera()
    {
        Quaternion gyroQuaternion = GyroToUnity(Input.gyro.attitude);
        // rotate coordinate system 90 degrees. Correction Quaternion has to come first
        // return correctionQuaternion * gyroQuaternion;
        return gyroQuaternion;
    }

    // draw GUI elements
    void OnGUI()
    {
        if (captureGUIOn)
        {
            GUI.DrawTexture(crosshairPosition, crosshairTexture);

            GUI.Box(leftRect, GUIContent.none, frameStyle);
            GUI.Box(rightRect, GUIContent.none, frameStyle);
            GUI.Box(topRect, GUIContent.none, frameStyle);
            GUI.Box(bottomRect, GUIContent.none, frameStyle);
        }
    }


    void Update()
    {

        Vector2 focalLength;
        Vector2 principalPoint;
        Vector2 resolution;

        Quaternion gyroQuaternion = GyroModifyCamera();
        camRotation = gyroQuaternion.eulerAngles;


        if ( isSendingImages && isRGBready && isDepthReady && postMethod.isServerRunning(true))
        {
            isRGBready = false;
            isDepthReady = false;

            int timestamp = (int)(Time.time * 100);

            // retrieving text prompt from the input box
            string prompt = promptInput.text;

            // retrieving camera intrinsics, reformating for ease of use on the server side
            if (!m_CameraManager.TryGetIntrinsics(out camIntrinsics))
            {
                throw new Exception("Camera intrinsics could not be acquired!");
            }

            focalLength = camIntrinsics.focalLength;
            principalPoint = camIntrinsics.principalPoint;
            resolution = camIntrinsics.resolution;

            string camIntrinsicDict = "{ 'focalLength.x' :" + focalLength.x + ", 'focalLength.y' :" + focalLength.y + 
                ", 'principalPoint.x':" + principalPoint.x + ", 'principalPoint.y':" + principalPoint.y +
                ", 'resolution.x':" + resolution.x + ", 'resolution.y':" + resolution.y + " }";

            Debug.Log($"Camera intrinsics {camIntrinsicDict}");

            // sending client data to server
            StartCoroutine(postMethod.Upload(_imageBuffer, _depthBuffer, //_confidenceBuffer, 
                prompt, timestamp, isGenerative, camIntrinsicDict,
                _imageWidth, _imageHeight, _depthWidth, _depthHeight, camRotation)); //, _confidenceWidth, _confidenceHeight));
        }


        if (isSendingImages && !isExpectingObject && !isObjectPlaced && postMethod.isMeshReceived())
        {
            isExpectingObject = true;
            isSendingImages = false;
            messageText.enabled = true;
            messageText.text = "Loading and rendering mesh...";

            string meshPath = postMethod.getNewMeshPath();
            string matPath = postMethod.getNewMatPath();


            loader = spawnedObject.AddComponent<ObjectLoader>();  
            loader.Load(meshPath, matPath);

        }

        if (isExpectingObject && !isObjectPlaced && 
              loader != null && loader.getLoaded()) 
        {
            isObjectPlaced = true;
            isExpectingObject = false;
            Debug.Log($"Spawned new mesh successfuly");
            messageText.enabled = false;
            spawnedObject.transform.LookAt(target, worldUp);
        }

    }
     

    // retrieving depth image estiamted by ARCore
    void UpdateEnvironmentDepthImage()
    {
        if (_occlusionManager &&
            _occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        {
            using (image)
            {
                _depthWidth = image.width;
                _depthHeight = image.height;

                var slice = new NativeSlice<byte>(image.GetPlane(0).data).SliceConvert<byte>();
                _depthBuffer = new byte[slice.Length];
                slice.CopyTo(_depthBuffer);

                isDepthReady = true;
            }
        }
    }

    // sending user data to the server
    void AsynchronousConversion()
    {
        messageText.enabled = true;
        messageText.text = "Sending data to server...";
        isSendingImages = true;
        camRotation = GyroToUnity(Input.gyro.attitude).eulerAngles;

        // Acquire RGB Camera image
        if (m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // If successful, launch an asynchronous conversion coroutine
            StartCoroutine(ConvertImageAsync(image));

            // It is safe to dispose the image before the async operation completes
            image.Dispose();
        }
        UpdateEnvironmentDepthImage();

    }

    // write RGB Image data to buffer for sending to the server 
    IEnumerator ConvertImageAsync(XRCpuImage image)
    {
        // Create the async conversion request
        var request = image.ConvertAsync(new XRCpuImage.ConversionParams
        {
            // Use the full image
            inputRect = new RectInt(0, 0, image.width, image.height),

            outputDimensions = new Vector2Int(image.width, image.height),

            // Output an RGB color image format
            outputFormat = TextureFormat.RGB24,

            // Flip across the Y axis
            transformation = XRCpuImage.Transformation.MirrorY
        });

        // Wait for the conversion to complete
        while (!request.status.IsDone())
            yield return null;

        // Check status to see if the conversion completed successfully
        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            isRGBready = false;
            isSendingImages = false;
            isDepthReady = false;
            // Something went wrong
            Debug.LogErrorFormat("Request failed with status {0}", request.status);

            // Dispose even if there is an error
            request.Dispose();
            yield break;
        }

        // Image data is ready. Let's apply it to a Texture2D
        var rawData = request.GetData<byte>();

        // Create a texture
        var texture = new Texture2D(
            request.conversionParams.outputDimensions.x,
            request.conversionParams.outputDimensions.y,
            request.conversionParams.outputFormat,
            false);

        // Copy the image data into the texture
        texture.LoadRawTextureData(rawData);
        texture.Apply();
        _imageBuffer = texture.EncodeToPNG();
        isRGBready = true;

        // Dispose the request including raw data
        request.Dispose();
    }


    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

}


