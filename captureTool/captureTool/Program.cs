﻿using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.IO;

using System.Windows.Forms;
using IronOcr;

namespace ConsoleApp1
{
    static class SendKey
    {
        const UInt32 WM_KEYDOWN = 0x0100;
        const int VK_F5 = 0x74;



    }

    class Hello
    {
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public class ScreenCapture
        {


            public Image CaptureScreen()
            {
                return CaptureWindow(User32.GetDesktopWindow());
            }

            public static IntPtr WinGetHandle(string wName)
            {
                IntPtr hWnd = IntPtr.Zero;
                foreach (Process pList in Process.GetProcesses())
                {
                    if (pList.MainWindowTitle.Contains(wName))
                    {
                        Debug.WriteLine("found the window");
                        hWnd = pList.MainWindowHandle;
                    }
                }
                return hWnd;
            }

            /// <summary>
            /// Creates an Image object containing a screen shot of a specific window
            /// </summary>
            /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
            /// <returns></returns>
            public Image CaptureWindow(IntPtr handle)
            {
                // get te hDC of the target window
                IntPtr hdcSrc = User32.GetWindowDC(handle);
                // get the size
                User32.RECT windowRect = new User32.RECT();
                User32.GetWindowRect(handle, ref windowRect);
                int width = windowRect.right - windowRect.left;
                int height = windowRect.bottom - windowRect.top;
                // create a device context we can copy to
                IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
                // create a bitmap we can copy it to,
                // using GetDeviceCaps to get the width/height
                IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
                // select the bitmap object
                IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
                // bitblt over
                GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
                // restore selection
                GDI32.SelectObject(hdcDest, hOld);
                // clean up 
                GDI32.DeleteDC(hdcDest);
                User32.ReleaseDC(handle, hdcSrc);
                // get a .NET image object for it
                Image img = Image.FromHbitmap(hBitmap);
                // free up the Bitmap object
                GDI32.DeleteObject(hBitmap);
                return img;
            }
            /// <summary>
            /// Captures a screen shot of a specific window, and saves it to a file
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="filename"></param>
            /// <param name="format"></param>
            public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
            {
                Image img = CaptureWindow(handle);
                img.Save(filename, format);
            }
            /// <summary>
            /// Captures a screen shot of the entire desktop, and saves it to a file
            /// </summary>
            /// <param name="filename"></param>
            /// <param name="format"></param>
            public void CaptureScreenToFile(string filename, ImageFormat format)
            {
                Image img = CaptureScreen();
                img.Save(filename, format);
            }

            /// <summary>
            /// Helper class containing Gdi32 API functions
            /// </summary>
            private class GDI32
            {

                public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
                [DllImport("gdi32.dll")]
                public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                    int nWidth, int nHeight, IntPtr hObjectSource,
                    int nXSrc, int nYSrc, int dwRop);
                [DllImport("gdi32.dll")]
                public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                    int nHeight);
                [DllImport("gdi32.dll")]
                public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
                [DllImport("gdi32.dll")]
                public static extern bool DeleteDC(IntPtr hDC);
                [DllImport("gdi32.dll")]
                public static extern bool DeleteObject(IntPtr hObject);
                [DllImport("gdi32.dll")]
                public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
            }

