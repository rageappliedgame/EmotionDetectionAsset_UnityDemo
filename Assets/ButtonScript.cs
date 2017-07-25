using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using AssetPackage;

public class ButtonScript : MonoBehaviour
{
    //! http://answers.unity3d.com/questions/909967/getting-a-web-cam-to-play-on-ui-texture-image.html

    /// <summary>
    /// The rawimage, used to show webcam output.  
    /// </summary>
    ///
    /// <remarks>
    /// In this demo, rawimage is the Canvas of the scene.
    /// </remarks>
    public RawImage rawimage;

    /// <summary>
    /// The emotions, used to show the detedted emotions.
    /// </summary>
    ///
    /// <remarks>
    /// In this demo, emotions is the bottom Text object of the scene.
    /// </remarks>
    public Text emotions;

    /// <summary>
    /// The message, used to signal the number of faces detected.
    /// </summary>
    /// 
    /// <remarks>
    /// In this demo, emotions is the top Text object of the scene.
    /// </remarks>
    public Text msg;

    //! https://answers.unity3d.com/questions/1101792/how-to-post-process-a-webcamtexture-in-realtime.html

    /// <summary>
    /// The webcam.
    /// </summary>
    WebCamTexture webcam;

    /// <summary>
    /// The output.
    /// </summary>
    Texture2D output;

    /// <summary>
    /// The data.
    /// </summary>
    Color32[] data;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    void Start()
    {
        //1) Enumerate webcams
        //
        WebCamDevice[] devices = WebCamTexture.devices;

        //2) for debugging purposes, prints available devices to the console
        //
        for (int i = 0; i < devices.Length; i++)
        {
            print("Webcam available: " + devices[i].name);
        }

        //! http://answers.unity3d.com/questions/909967/getting-a-web-cam-to-play-on-ui-texture-image.html
        //WebCamTexture webcam = new WebCamTexture();
        //rawimage.texture = webcam;
        //rawimage.material.mainTexture = webcam;
        //webcamTexture.Play();

        //! https://answers.unity3d.com/questions/1101792/how-to-post-process-a-webcamtexture-in-realtime.html
        //3) Create a WebCamTexture (size should not be to big)
        webcam = new WebCamTexture(640, 480);

        //4) Assign the texture to an image in the UI to see output (these two lines are not necceasary if you do 
        //   not want to show the webcam video, but might be handy for debugging purposes)
        rawimage.texture = webcam;
        rawimage.material.mainTexture = webcam;

        //5) Start capturing the webcam.
        //
        webcam.Play();

        //6) ??
        //output = new Texture2D(webcam.width, webcam.height);
        //GetComponent<Renderer>().material.mainTexture = output;

        // 7) Create an array to hold the ARGB data of a webcam video frame texture. 
        //
        data = new Color32[webcam.width * webcam.height];

        //8) Create an EmotionDetectionAsset
        //
        //   The asset will load the appropriate dlibwrapper depending on process and OS.
        //   Note that during development unity tends to use the 32 bits version where 
        //   during playing it uses either 32 or 64 bits version dependend on the OS.
        //   
        eda = new EmotionDetectionAsset();

        //9) Assign a bridge (no interfaces are required but ILog is convenient during development.
        // 
        eda.Bridge = new dlib_csharp.Bridge();

        //10) Init the EmotionDetectionAsset. 
        //    Note this takes a couple of seconds as it need to read/parse the shape_predictor_68_face_landmarks database
        // 
        eda.Initialize(@"Assets\", database);

        //11) Read the fuzzy logic rules and parse them.
        // 
        String[] lines = File.ReadAllLines(furia);
        eda.ParseRules(lines);

        Debug.Log("Emotion detection Ready for Use");
    }

    Int32 frames = 0;

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update()
    {
        // 1) Check if the data array is allocated and we have a created a valid EmotionDetectionAsset.
        //    If so, process every 5th frame.
        // 
        if (data != null && eda != null && (++frames) % 5 > 0)
        {
            // 2) Get the raw 32 bits ARGB data from the frame.
            // 
            webcam.GetPixels32(data);

            // 3) Process this ARGB Data.
            // 
            ProcessColor32(data, webcam.width, webcam.height);

            // You can play around with data however you want here.
            // Color32 has member variables of a, r, g, and b. You can read and write them however you want.

            //output.SetPixels32(data);
            //output.Apply();

            // For debugging it might be handy to stop processsing after a number of processed frames.
            // 
            //if (frames == 0)
            //{
            // webcam.Stop();
            //}

            frames = 0;
        }
    }

    /// <summary>
    /// A face (test input).
    /// </summary>
    const String face3 = @"Assets\Kiavash1.jpg";

    /// <summary>
    /// The Furia Fuzzy Logic Rules.
    /// </summary>
    const String furia = @"Assets\FURIA Fuzzy Logic Rules.txt";

    /// <summary>
    /// The landmark database.
    /// </summary>
    const String database = @"Assets\shape_predictor_68_face_landmarks.dat";

    /// <summary>
    /// http://ericeastwood.com/blog/17/unity-and-dlls-c-managed-and-c-unmanaged
    /// https://docs.unity3d.com/Manual/NativePluginInterface.html.
    /// </summary>
    EmotionDetectionAsset eda;

    /// <summary>
    /// Loads a PNG.
    /// </summary>
    ///
    /// <param name="filePath"> Full pathname of the file. </param>
    ///
    /// <returns>
    /// The PNG.
    /// </returns>
    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(640, 864, TextureFormat.RGBA32, false);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    /// <summary>
    /// Color32 array to byte array.
    /// </summary>
    ///
    /// <param name="colors"> The colors. </param>
    ///
    /// <returns>
    /// A byte[].
    /// </returns>
    private static byte[] Color32ArrayToByteArray(UnityEngine.Color32[] colors)
    {
        if (colors == null || colors.Length == 0)
            return null;

        int lengthOfColor32 = Marshal.SizeOf(typeof(UnityEngine.Color32));
        int length = lengthOfColor32 * colors.Length;
        byte[] bytes = new byte[length];

        GCHandle handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            Marshal.Copy(ptr, bytes, 0, length);
        }
        finally
        {
            if (handle != default(GCHandle))
                handle.Free();
        }

        return bytes;
    }

