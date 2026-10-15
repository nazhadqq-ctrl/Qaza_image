using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace PicQaza
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string serverScript = Path.Combine(baseDir, "server.js");
            string configFile = Path.Combine(baseDir, "app_config.json");

            string targetUrl = "http://127.0.0.1:4500";
            string remoteServerUrl = "http://192.168.3.1:4500";
            bool useNetworkServer = false;

            // Read app_config.json if present
            if (File.Exists(configFile))
            {
                try
                {
                    string json = File.ReadAllText(configFile);
                    if (json.IndexOf("\"useNetworkServer\": true", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        useNetworkServer = true;
                    }

                    int idx = json.IndexOf("\"networkServerUrl\"", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        int colon = json.IndexOf(':', idx);
                        int startQuote = json.IndexOf('"', colon + 1);
                        int endQuote = json.IndexOf('"', startQuote + 1);
                        if (startQuote >= 0 && endQuote > startQuote)
                        {
                            remoteServerUrl = json.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
                        }
                    }

                    if (useNetworkServer)
                    {
                        targetUrl = remoteServerUrl;
                    }
                }
                catch { }
            }

            // Local mode: start local Node.js server if not in network mode
            if (!useNetworkServer)
            {
                if (File.Exists(serverScript))
                {
                    bool isRunning = IsPortOpen("127.0.0.1", 4500);
                    if (!isRunning)
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = "cmd.exe";
                            psi.Arguments = "/c node \"" + serverScript + "\"";
                            psi.WorkingDirectory = baseDir;
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;
                            psi.WindowStyle = ProcessWindowStyle.Hidden;

                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("هەڵە لە دەستپێکردنی سێرڤەری بەرنامە:\n" + ex.Message, "Pic_Qaza", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Wait up to 6 seconds for port to open
                        for (int i = 0; i < 12; i++)
                        {
                            Thread.Sleep(500);
                            if (IsPortOpen("127.0.0.1", 4500))
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // If local server.js does not exist, check if remote server is reachable
                    if (IsPortOpen("192.168.3.1", 4500))
                    {
                        targetUrl = remoteServerUrl;
                    }
                    else
                    {
                        MessageBox.Show("فایلی server.js نەدۆزرایەوە لە:\n" + serverScript + "\n\nتکایە دڵنیابەرەوە بەرنامەکە بە تەواوی دامەزراوە.", "Pic_Qaza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // Locate browser engine for App Mode (Edge, Chrome, Brave)
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
                    appPsi.Arguments = string.Format(
                        "--app=\"{0}\" --user-data-dir=\"{1}\" --app-id=Pic_Qaza --window-size=1300,860 --window-position=center --no-first-run --no-default-browser-check --disable-features=Translate,OptimizationHints,MediaRouter --disable-extensions",
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
                MessageBox.Show("هەڵە لە کردنەوەی پەنجەرەی بەرنامەی دیسکتۆپ:\n" + ex.Message, "Pic_Qaza", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string FindAppBrowser()
        {
            // 1. Microsoft Edge
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

            // 2. Google Chrome
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

            // 3. Brave Browser
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

        static bool IsPortOpen(string host, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(400);
                    return success && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