            /// <summary>
            /// Helper class containing User32 API functions
            /// </summary>
            private class User32
            {
                [StructLayout(LayoutKind.Sequential)]
                public struct RECT
                {
                    public int left;
                    public int top;
                    public int right;
                    public int bottom;
                }
                [DllImport("user32.dll")]
                public static extern IntPtr GetDesktopWindow();
                [DllImport("user32.dll")]
                public static extern IntPtr GetWindowDC(IntPtr hWnd);
                [DllImport("user32.dll")]
                public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
                [DllImport("user32.dll")]
                public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
            }
        }


        private static void takescreenshot(string programName, string savePath)
        {
            // take the screenshot
            ScreenCapture screencap = new ScreenCapture();
            IntPtr handle = ScreenCapture.WinGetHandle(programName);
            screencap.CaptureWindowToFile(handle, savePath, ImageFormat.Png);
        }

        private static void launchFile(string fileName, string programName)
        {
            // open a pdf file
            string file = fileName;
            ProcessStartInfo pi = new ProcessStartInfo(file);
            pi.Arguments = Path.GetFileName(file);
            pi.UseShellExecute = true;
            pi.WorkingDirectory = Path.GetDirectoryName(file);
            pi.FileName = programName;
            pi.Verb = "OPEN";
            Process.Start(pi);
        }

        private static void sendKeyToApplication(Process[] processes, string command)
        {
            foreach (Process proc in processes)
            {
                IntPtr h = proc.MainWindowHandle;
                SetForegroundWindow(h);
                SendKeys.SendWait(command);
                //Console.WriteLine("sent to " + proc.ToString());
                break;
            }
        }

        private static void killProcess(Process[] processes)
        {
            foreach (Process proc in processes)
            {
                try
                {
                    //proc.Close();
                    proc.Kill();
                    //proc.CloseMainWindow();
                    //proc.WaitForExit();
                }
                catch (System.NullReferenceException)
                {
                    Console.WriteLine("No instance of the app running");
                }
            }
        }

        private static Boolean checkErrorMessage(string imagePath)
        {
            Boolean hasError = false;

            /*
            Console.WriteLine("Checking for error messages...");
            AutoOcr Ocr = new AutoOcr();
            OcrResult result = Ocr.Read(imagePath);
            Console.WriteLine("Result:");
            Console.WriteLine(result.Text);
            */
            
            AdvancedOcr OcrAdvanced = new AdvancedOcr()
            {
                CleanBackgroundNoise = false,
                EnhanceContrast = false,
                EnhanceResolution = false,
                Language = IronOcr.Languages.English.OcrLanguagePack,
                Strategy = IronOcr.AdvancedOcr.OcrStrategy.Advanced,
                ColorSpace = AdvancedOcr.OcrColorSpace.Color,
                DetectWhiteTextOnDarkBackgrounds = true,
                InputImageType = AdvancedOcr.InputTypes.AutoDetect,
                RotateAndStraighten = false,
                ReadBarCodes = false,
                ColorDepth = 4
            };
            OcrResult result = OcrAdvanced.Read(imagePath);
            Console.WriteLine(result.Text);

            String[] errorTerms = new string[] { "File", "Edit", "Home", "Tools" };
            String resultStr = result.Text;
            foreach (String badTerm in errorTerms)
            {
                if (resultStr.Contains(badTerm))
                {
                    Console.WriteLine("detected error message with term: " + badTerm);
                    hasError = true;
                }
            }
            
            return hasError;
        }

        private static Boolean CropImage(int x, int y, int width, int height, string imagePath, string cropPath)
        {
            Boolean hasFailed = false;

            //using (var originalImage = new Bitmap(imagePath))
            var originalImage = new Bitmap(imagePath);
            // check dimension of the file
            if (originalImage.Height == 1 && originalImage.Width == 1)
            {
                hasFailed = true;
            }

            if (hasFailed == false)
            {
                Bitmap croppedImage;
                Rectangle crop = new Rectangle(x, y, width, height);
                croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);
                //croppedImage = new Bitmap(originalImage);
                croppedImage.Save(cropPath, ImageFormat.Png);
                croppedImage.Dispose();
                croppedImage = null;
            }

            originalImage.Dispose();
            originalImage = null;

            return hasFailed;
        }

        private static void processFile(string targetFile, string screenshotFolder)
        {
            /*
            foreach (Process PPath in Process.GetProcesses())
            {
                Console.WriteLine(PPath.ProcessName.ToString());
            }
            */

            String pureFileName = Path.GetFileName(targetFile);
            String screenshotSavePath = screenshotFolder + pureFileName.Substring(0, pureFileName.Length - 4) + "-acroread.png";
            //String screenshotSavePath = screenshotFolder + "screenshot.png";
            Console.WriteLine(screenshotSavePath);

            // https://stackoverflow.com/questions/42498440/sendkeys-to-a-specific-program-without-it-being-in-focus
            launchFile(targetFile, "C:\\Program Files (x86)\\Adobe\\Acrobat Reader DC\\Reader\\AcroRd32.exe");


            Process[] processes = Process.GetProcessesByName("AcroRd32");
            foreach (Process proc in processes)
            {
                ShowWindow(proc.MainWindowHandle, 3);
            }

            Thread.Sleep(3000);

            processes = Process.GetProcessesByName("AcroRd32");
            if (processes.Length == 2)
            {
                // try full screen it
                sendKeyToApplication(processes, "{ENTER}");
                Thread.Sleep(200);
                sendKeyToApplication(processes, "^(l)");

                Thread.Sleep(2000);

                // take screenshot
                takescreenshot("Adobe", screenshotSavePath);
                //CaptureScreenToFile(@"C:\Users\edmund\Desktop\screenshot.png", ImageFormat.Png);
                //string cropPath = @"C:\Users\edmund\Desktop\cropped.png";

                //CropImage(0, 0, 300, 100, screenshotSavePath, cropPath);
                // check if the screenshot located an error message
                //Boolean hasError = checkErrorMessage(cropPath);

                // close the application depending if there is an error

                // send enter command and full screen command
                Console.WriteLine("Closing the app");
                sendKeyToApplication(processes, "{ESC}");
                Thread.Sleep(2000);
                sendKeyToApplication(processes, "{ENTER}");
                Thread.Sleep(200);
                sendKeyToApplication(processes, "%{F4}");
                Thread.Sleep(200);

                processes = Process.GetProcessesByName("AcroRd32");
                if (processes.Length == 2)
                {
                    sendKeyToApplication(processes, "n");
                }

                Thread.Sleep(1000);

            }

            // kill and clean environment before we start the screenshot
            processes = Process.GetProcessesByName("AcroRd32");
            killProcess(processes);
            
            Thread.Sleep(1000);
        }

        private static void batchProcess()
        {
            // get list of files from folder
            string[] filePaths = Directory.GetFiles(@"C:\Users\edmund\Desktop\testSuite\masterPool\");
            //string[] filePaths = Directory.GetFiles(@"C:\Users\edmund\Desktop\testSuite\test\");
            string screenshotFolder = @"C:\Users\edmund\Desktop\testSuite\screenshot\";

            // check if screenshot folder exists
            try
            {
                if (!Directory.Exists(screenshotFolder))
                {
                    Console.WriteLine("deleting the old folder");
                    Directory.CreateDirectory(screenshotFolder);
                }
                else
                {
                    Console.WriteLine("deleting the old folder and recreating it");
                    Directory.Delete(screenshotFolder, true);
                    Directory.CreateDirectory(screenshotFolder);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception with the screenshot folder:" + e);
            }

            // kill and clean environment before we start the screenshot
            Process[] processes = Process.GetProcessesByName("AcroRd32");
            killProcess(processes);

            foreach (string pdfFilePath in filePaths)
            {
                Console.WriteLine(pdfFilePath);
                processFile(pdfFilePath, screenshotFolder);
            }

        }

        static void Main()
        {
            batchProcess();
            /*
            foreach (Process PPath in Process.GetProcesses())
            {
                Console.WriteLine(PPath.ProcessName.ToString());
            }

            // https://stackoverflow.com/questions/42498440/sendkeys-to-a-specific-program-without-it-being-in-focus
            launchFile(@"C:\Users\edmund\Desktop\minimal.pdf", "C:\\Program Files (x86)\\Adobe\\Acrobat Reader DC\\Reader\\AcroRd32.exe");

            Thread.Sleep(3000);

            Process[] processes = Process.GetProcessesByName("AcroRd32");
            foreach (Process proc in processes)
            {
                ShowWindow(proc.MainWindowHandle, 3);
            }


            string imagePath = @"C:\Users\edmund\Desktop\screenshot.png";

            takescreenshot("Adobe", imagePath);

            // check if the screenshot located an error message
            Boolean hasError = checkErrorMessage(imagePath);

            if (!hasError)
            {
                // send enter command and full screen command
                Console.WriteLine("Total number of processes: " + processes.Length);
                sendKeyToApplication(processes, "{ENTER}");
                sendKeyToApplication(processes, "^(l)");

                Thread.Sleep(3000);
                
                sendKeyToApplication(processes, "{ESC}");
                sendKeyToApplication(processes, "%{F4}");
            } else
            {
                // send enter command only and close it
                sendKeyToApplication(processes, "{ENTER}");
                sendKeyToApplication(processes, "%{F4}");

                //killProcess(processes);
            }
            */
            Console.WriteLine("Press any key to exit the tool..");
            Console.Read();
            
        }

    }
}