    /// <summary>
    /// Executes the click action.
    /// This will process face3.
    /// </summary>
    public void onClick()
    {
        // How to detect emotions in a Texture.
        // 
        //onClickTexture();

        // How to detect emotions in a Bitmap Texture
        // 
        onClickBitmap();

        // How to detect emotions in an Image.
        // This will need a using System.Drawing; statemnt and System.Drawing.dll from Mono being dropped into the asset folder.
        // Unity does not support .Net Images without this workaround.
        // 
        //onClickImage();
    }

    /// <summary>
    /// Executes the click texture action.
    /// 
    /// Example of how to process a texture.
    /// </summary>
    ///
    /// <remarks>
    /// Demo code.
    /// </remarks>
    public void onClickTexture()
    {
        //! Save both spike detection and averaging.
        //
        Int32 avg = (eda.Settings as EmotionDetectionAssetSettings).Average;
        Boolean spike = (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes;

        Texture2D texture = LoadPNG(face3);

        // Reference (0,0) is BottomLeft !! instead of Topleft.
        // 
        ProcessTexture(texture);

        //! Save both spike detection and averaging.
        //
        (eda.Settings as EmotionDetectionAssetSettings).Average = avg;
        (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes = spike;
    }

    /// <summary>
    /// Executes the click bitmap action.
    /// </summary>
    ///
    /// <remarks>
    /// Demo code.
    /// </remarks>
    public void onClickBitmap()
    {
        // webcam.Stop();

        //! Save both spike detection and averaging.
        //
        Int32 avg = (eda.Settings as EmotionDetectionAssetSettings).Average;
        Boolean spike = (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes;

        System.Drawing.Image img = System.Drawing.Image.FromFile(@"Assets\dump.bmp");

        ProcessImage(img);

        //! Save both spike detection and averaging.
        //
        (eda.Settings as EmotionDetectionAssetSettings).Average = avg;
        (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes = spike;
    }

    /// <summary>
    /// Process the texture described by texture.
    /// </summary>
    ///
    /// <remarks>
    /// Demo code.
    /// </remarks>
    ///
    /// <param name="texture"> The texture. </param>
    private void ProcessTexture(Texture2D texture)
    {
        //UnityEngine.Color c = texture.GetPixel(0, texture.height - 1);

        //// ARGB[255] 253 216 174 (texture bottomleft)
        //// ARGB[255] 229 198 154 (topleft)
        //Debug.LogWarning(String.Format("ARGB [{0:0}] {1:0} {2:0} {3:0}",
        //    c.a * 255,
        //    c.r * 255,
        //    c.g * 255,
        //    c.b * 255));

        Rect r = new Rect(0, 0, texture.width, texture.height);

        Sprite sprite = Sprite.Create(texture, r, Vector2.zero);

        GetComponent<UnityEngine.UI.Image>().sprite = sprite;

        //UnityEngine.UI.Image ui = (UnityEngine.UI.Image)(GameObject.Find("anmimage"));
        //ui.sprite = sprite;

        //Material m = new Material(M
        //animage.maintexture = texture;

        //GUI.DrawTexture(,
        //    texture);

        ProcessColor32(texture.GetPixels32(), texture.width, texture.height);
    }

    /// <summary>
    /// Process the color 32.
    /// </summary>
    ///
    /// <remarks>
    /// This method is used to process raw data from Unity webcam frame textures.
    /// </remarks>
    ///
    /// <param name="pixels"> The pixels. </param>
    /// <param name="width">  The width. </param>
    /// <param name="height"> The height. </param>
    private void ProcessColor32(Color32[] pixels, Int32 width, Int32 height)
    {
        // Convert raw ARGB data into a byte array.
        // 
        byte[] raw = Color32ArrayToByteArray(pixels);

        // Disable Average and SpikeSupression. Needed only for single unrelated images  
        // For video of the same person, adjst this to your need (or disable both lines for default
        // settings) . 
        (eda.Settings as EmotionDetectionAssetSettings).Average = 1;
        (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes = false;


        // Load image into detection 
        // 
        // and  
        // 
        // Try to detect faces. This is the most time consuming part.
        // 
        // Note there the formats supported are limited to 24 and 32 bits RGB at the moment.
        // 
        if (eda.ProcessImage(raw, width, height, true))
        {
            msg.text = String.Format("{0} Face(s detected.", eda.Faces.Count);

            // Process each detected face by detecting the 68 landmarks in each face
            // 
            if (eda.ProcessFaces())
            {
                // Process landmarks into emotions using fuzzy logic.
                // 
                if (eda.ProcessLandmarks())
                {
                    // Extract results.
                    // 
                    Dictionary<string, double> emos = new Dictionary<string, double>();

                    foreach (String emo in eda.Emotions)
                    {
                        // Debug.LogFormat("{0} scores {1}.", emo, eda[0, emo]);

                        // Extract (averaged) emotions of the first face only.
                        // 
                        emos[emo] = eda[0, emo];
                    }

                    //Create the emotion strings.
                    // 
                    emotions.text = String.Join("\r\n", emos.OrderBy(p => p.Key).Select(p => String.Format("{0}={1:0.00}", p.Key, p.Value)).ToArray());
                }
                else
                {
                    emotions.text = "No emotions detected";
                }
            }
            else
            {
                emotions.text = "No landmarks detected";
            }
        }
        else
        {
            msg.text = "No Face(s) detected";
        }
    }

    /// <summary>
    /// Executes the click image action, processed a System.Drawing.Image.
    /// </summary>
    ///
    /// <remarks>
    /// Demo code.
    /// </remarks>
    public void onClickImage()
    {
        System.Drawing.Image img = System.Drawing.Image.FromFile(face3);

        ProcessImage(img);
    }

    /// <summary>
    /// Process the image described by img.
    /// </summary>
    ///
    /// <param name="img"> The image. </param>
    ///
    /// <remarks>
    /// Demo code.
    /// </remarks>
    private void ProcessImage(System.Drawing.Image img)
    {
        // ARGB[255] 253 216 174 (texture bottomleft)
        // ARGB[255] 229 198 154 (topleft)
        //Debug.LogWarning(String.Format("ARGB [{0:0}] {1:0} {2:0} {3:0}",
        //    ((Bitmap)img).GetPixel(0, 0).A,
        //    ((Bitmap)img).GetPixel(0, 0).R,
        //    ((Bitmap)img).GetPixel(0, 0).G,
        //    ((Bitmap)img).GetPixel(0, 0).B));

        Rect r = new Rect(0, 0, img.Width, img.Height);

        (eda.Settings as EmotionDetectionAssetSettings).Average = 1;
        (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes = false;

        if (eda.ProcessImage(img))
        {
            msg.text = String.Format("{0} Face(s detected.", eda.Faces.Count);

            if (eda.ProcessFaces())
            {
                if (eda.ProcessLandmarks())
                {
                    Dictionary<string, double> emos = new Dictionary<string, double>();

                    foreach (String emo in eda.Emotions)
                    {
                        // Debug.LogFormat("{0} scores {1}.", emo, eda[0, emo]);
                        emos[emo] = eda[0, emo];
                    }

                    emotions.text = String.Join("\r\n", emos.OrderBy(p => p.Key).Select(p => String.Format("{0}={1:0.00}", p.Key, p.Value)).ToArray());
                }
                else
                {
                    msg.text = "No emotions detected";
                }
            }
            else
            {
                msg.text = "No landmarks detected";
            }
        }
        else
        {
            msg.text = "No Face(s) detected";
        }
    }
}
