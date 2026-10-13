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

            if (!File.Exists(serverScript))
            {
                MessageBox.Show("فایلی server.js نەدۆزرایەوە لە:\n" + serverScript, "هەڵە", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if server is already running on port 4500
            bool alreadyRunning = IsPortOpen("127.0.0.1", 4500);

            if (!alreadyRunning)
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
                    MessageBox.Show("هەڵە لە دەستپێکردنی سێرڤەری Node.js:\n" + ex.Message, "هەڵە", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Find browser
            string edge1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe");
            string edge2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe");
            string browserPath = null;

            if (File.Exists(edge1)) browserPath = edge1;
            else if (File.Exists(edge2)) browserPath = edge2;

            try
            {
                if (browserPath != null)
                {
                    ProcessStartInfo appPsi = new ProcessStartInfo();
                    appPsi.FileName = browserPath;
                    appPsi.Arguments = "--app=\"http://localhost:4500\" --window-size=1260,840";
                    appPsi.UseShellExecute = true;

                    Process.Start(appPsi);
                }
                else
                {
                    Process.Start("http://localhost:4500");
                }
            }
            catch
            {
                Process.Start("http://localhost:4500");
            }
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
