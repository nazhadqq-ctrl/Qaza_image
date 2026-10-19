using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;

namespace PicQazaInstaller
{
    public class SetupWizardForm : Form
    {
        private int currentStep = 1;
        private const int TotalSteps = 4;

        // Top Banner
        private Panel bannerPanel;
        private Label lblBannerTitle;
        private Label lblBannerSub;

        // Bottom Controls
        private Panel bottomPanel;
        private Button btnBack;
        private Button btnNext;
        private Button btnCancel;

        // Pages Panels
        private Panel pageWelcome;
        private Panel pageFolder;
        private Panel pageProgress;
        private Panel pageFinished;

        // Folder & Server Config page
        private TextBox txtInstallPath;
        private Button btnBrowse;
        private RadioButton rbClientMode;
        private RadioButton rbServerMode;
        private TextBox txtServerUrl;
        private Button btnTestServer;
        private Label lblServerStatus;
        private CheckBox chkDesktopShortcut;
        private CheckBox chkStartMenuShortcut;
        private CheckBox chkLaunchAfter;

        // Progress page
        private ProgressBar progressBar;
        private Label lblStatus;
        private BackgroundWorker installWorker;

        // Error message if any
        private string installError = null;

        public SetupWizardForm()
        {
            InitializeComponent();
            ShowStep(1);
        }

        private void InitializeComponent()
        {
            this.Text = "ڕێبەری دامەزراندنی بەرنامەی Pic_Qaza";
            this.Size = new Size(720, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.White;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                try { this.Icon = new Icon(icoPath); } catch { }
            }

            // 1. Banner
            bannerPanel = new Panel();
            bannerPanel.Dock = DockStyle.Top;
            bannerPanel.Height = 78;
            bannerPanel.BackColor = Color.FromArgb(248, 250, 252);
            bannerPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, bannerPanel.Height - 1, bannerPanel.Width, bannerPanel.Height - 1);
            };

            lblBannerTitle = new Label();
            lblBannerTitle.Text = "📦 دامەزرێنەری بەرنامەی Pic_Qaza (Setup Wizard)";
            lblBannerTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblBannerTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblBannerTitle.AutoSize = true;
            lblBannerTitle.Location = new Point(24, 14);

            lblBannerSub = new Label();
            lblBannerSub.Text = "بەخێربێن بۆ ڕێبەری دامەزراندنی فۆڕمی کۆگای وێنە و پسووڵەکان";
            lblBannerSub.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblBannerSub.ForeColor = Color.FromArgb(100, 116, 139);
            lblBannerSub.AutoSize = true;
            lblBannerSub.Location = new Point(26, 44);

            bannerPanel.Controls.Add(lblBannerTitle);
            bannerPanel.Controls.Add(lblBannerSub);
            this.Controls.Add(bannerPanel);

