using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PicQaza
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configFile = Path.Combine(baseDir, "app_config.json");

            // Server is at 192.168.3.1 on local network port 4500
            string targetUrl = "http://192.168.3.1:4500";

            if (File.Exists(configFile))
            {
                try
                {
                    string json = File.ReadAllText(configFile);
                    int idx = json.IndexOf("\"networkServerUrl\"", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        int colon = json.IndexOf(':', idx);
                        int startQuote = json.IndexOf('"', colon + 1);
                        int endQuote = json.IndexOf('"', startQuote + 1);
                        if (startQuote >= 0 && endQuote > startQuote)
                        {
                            string url = json.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
                            if (!string.IsNullOrEmpty(url))
                            {
                                targetUrl = url;
                            }
                        }
                    }
                }
                catch { }
            }

            targetUrl = targetUrl.Trim();
            if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                targetUrl = "http://" + targetUrl;
            }

            // Locate browser engine for standalone App Mode (Edge, Chrome, Brave)
            string browserPath = FindAppBrowser();

            // Set up a dedicated, isolated application profile directory
            // This guarantees that Edge/Chrome opens as a standalone desktop app window without browser tabs or address bar
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pic_Qaza", "AppData");
            try
            {
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(browserPath))
                {
                    ProcessStartInfo appPsi = new ProcessStartInfo();
                    appPsi.FileName = browserPath;
                    // CRITICAL: Chromium --app switch must NOT have quotes around the URL!
                    // Example: --app=http://192.168.3.1:4500 --user-data-dir="C:\..."
                    appPsi.Arguments = string.Format(
                        "--app={0} --user-data-dir=\"{1}\" --app-id=Pic_Qaza --window-size=1360,860 --window-position=center --no-first-run --no-default-browser-check --disable-features=Translate,OptimizationHints,MediaRouter --disable-extensions --disable-component-update --disable-background-networking --disable-sync",
                        targetUrl,
                        appDataDir
                    );
                    appPsi.UseShellExecute = true;

                    Process.Start(appPsi);
                }
                else
                {
                    Process.Start(targetUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("هەڵە لە کردنەوەی بەرنامەی دیسکتۆپ:\n" + ex.Message, "Pic_Qaza", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string FindAppBrowser()
        {
            string[] edgePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\Application\msedge.exe")
            };

            foreach (string p in edgePaths)
            {
                if (File.Exists(p)) return p;
            }

            string[] chromePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
            };

            foreach (string p in chromePaths)
            {
                if (File.Exists(p)) return p;
            }

            string[] bravePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\Application\brave.exe")
            };

            foreach (string p in bravePaths)
            {
                if (File.Exists(p)) return p;
            }

            return null;
        }
    }
}
