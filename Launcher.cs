using System;
using System.Diagnostics;
using System.Drawing;
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
            string configFile = Path.Combine(baseDir, "app_config.json");

            string serverUrl = "http://127.0.0.1:4500";
            string networkServerUrl = "http://192.168.3.1:4500";
            bool useNetworkServer = true; // Default to network server so client PCs never get stuck on localhost

            // 1. Read app_config.json
            if (File.Exists(configFile))
            {
                try
                {
                    string json = File.ReadAllText(configFile);

                    // Check useNetworkServer setting
                    if (json.IndexOf("\"useNetworkServer\": false", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        useNetworkServer = false;
                    }
                    else if (json.IndexOf("\"useNetworkServer\": true", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        useNetworkServer = true;
                    }

                    // Read networkServerUrl
                    string netUrl = ExtractJsonValue(json, "networkServerUrl");
                    if (!string.IsNullOrEmpty(netUrl))
                    {
                        networkServerUrl = FormatUrl(netUrl);
                    }

                    // Read serverUrl
                    string locUrl = ExtractJsonValue(json, "serverUrl");
                    if (!string.IsNullOrEmpty(locUrl))
                    {
                        serverUrl = FormatUrl(locUrl);
                    }
                }
                catch { }
            }

            string targetUrl = useNetworkServer ? networkServerUrl : serverUrl;

            // 2. Server verification and auto-recovery
            if (useNetworkServer)
            {
                string host;
                int port;
                TryParseHostPort(networkServerUrl, out host, out port);

                bool isReachable = IsPortOpen(host, port, 1000);
                if (!isReachable)
                {
                    // Check if local server is already running as fallback
                    if (IsPortOpen("127.0.0.1", 4500, 300))
                    {
                        targetUrl = "http://127.0.0.1:4500";
                    }
                    else
                    {
                        // Prompt user with friendly connection dialog
                        string newUrl = ShowConnectionDialog(networkServerUrl, configFile);
                        if (string.IsNullOrEmpty(newUrl))
                        {
                            return; // User canceled
                        }
                        targetUrl = newUrl;
                    }
                }
                else
                {
                    targetUrl = networkServerUrl;
                }
            }
            else
            {
                // Local mode: check if local server is running or can be started
                bool isLocalRunning = IsPortOpen("127.0.0.1", 4500, 400);
                if (!isLocalRunning)
                {
                    if (File.Exists(serverScript) && HasNodeJs())
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

                            // Wait up to 6 seconds for port 4500
                            for (int i = 0; i < 12; i++)
                            {
                                Thread.Sleep(500);
                                if (IsPortOpen("127.0.0.1", 4500, 400))
                                {
                                    isLocalRunning = true;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }

                    if (!isLocalRunning)
                    {
                        // Fallback: check if network server is accessible
                        string host;
                        int port;
                        TryParseHostPort(networkServerUrl, out host, out port);
                        if (IsPortOpen(host, port, 1000))
                        {
                            targetUrl = networkServerUrl;
                            SaveConfig(configFile, networkServerUrl, true);
                        }
                        else
                        {
                            string newUrl = ShowConnectionDialog(networkServerUrl, configFile);
                            if (string.IsNullOrEmpty(newUrl))
                            {
                                return;
                            }
                            targetUrl = newUrl;
                        }
                    }
                    else
                    {
                        targetUrl = serverUrl;
                    }
                }
                else
                {
                    targetUrl = serverUrl;
                }
            }

            // 3. Launch App Mode in Edge / Chrome / Brave
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

        static bool HasNodeJs()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("node", "-v");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(1000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static string ExtractJsonValue(string json, string key)
        {
            int idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int colon = json.IndexOf(':', idx);
                int startQuote = json.IndexOf('"', colon + 1);
                int endQuote = json.IndexOf('"', startQuote + 1);
                if (startQuote >= 0 && endQuote > startQuote)
                {
                    return json.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
                }
            }
            return null;
        }

        public static string FormatUrl(string input)
        {
            input = (input ?? "").Trim();
            if (string.IsNullOrEmpty(input)) return "http://192.168.3.1:4500";
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                input = "http://" + input;
            }
            if (!input.Substring(7).Contains(":"))
            {
                input += ":4500";
            }
            return input;
        }

        public static bool TryParseHostPort(string url, out string host, out int port)
        {
            host = "192.168.3.1";
            port = 4500;
            try
            {
                url = FormatUrl(url);
                Uri uri = new Uri(url);
                host = uri.Host;
                port = uri.Port > 0 ? uri.Port : 4500;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPortOpen(string host, int port, int timeoutMs = 800)
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

        public static void SaveConfig(string configFile, string networkUrl, bool useNetwork)
        {
            try
            {
                string json = string.Format(
                    "{{\n  \"serverUrl\": \"http://127.0.0.1:4500\",\n  \"networkServerUrl\": \"{0}\",\n  \"useNetworkServer\": {1},\n  \"notes\": \"بۆ بەکارهێنان لەسەر کۆمپیوتەری تر (Client)، ئەم فایله ناونیشانی سێرڤەری تێدایە.\"\n}}\n",
                    networkUrl,
                    useNetwork ? "true" : "false"
                );
                File.WriteAllText(configFile, json);
            }
            catch { }
        }

        static string ShowConnectionDialog(string currentUrl, string configFile)
        {
            Form form = new Form();
            form.Text = "پەیوەندی بە سێرڤەری Pic_Qaza";
            form.Size = new Size(540, 310);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.BackColor = Color.White;
            form.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Icon
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                try { form.Icon = new Icon(icoPath); } catch { }
            }

            Label lblTitle = new Label();
            lblTitle.Text = "⚠️ نەتوانرا پەیوەندی بە سێرڤەرەوە بکرێت!";
            lblTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(185, 28, 28);
            lblTitle.Location = new Point(24, 18);
            lblTitle.AutoSize = true;

            Label lblDesc = new Label();
            lblDesc.Text = "بەرنامەکە نەیتوانی پەیوەندی بە سێرڤەری ناوخۆیی یان تۆڕەوە بکات.\nئەگەر سێرڤەر لەسەر کۆمپیوتەرێکی ترە، تکایە ئایپی (IP) یان ناونیشانی سێرڤەر بنووسە:";
            lblDesc.ForeColor = Color.FromArgb(71, 85, 105);
            lblDesc.Location = new Point(24, 52);
            lblDesc.Size = new Size(480, 44);

            Label lblInput = new Label();
            lblInput.Text = "ناونیشانی سێرڤەر (Server IP / Host):";
            lblInput.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblInput.Location = new Point(24, 106);
            lblInput.AutoSize = true;

            TextBox txtUrl = new TextBox();
            txtUrl.Text = currentUrl;
            txtUrl.Location = new Point(140, 132);
            txtUrl.Size = new Size(360, 29);
            txtUrl.RightToLeft = RightToLeft.No;

            Button btnTest = new Button();
            btnTest.Text = "پشکنین";
            btnTest.Location = new Point(24, 131);
            btnTest.Size = new Size(105, 31);

            Label lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Location = new Point(24, 168);
            lblStatus.Size = new Size(480, 24);

            btnTest.Click += (s, e) =>
            {
                string testUrl = FormatUrl(txtUrl.Text);
                string h;
                int p;
                TryParseHostPort(testUrl, out h, out p);
                lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
                lblStatus.Text = "پشکنین دەکرێت بۆ " + h + ":" + p + "...";
                Application.DoEvents();

                if (IsPortOpen(h, p, 1200))
                {
                    lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                    lblStatus.Text = "✔ سێرڤەر بەردەستە و پەیوەندی سەرکەوتووبوو!";
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = "❌ پەیوەندی نەبەسترا! دڵنیابەرەوە سێرڤەر کاردەکات و لە هەمان تۆڕدان.";
                }
            };

            Button btnSaveConnect = new Button();
            btnSaveConnect.Text = "پاشەکەوتکردن و چوونەژوورەوە ➔";
            btnSaveConnect.Size = new Size(220, 36);
            btnSaveConnect.Location = new Point(280, 210);
            btnSaveConnect.BackColor = Color.FromArgb(2, 132, 199);
            btnSaveConnect.ForeColor = Color.White;
            btnSaveConnect.FlatStyle = FlatStyle.Flat;
            btnSaveConnect.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button();
            btnCancel.Text = "داخستن";
            btnCancel.Size = new Size(100, 36);
            btnCancel.Location = new Point(24, 210);

            string selectedUrl = null;

            btnSaveConnect.Click += (s, e) =>
            {
                string formatted = FormatUrl(txtUrl.Text);
                SaveConfig(configFile, formatted, true);
                selectedUrl = formatted;
                form.DialogResult = DialogResult.OK;
                form.Close();
            };

            btnCancel.Click += (s, e) =>
            {
                form.DialogResult = DialogResult.Cancel;
                form.Close();
            };

            form.Controls.Add(lblTitle);
            form.Controls.Add(lblDesc);
            form.Controls.Add(lblInput);
            form.Controls.Add(btnTest);
            form.Controls.Add(txtUrl);
            form.Controls.Add(lblStatus);
            form.Controls.Add(btnSaveConnect);
            form.Controls.Add(btnCancel);

            DialogResult dr = form.ShowDialog();
            if (dr == DialogResult.OK)
            {
                return selectedUrl;
            }
            return null;
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
    }
}