            // 2. Bottom Action Panel
            bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 60;
            bottomPanel.BackColor = Color.FromArgb(241, 245, 249);
            bottomPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, 0, bottomPanel.Width, 0);
            };

            btnCancel = new Button();
            btnCancel.Text = "پاشگەزبوونەوە";
            btnCancel.Size = new Size(110, 34);
            btnCancel.Location = new Point(20, 13);
            btnCancel.Click += (s, e) => this.Close();

            btnBack = new Button();
            btnBack.Text = "⬅ پێشوو";
            btnBack.Size = new Size(95, 34);
            btnBack.Location = new Point(480, 13);
            btnBack.Click += (s, e) => ShowStep(currentStep - 1);

            btnNext = new Button();
            btnNext.Text = "دواتر ➔";
            btnNext.Size = new Size(110, 34);
            btnNext.Location = new Point(585, 13);
            btnNext.BackColor = Color.FromArgb(2, 132, 199);
            btnNext.ForeColor = Color.White;
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += OnNextClick;

            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnBack);
            bottomPanel.Controls.Add(btnNext);
            this.Controls.Add(bottomPanel);

            // ================= PAGE 1: WELCOME =================
            pageWelcome = new Panel();
            pageWelcome.Dock = DockStyle.Fill;
            pageWelcome.Padding = new Padding(30);

            Label lblWelcomeTitle = new Label();
            lblWelcomeTitle.Text = "بەخێربێن بۆ سیستەمی دامەزراندنی بەرنامەی Pic_Qaza";
            lblWelcomeTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblWelcomeTitle.ForeColor = Color.FromArgb(30, 41, 59);
            lblWelcomeTitle.Location = new Point(30, 25);
            lblWelcomeTitle.AutoSize = true;

            Label lblWelcomeText = new Label();
            lblWelcomeText.Text = "ئەم ڕێبەرە بەرنامەی Pic_Qaza بە شێوەیەکی سەربەخۆ لەسەر کۆمپیوتەرەکەت دادەمەزرێنێت،\nبەبێ ئەوەی کێشەی نەدۆزینەوەی لۆکاڵ هۆست یان پێویستی بە داتابەیسی ناوخۆیی هەبێت.\n\nتایبەتمەندییە سەرەکییەکان:\n • کارکردنی ئاسان لەسەر دیسکتۆپی هەموو یوزەرەکان لە شوێنە جیاوازەکان\n • پەیوەستبوونی ڕاستەوخۆ بە سێرڤەری سەرەکی\n • تۆمارکردن، ئەرشیف و چاپکردنی خێرای پسووڵەکان بە قەبارەی ستانداردی A5\n • دروستکردنی شۆرتکەتی تایبەت لەسەر شاشەی دیسکتۆپ بە ئایکۆنی 3D\n\nتکایە کلیک لە دوگمەی 'دواتر ➔' بکە بۆ دیاریکردنی هەڵبژاردنەکان.";
            lblWelcomeText.Location = new Point(30, 70);
            lblWelcomeText.Size = new Size(640, 220);
            lblWelcomeText.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblWelcomeText.ForeColor = Color.FromArgb(51, 65, 85);

            pageWelcome.Controls.Add(lblWelcomeTitle);
            pageWelcome.Controls.Add(lblWelcomeText);
            this.Controls.Add(pageWelcome);

            // ================= PAGE 2: SETTINGS & SERVER =================
            pageFolder = new Panel();
            pageFolder.Dock = DockStyle.Fill;
            pageFolder.Padding = new Padding(24);

            // Group: Install Path
            Label lblFolderTitle = new Label();
            lblFolderTitle.Text = "١. شوێنی دابەزاندنی فایلەکان:";
            lblFolderTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblFolderTitle.Location = new Point(24, 12);
            lblFolderTitle.AutoSize = true;

            txtInstallPath = new TextBox();
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "qaza_image");
            txtInstallPath.Text = defaultPath;
            txtInstallPath.Location = new Point(120, 38);
            txtInstallPath.Size = new Size(550, 29);
            txtInstallPath.RightToLeft = RightToLeft.No;

            btnBrowse = new Button();
            btnBrowse.Text = "گەڕان...";
            btnBrowse.Location = new Point(24, 37);
            btnBrowse.Size = new Size(88, 31);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "تکایە بوخچەی دامەزراندنی Pic_Qaza هەڵبژێرە:";
                    fbd.SelectedPath = txtInstallPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtInstallPath.Text = fbd.SelectedPath;
                    }
                }
            };

            // Group: Deployment Mode
            Label lblModeTitle = new Label();
            lblModeTitle.Text = "٢. شێوازی کارکردن (Mode):";
            lblModeTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblModeTitle.Location = new Point(24, 82);
            lblModeTitle.AutoSize = true;

            rbClientMode = new RadioButton();
            rbClientMode.Text = "💻 کۆمپیوتەری بەکارهێنەر (Client Desktop) - پەیوەستبوون بە سێرڤەری سەرەکی [پێشنیارکراو]";
            rbClientMode.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbClientMode.ForeColor = Color.FromArgb(2, 132, 199);
            rbClientMode.Checked = true;
            rbClientMode.Location = new Point(24, 110);
            rbClientMode.AutoSize = true;

            rbServerMode = new RadioButton();
            rbServerMode.Text = "🖥️ سێرڤەری سەرەکی (Main Server - Localhost) - تەنها بۆ ئەو کۆمپیوتەرەی بنکەدراوەکەی لەسەرە";
            rbServerMode.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            rbServerMode.ForeColor = Color.FromArgb(71, 85, 105);
            rbServerMode.Location = new Point(24, 138);
            rbServerMode.AutoSize = true;

            // Group: Server URL
            Label lblServerTitle = new Label();
            lblServerTitle.Text = "٣. ناونیشانی سێرڤەری سەرەکی (Server IP / Host):";
            lblServerTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblServerTitle.Location = new Point(24, 175);
            lblServerTitle.AutoSize = true;

            txtServerUrl = new TextBox();
            txtServerUrl.Text = "http://192.168.3.1:4500";
            txtServerUrl.Location = new Point(150, 203);
            txtServerUrl.Size = new Size(520, 29);
            txtServerUrl.RightToLeft = RightToLeft.No;

            btnTestServer = new Button();
            btnTestServer.Text = "پشکنین";
            btnTestServer.Location = new Point(24, 202);
            btnTestServer.Size = new Size(115, 31);

            lblServerStatus = new Label();
            lblServerStatus.Text = "ئامادەیە بۆ بەستنەوە بە سێرڤەری داتابەیس.";
            lblServerStatus.ForeColor = Color.FromArgb(100, 116, 139);
            lblServerStatus.Location = new Point(24, 238);
            lblServerStatus.Size = new Size(645, 22);

            btnTestServer.Click += (s, e) =>
            {
                string u = FormatUrl(txtServerUrl.Text);
                string h;
                int p;
                TryParseHostPort(u, out h, out p);
                lblServerStatus.ForeColor = Color.FromArgb(100, 116, 139);
                lblServerStatus.Text = "پشکنین دەکرێت بۆ " + h + ":" + p + "...";
                Application.DoEvents();

                if (IsPortOpen(h, p, 1000))
                {
                    lblServerStatus.ForeColor = Color.FromArgb(22, 163, 74);
                    lblServerStatus.Text = "✔ پەیوەندی سەرکەوتووبوو! سێرڤەر بەردەستە و بە تەواوی ئیش دەکات.";
                }
                else
                {
                    lblServerStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblServerStatus.Text = "⚠️ پەیوەندی نەبەسترا! دڵنیابەرەوە سێرڤەری سەرەکی داگیرساوە و لە هەمان تۆڕدان.";
                }
            };

            rbServerMode.CheckedChanged += (s, e) =>
            {
                if (rbServerMode.Checked)
                {
                    txtServerUrl.Text = "http://127.0.0.1:4500";
                    txtServerUrl.Enabled = false;
                    btnTestServer.Enabled = false;
                    lblServerStatus.Text = "دۆخی سێرڤەری ناوخۆیی (Localhost) هەڵبژێردراوە.";
                }
                else
                {
                    txtServerUrl.Text = "http://192.168.3.1:4500";
                    txtServerUrl.Enabled = true;
                    btnTestServer.Enabled = true;
                    lblServerStatus.Text = "ئامادەیە بۆ بەستنەوە بە سێرڤەری تۆڕ.";
                }
            };

            // Group: Shortcuts
            chkDesktopShortcut = new CheckBox();
            chkDesktopShortcut.Text = "دروستکردنی شۆرتکەت لەسەر شاشەی دیسکتۆپ (Pic_Qaza)";
            chkDesktopShortcut.Checked = true;
            chkDesktopShortcut.Location = new Point(24, 275);
            chkDesktopShortcut.AutoSize = true;

            chkStartMenuShortcut = new CheckBox();
            chkStartMenuShortcut.Text = "زیادکردنی بەرنامەکە بۆ لیستی دەستپێک (Start Menu)";
            chkStartMenuShortcut.Checked = true;
            chkStartMenuShortcut.Location = new Point(24, 305);
            chkStartMenuShortcut.AutoSize = true;

            chkLaunchAfter = new CheckBox();
            chkLaunchAfter.Text = "کردنەوەی بەرنامەی Pic_Qaza ڕاستەوخۆ دوای تەواوبوونی دامەزراندن";
            chkLaunchAfter.Checked = true;
            chkLaunchAfter.Location = new Point(24, 335);
            chkLaunchAfter.AutoSize = true;

            pageFolder.Controls.Add(lblFolderTitle);
            pageFolder.Controls.Add(btnBrowse);
            pageFolder.Controls.Add(txtInstallPath);
            pageFolder.Controls.Add(lblModeTitle);
            pageFolder.Controls.Add(rbClientMode);
            pageFolder.Controls.Add(rbServerMode);
            pageFolder.Controls.Add(lblServerTitle);
            pageFolder.Controls.Add(btnTestServer);
            pageFolder.Controls.Add(txtServerUrl);
            pageFolder.Controls.Add(lblServerStatus);
            pageFolder.Controls.Add(chkDesktopShortcut);
            pageFolder.Controls.Add(chkStartMenuShortcut);
            pageFolder.Controls.Add(chkLaunchAfter);
            this.Controls.Add(pageFolder);

            // ================= PAGE 3: PROGRESS =================
            pageProgress = new Panel();
            pageProgress.Dock = DockStyle.Fill;
            pageProgress.Padding = new Padding(30);

            Label lblProgTitle = new Label();
            lblProgTitle.Text = "تکایە چاوەڕوانبە، بەرنامەکە دادەمەزرێت...";
            lblProgTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblProgTitle.Location = new Point(30, 30);
            lblProgTitle.AutoSize = true;

            progressBar = new ProgressBar();
            progressBar.Location = new Point(30, 80);
            progressBar.Size = new Size(640, 28);
            progressBar.Style = ProgressBarStyle.Continuous;

            lblStatus = new Label();
            lblStatus.Text = "ئامادەکاری بۆ دەرهێنانی فایلەکان...";
            lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
            lblStatus.Location = new Point(30, 120);
            lblStatus.Size = new Size(640, 60);

            pageProgress.Controls.Add(lblProgTitle);
            pageProgress.Controls.Add(progressBar);
            pageProgress.Controls.Add(lblStatus);
            this.Controls.Add(pageProgress);

            // ================= PAGE 4: FINISHED =================
            pageFinished = new Panel();
            pageFinished.Dock = DockStyle.Fill;
            pageFinished.Padding = new Padding(30);

            Label lblFinishTitle = new Label();
            lblFinishTitle.Text = "✔ پیرۆزە! بەرنامەی Pic_Qaza بە سەرکەوتوویی دامەزرا";
            lblFinishTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblFinishTitle.ForeColor = Color.FromArgb(16, 185, 129);
            lblFinishTitle.Location = new Point(30, 30);
            lblFinishTitle.AutoSize = true;

            Label lblFinishDesc = new Label();
            lblFinishDesc.Text = "سەرجەم فایلە پێویستەکان جێگیرکران و ڕێکخستنی سێرڤەر بە سەرکەوتوویی تەواوبوو.\nشۆرتکەتی بەرنامەکە خرایە سەر شاشەی دیسکتۆپ.\n\nکلیک لەسەر دوگمەی 'تەواوکردن' بکە بۆ داخستنی ئەم ڕێبەرە.";
            lblFinishDesc.Location = new Point(30, 85);
            lblFinishDesc.Size = new Size(640, 140);
            lblFinishDesc.ForeColor = Color.FromArgb(51, 65, 85);

            pageFinished.Controls.Add(lblFinishTitle);
            pageFinished.Controls.Add(lblFinishDesc);
            this.Controls.Add(pageFinished);

            // Worker setup
            installWorker = new BackgroundWorker();
            installWorker.WorkerReportsProgress = true;
            installWorker.DoWork += InstallWorker_DoWork;
            installWorker.ProgressChanged += (s, e) =>
            {
                progressBar.Value = Math.Min(100, Math.Max(0, e.ProgressPercentage));
                if (e.UserState != null)
                {
                    lblStatus.Text = e.UserState.ToString();
                }
            };
            installWorker.RunWorkerCompleted += InstallWorker_RunWorkerCompleted;
        }

        private void ShowStep(int step)
        {
            currentStep = step;
            pageWelcome.Visible = (step == 1);
            pageFolder.Visible = (step == 2);
            pageProgress.Visible = (step == 3);
            pageFinished.Visible = (step == 4);

            btnBack.Enabled = (step == 2);
            btnBack.Visible = (step <= 2);
            btnCancel.Enabled = (step <= 2 || step == 4);

            if (step == 1)
            {
                btnNext.Text = "دواتر ➔";
                lblBannerSub.Text = "بەخێربێن بۆ ڕێبەری دامەزراندنی فۆڕمی کۆگای وێنە و پسووڵەکان";
            }
            else if (step == 2)
            {
                btnNext.Text = "دامەزراندن ➔";
                lblBannerSub.Text = "دیاریکردنی شوێنی دابەزاندن، جۆری کارکردن و ناونیشانی سێرڤەر";
            }
            else if (step == 3)
            {
                btnNext.Enabled = false;
                btnBack.Enabled = false;
                btnCancel.Enabled = false;
                lblBannerSub.Text = "دەرهێنان و جێگیرکردنی فایلەکان لەسەر کۆمپیوتەر...";
            }
            else if (step == 4)
            {
                btnNext.Enabled = true;
                btnNext.Text = "تەواوکردن ✔";
                btnBack.Visible = false;
                btnCancel.Visible = false;
                lblBannerSub.Text = "دامەزراندنی بەرنامە بە سەرکەوتوویی جێبەجێکرا";
            }
        }

        private void OnNextClick(object sender, EventArgs e)
        {
            if (currentStep == 1)
            {
                ShowStep(2);
            }
            else if (currentStep == 2)
            {
                string targetPath = txtInstallPath.Text.Trim();
                if (string.IsNullOrEmpty(targetPath))
                {
                    MessageBox.Show("تکایە شوێنی دابەزاندنی فایلەکان دیاری بکە.", "ئاگاداری", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ShowStep(3);

                // Pass install parameters
                InstallParams p = new InstallParams();
                p.TargetDir = targetPath;
                p.IsClientMode = rbClientMode.Checked;
                p.ServerUrl = FormatUrl(txtServerUrl.Text);
                p.CreateDesktopShortcut = chkDesktopShortcut.Checked;
                p.CreateStartShortcut = chkStartMenuShortcut.Checked;

                installWorker.RunWorkerAsync(p);
            }
            else if (currentStep == 4)
            {
                if (chkLaunchAfter.Checked)
                {
                    string targetPath = txtInstallPath.Text.Trim();
                    string exePath = Path.Combine(targetPath, "Pic_Qaza.exe");
                    if (File.Exists(exePath))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = targetPath });
                        }
                        catch { }
                    }
                }
                this.Close();
            }
        }

        private class InstallParams
        {
            public string TargetDir;
            public bool IsClientMode;
            public string ServerUrl;
            public bool CreateDesktopShortcut;
            public bool CreateStartShortcut;
        }

        private void InstallWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            InstallParams param = (InstallParams)e.Argument;
            string targetDir = param.TargetDir;
            installError = null;

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                installWorker.ReportProgress(5, "دەرهێنانی فایلی دامەزراندن...");

                Assembly asm = Assembly.GetExecutingAssembly();
                string resourceName = null;
                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = name;
                        break;
                    }
                }

                if (resourceName == null)
                {
                    throw new Exception("فایلی ناوەکی payload.zip نەدۆزرایەوە لەناو فایلی دامەزرێنەر.");
                }

                string tempZip = Path.Combine(Path.GetTempPath(), "pic_qaza_" + Guid.NewGuid().ToString("N") + ".zip");
                using (Stream resStream = asm.GetManifestResourceStream(resourceName))
                {
                    using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                    {
                        resStream.CopyTo(fs);
                    }
                }

                installWorker.ReportProgress(15, "دەرهێنانی فایلەکان بۆ: " + targetDir);

                using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                {
                    int count = archive.Entries.Count;
                    int idx = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        idx++;
                        string destPath = Path.Combine(targetDir, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            entry.ExtractToFile(destPath, true);
                        }

                        int pct = 15 + (int)(((double)idx / count) * 75.0);
                        installWorker.ReportProgress(pct, "دامەزراندنی: " + entry.Name);
                    }
                }

                try { File.Delete(tempZip); } catch { }

                // Save custom app_config.json
                string configPath = Path.Combine(targetDir, "app_config.json");
                string configJson = string.Format(
                    "{{\n  \"serverUrl\": \"http://127.0.0.1:4500\",\n  \"networkServerUrl\": \"{0}\",\n  \"useNetworkServer\": {1},\n  \"notes\": \"بۆ بەکارهێنان لەسەر کۆمپیوتەری تر (Client)، ئەم فایله ناونیشانی سێرڤەری تێدایە.\"\n}}\n",
                    param.ServerUrl,
                    param.IsClientMode ? "true" : "false"
                );
                File.WriteAllText(configPath, configJson);

                // Ensure uploads directory exists
                string uploadsDir = Path.Combine(targetDir, @"public\uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Create Desktop Shortcut
                if (param.CreateDesktopShortcut)
                {
                    installWorker.ReportProgress(92, "دروستکردنی شۆرتکەت لەسەر دیسکتۆپ...");
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string shortcutFile = Path.Combine(desktopPath, "Pic_Qaza.lnk");
                    string targetExe = Path.Combine(targetDir, "Pic_Qaza.exe");
                    CreateShortcut(shortcutFile, targetExe, targetDir, "سیستەمی تۆمارکردنی وێنەی پسووڵە - Pic_Qaza");
                }

                // Create Start Menu Shortcut
                if (param.CreateStartShortcut)
                {
                    installWorker.ReportProgress(96, "دروستکردنی شۆرتکەت لەناو Start Menu...");
                    string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string shortcutFile = Path.Combine(startMenu, "Pic_Qaza.lnk");
                    string targetExe = Path.Combine(targetDir, "Pic_Qaza.exe");
                    CreateShortcut(shortcutFile, targetExe, targetDir, "سیستەمی تۆمارکردنی وێنەی پسووڵە - Pic_Qaza");
                }

                installWorker.ReportProgress(100, "تەواوبوو!");
            }
            catch (Exception ex)
            {
                installError = ex.Message;
            }
        }

        private void InstallWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(installError))
            {
                MessageBox.Show("هەڵە لە دامەزراندن:\n" + installError, "هەڵە", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowStep(2);
            }
            else
            {
                ShowStep(4);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetExe, string workingDir, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                    Type scType = shortcut.GetType();
                    scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetExe });
                    scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDir });
                    scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                    scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                    return;
                }
            }
            catch { }

            try
            {
                string ps = string.Format(
                    "$s = (New-Object -COM WScript.Shell).CreateShortcut('{0}'); $s.TargetPath = '{1}'; $s.WorkingDirectory = '{2}'; $s.Description = '{3}'; $s.Save();",
                    shortcutPath.Replace("'", "''"), targetExe.Replace("'", "''"), workingDir.Replace("'", "''"), description.Replace("'", "''")
                );
                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"" + ps + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                }
            }
            catch { }
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

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWizardForm());
        }
    }
}
