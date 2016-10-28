using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;

namespace AlwaysOnTop
{

    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint windowStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        int MOD_ALT = 0x1;
        int MOD_CONTROL = 0x2;
        int MOD_SHIFT = 0x4;
        int WM_HOTKEY = 0x312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        ArrayList alwayOnTopProcess;

        public Form1()
        {
            InitializeComponent();
        }

        void setWindowAlwaysOnTop(IntPtr MainWindowHandle)
        {
            for (int i = 1; i <= 3; i++)
            {
                //1u means normal : https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
                ShowWindow(MainWindowHandle, 1u);
                //-1 means HWND_TOPMOST : https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx
                //3u means 1u|2u = SWP_NOSIZE|SWP_NOMOVE : https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx
                SetWindowPos(MainWindowHandle, new IntPtr(-1), 0, 0, 0, 0, 3u);
            }
        }

        void disableWindowAlwaysOnTop(IntPtr MainWindowHandle)
        {
            for (int i = 1; i <= 3; i++)
            {
                //-2 means HWND_NOTOPMOST : https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx
                //3u means 1u|2u = SWP_NOSIZE|SWP_NOMOVE : https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx
                SetWindowPos(MainWindowHandle, new IntPtr(-2), 0, 0, 0, 0, 3u);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            alwayOnTopProcess = new ArrayList();
            RegisterHotKey(this.Handle, 131313, MOD_ALT, (int)Keys.Home);
            this.timer1.Start();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == 131313)
                {
                    UnregisterHotKey(this.Handle, 131313);
                    Process[] processList = Process.GetProcesses();
                    IntPtr handle = GetForegroundWindow();
                    foreach (Process process in processList)
                    {
                        if (process.MainWindowHandle == handle)
                        {
                            if (!processAlreadyExist(process))
                            {
                                alwayOnTopProcess.Add(process);
                                setWindowAlwaysOnTop(process.MainWindowHandle);
                            }
                            else
                            {
                                removeExistingProcess(process);
                                disableWindowAlwaysOnTop(process.MainWindowHandle);
                            }
                            break;
                        }
                    }
                    RegisterHotKey(this.Handle, 131313, MOD_ALT, (int)Keys.Home);
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            this.timer1.Stop();
        }

        private void ExitItme_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        public bool processAlreadyExist(Process other)
        {
            foreach (Process p in alwayOnTopProcess)
            {
                if (p.MainWindowHandle == other.MainWindowHandle && p.ProcessName == other.ProcessName && p.MainWindowTitle == other.MainWindowTitle)
                    return true;
            }
            return false;
        }

        public void removeExistingProcess(Process other)
        {
            Process processToDelete = null;
            foreach (Process p in alwayOnTopProcess)
            {
                if (p.MainWindowHandle == other.MainWindowHandle && p.ProcessName == other.ProcessName && p.MainWindowTitle == other.MainWindowTitle)
                {
                    processToDelete = p;
                    break;
                }
            }
            if (processToDelete != null)
                alwayOnTopProcess.Remove(processToDelete);
        }

        private void Item_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemWithData item = (ToolStripMenuItemWithData)sender;
            if (processAlreadyExist(item.data))
            {
                removeExistingProcess(item.data);
                item.Font = new Font(item.Font, FontStyle.Regular);
                disableWindowAlwaysOnTop(item.data.MainWindowHandle);
            }
            else
            {
                alwayOnTopProcess.Add(item.data);
                item.Font = new Font(item.Font, FontStyle.Bold);
                setWindowAlwaysOnTop(item.data.MainWindowHandle);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Process p in alwayOnTopProcess)
            {
                disableWindowAlwaysOnTop(p.MainWindowHandle);
            }
            UnregisterHotKey(this.Handle, 131313);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Clean process that the user terminated
            ArrayList deleted = new ArrayList();
            foreach (Process p in alwayOnTopProcess)
            {
                if (p.HasExited)
                    deleted.Add(p);
            }
            foreach (Process p in deleted)
            {
                alwayOnTopProcess.Remove(p);
            }

            //Generating new process list
            this.contextMenuStrip1.Items.Clear();
            Process[] processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    ToolStripMenuItemWithData item = new ToolStripMenuItemWithData();
                    item.Text = process.MainWindowTitle;
                    item.Click += Item_Click;
                    if (processAlreadyExist(process))
                        item.Font = new Font(item.Font, FontStyle.Bold);
                    item.data = process;
                    this.contextMenuStrip1.Items.Add(item);
                }
            }
            ToolStripSeparator seprator = new ToolStripSeparator();
            this.contextMenuStrip1.Items.Add(seprator);
            ToolStripMenuItem aboutItem = new ToolStripMenuItem();
            aboutItem.Text = "About";
            aboutItem.Click += AboutItem_Click;
            this.contextMenuStrip1.Items.Add(aboutItem);
            ToolStripMenuItem exitItme = new ToolStripMenuItem();
            exitItme.Text = "Exit";
            exitItme.Click += ExitItme_Click;
            this.contextMenuStrip1.Items.Add(exitItme);
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Created by Ofek Bashan", "AlwaysOnTop V1.0", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void contextMenuStrip1_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            this.timer1.Start();
        }
    }
}
