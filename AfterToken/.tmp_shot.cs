var dir = "Assets/Screenshots";
if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
UnityEngine.ScreenCapture.CaptureScreenshot(dir + "/settings_ai_tab.png");
return "shot";
