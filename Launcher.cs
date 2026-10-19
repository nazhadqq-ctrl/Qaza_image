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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string serverScript = Path.Combine(baseDir, "server.js");

            // 1. Start background backend service silently (connects to SQL Server at 192.168.3.1:1433)
            bool isRunning = IsPortOpen("127.0.0.1", 4500, 300);
            if (!isRunning && File.Exists(serverScript))
            {
                string nodeExe = FindNodeExe(baseDir);
                if (!string.IsNullOrEmpty(nodeExe))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = nodeExe;
                        psi.Arguments = "\"" + serverScript + "\"";
                        psi.WorkingDirectory = baseDir;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        psi.WindowStyle = ProcessWindowStyle.Hidden;

                        Process.Start(psi);

                        // Wait up to 10 seconds for port 4500 to become active
                        for (int i = 0; i < 20; i++)
                        {
                            Thread.Sleep(500);
                            if (IsPortOpen("127.0.0.1", 4500, 300))
                            {
                                isRunning = true;
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }

            // If the local backend service could not be started, warn user nicely instead of dropping into browser error
            if (!isRunning && !IsPortOpen("127.0.0.1", 4500, 500))
            {
                MessageBox.Show(
                    "سێرڤەری ناوخۆیی بەرنامەکە هێشتا ئامادە نییە.\n\nتکایە دڵنیابە لە هەبوونی فایلی node.exe لە فۆڵدەری بەرنامەکە و پەیوەندی بە سێرڤەری سەرەکی داتابەیس (192.168.3.1:1433).",
                    "سیستەمی Pic_Qaza",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // 2. Open standalone Desktop App window (Pure App Mode)
            // No URL bar, no tabs, no browser chrome, no localhost visible anywhere!
            string browserPath = FindAppBrowser();
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pic_Qaza", "AppData");
            try
            {
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
            }
            catch { }

            string targetUrl = "http://127.0.0.1:4500";

            if (!string.IsNullOrEmpty(browserPath))
            {
                ProcessStartInfo appPsi = new ProcessStartInfo();
                appPsi.FileName = browserPath;
                // CRITICAL: Chromium --app switch must NOT have quotes around the URL to enter pure App Mode!
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

        static string FindNodeExe(string baseDir)
        {
            string localNode = Path.Combine(baseDir, "node.exe");
            if (File.Exists(localNode)) return localNode;

            string pfNode = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"nodejs\node.exe");
            if (File.Exists(pfNode)) return pfNode;

            string pfxNode = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"nodejs\node.exe");
            if (File.Exists(pfxNode)) return pfxNode;

            return "node";
        }

        static bool IsPortOpen(string host, int port, int timeoutMs = 400)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(timeoutMs);
                    return success && client.Connected;
                }
            }
            catch
            {
                return false;
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
