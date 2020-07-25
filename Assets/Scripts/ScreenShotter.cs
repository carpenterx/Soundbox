#if (UNITY_EDITOR) 
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ScreenShotter : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            TakeScreenshot();
        }
    }

    public void TakeScreenshot()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "screenshots", Application.productName);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string fileName = Application.productName + " screenshot " + DateTime.Now.ToString("HH-mm-ss-ffff", CultureInfo.InvariantCulture) + ".png";
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
    }
}
#endif