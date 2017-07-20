using UnityEngine;
using System.Collections;
using AssetPackage;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Drawing;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ButtonScript : MonoBehaviour
{
    //! http://answers.unity3d.com/questions/909967/getting-a-web-cam-to-play-on-ui-texture-image.html
    public RawImage rawimage;
    public Text emotions;
    public Text msg;
  
    //! https://answers.unity3d.com/questions/1101792/how-to-post-process-a-webcamtexture-in-realtime.html
    WebCamTexture webcam;
    Texture2D output;
    Color32[] data;

    // Use this for initialization
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        // for debugging purposes, prints available devices to the console
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
        webcam = new WebCamTexture(640, 480);
        rawimage.texture = webcam;
        rawimage.material.mainTexture = webcam;
        //webcam.Play();

        output = new Texture2D(webcam.width, webcam.height);

        GetComponent<Renderer>().material.mainTexture = output;

       // data = new Color32[webcam.width * webcam.height];

        eda = new EmotionDetectionAsset();

        eda.Bridge = new dlib_csharp.Bridge();

        eda.Initialize(@"Assets\", database);

        String[] lines = File.ReadAllLines(furia);
        eda.ParseRules(lines);
  
   	Debug.Log("Emotion detection Ready for Use");
    }

    Int32 frames = 0;

    // Update is called once per frame
    void Update()
    {
    /*
        if (data != null && eda != null && (++frames)%5 > 0)
        {
            webcam.GetPixels32(data);

            ProcessColor32(data, webcam.width, webcam.height);

            //once = false;

            // You can play around with data however you want here.
            // Color32 has member variables of a, r, g, and b. You can read and write them however you want.

            //output.SetPixels32(data);
            //output.Apply();
            if (frames == 0)
            {
                webcam.Stop();
            }
            
            frames = 0;
        }
   */
   }

    const String face3 = @"Assets\Kiavash1.jpg";
    const String furia = @"Assets\FURIA Fuzzy Logic Rules.txt";
    const String database = @"Assets\shape_predictor_68_face_landmarks.dat";

    // http://ericeastwood.com/blog/17/unity-and-dlls-c-managed-and-c-unmanaged
    // https://docs.unity3d.com/Manual/NativePluginInterface.html
    // 
    EmotionDetectionAsset eda;
    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(640, 864,TextureFormat.RGBA32,false);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

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

    public void onClick()
    {
        //onClickTexture();
        onClickBitmap();
        //onClickImage();
    }

    public void onClickTexture()
    {
        // webcam.Stop();
     
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
    /// <param name="pixels">   The pixels. </param>
    /// <param name="width">    The width. </param>
    /// <param name="height">   The height. </param>
    private void ProcessColor32(Color32[] pixels, Int32 width, Int32 height)
    {
        byte[] raw = Color32ArrayToByteArray(pixels);

        (eda.Settings as EmotionDetectionAssetSettings).Average = 1;
        (eda.Settings as EmotionDetectionAssetSettings).SuppressSpikes = false;

        if (eda.ProcessImage(raw, width, height, true))
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

    public void onClickImage()
    {
        System.Drawing.Image img = System.Drawing.Image.FromFile(face3);

        ProcessImage(img);
    }

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
