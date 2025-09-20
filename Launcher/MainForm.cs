using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NoxonExecutor
{
    public partial class MainForm : Form
    {
        // Import the DLL injector function
        [DllImport("Injector.dll")]
        private static extern bool Inject(int processId, string dllPath);
        
        // UI Elements
        private RichTextBox scriptBox;
        private Button btnExecute, btnClear, btnOpen, btnInject, btnConsole;
        private ContextMenuStrip openMenuStrip;
        
        public MainForm()
        {
            InitializeComponent();
            this.Text = "Noxon Executor v1.0";
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ForeColor = System.Drawing.Color.White;
            this.Size = new System.Drawing.Size(700, 500);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
        }
        
        private void InitializeComponent()
        {
            // Script Box
            scriptBox = new RichTextBox();
            scriptBox.Location = new System.Drawing.Point(12, 40);
            scriptBox.Size = new System.Drawing.Size(660, 380);
            scriptBox.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            scriptBox.ForeColor = System.Drawing.Color.White;
            scriptBox.Font = new System.Drawing.Font("Consolas", 10);
            this.Controls.Add(scriptBox);
            
            // Execute Button
            btnExecute = new Button();
            btnExecute.Text = "Execute";
            btnExecute.Location = new System.Drawing.Point(12, 5);
            btnExecute.Size = new System.Drawing.Size(75, 30);
            btnExecute.BackColor = System.Drawing.Color.Red;
            btnExecute.ForeColor = System.Drawing.Color.White;
            btnExecute.FlatStyle = FlatStyle.Flat;
            btnExecute.Click += BtnExecute_Click;
            this.Controls.Add(btnExecute);
            
            // Clear Button
            btnClear = new Button();
            btnClear.Text = "Clear";
            btnClear.Location = new System.Drawing.Point(93, 5);
            btnClear.Size = new System.Drawing.Size(75, 30);
            btnClear.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            btnClear.ForeColor = System.Drawing.Color.White;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);
            
            // Open Button
            btnOpen = new Button();
            btnOpen.Text = "Open";
            btnOpen.Location = new System.Drawing.Point(174, 5);
            btnOpen.Size = new System.Drawing.Size(75, 30);
            btnOpen.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            btnOpen.ForeColor = System.Drawing.Color.White;
            btnOpen.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnOpen);
            
            // Inject Button
            btnInject = new Button();
            btnInject.Text = "Inject";
            btnInject.Location = new System.Drawing.Point(255, 5);
            btnInject.Size = new System.Drawing.Size(75, 30);
            btnInject.BackColor = System.Drawing.Color.Red;
            btnInject.ForeColor = System.Drawing.Color.White;
            btnInject.FlatStyle = FlatStyle.Flat;
            btnInject.Click += BtnInject_Click;
            this.Controls.Add(btnInject);
            
            // Console Button
            btnConsole = new Button();
            btnConsole.Text = "Console";
            btnConsole.Location = new System.Drawing.Point(336, 5);
            btnConsole.Size = new System.Drawing.Size(75, 30);
            btnConsole.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            btnConsole.ForeColor = System.Drawing.Color.White;
            btnConsole.FlatStyle = FlatStyle.Flat;
            btnConsole.Click += BtnConsole_Click;
            this.Controls.Add(btnConsole);
            
            // Open Menu
            openMenuStrip = new ContextMenuStrip();
            var localItem = openMenuStrip.Items.Add("Local File");
            var githubItem = openMenuStrip.Items.Add("From GitHub");
            localItem.Click += LocalFileToolStripMenuItem_Click;
            githubItem.Click += GitHubToolStripMenuItem_Click;
            btnOpen.Click += (s, e) => openMenuStrip.Show(btnOpen, new System.Drawing.Point(0, btnOpen.Height));
        }
        
        private void BtnExecute_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Execution would happen here if we were injected.");
        }
        
        private void BtnClear_Click(object sender, EventArgs e)
        {
            scriptBox.Clear();
        }
        
        private void BtnInject_Click(object sender, EventArgs e)
        {
            try
            {
                // Find Roblox process
                var processes = Process.GetProcessesByName("RobloxPlayerBeta");
                if (processes.Length == 0)
                {
                    MessageBox.Show("Open Roblox first, dumbass.");
                    return;
                }
                
                // Get path to our API DLL
                string dllPath = Path.Combine(Application.StartupPath, "API.dll");
                
                if (!File.Exists(dllPath))
                {
                    MessageBox.Show($"API.dll not found at: {dllPath}");
                    return;
                }
                
                // Inject the DLL
                bool success = Inject(processes[0].Id, dllPath);
                MessageBox.Show(success ? "Injected successfully!" : "Injection failed, you fucked something up.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Injection error: {ex.Message}");
            }
        }
        
        private void BtnConsole_Click(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("RobloxPlayerBeta");
            if (processes.Length > 0)
            {
                SetForegroundWindow(processes[0].MainWindowHandle);
                SendKeys.SendWait("{F9}");
            }
            else
            {
                MessageBox.Show("Open Roblox first, you idiot.");
            }
        }
        
        private void LocalFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Lua Files|*.lua|Text Files|*.txt|All Files|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                scriptBox.Text = File.ReadAllText(dialog.FileName);
            }
        }
        
        private void GitHubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Placeholder for GitHub integration
            MessageBox.Show("GitHub integration would go here.");
        }
        
        // Windows API functions for bringing windows to foreground
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
