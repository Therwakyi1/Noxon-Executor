using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NoxonExecutor
{
    public partial class MainForm : Form
    {
        // Import the DLL injector function
        [DllImport("Injector.dll")]
        private static extern bool Inject(int processId, string dllPath);
        
        // UI Elements
        private SyntaxRichTextBox scriptBox;
        private Button btnExecute, btnClear, btnOpen, btnInject, btnConsole;
        private ContextMenuStrip openMenuStrip;
        
        // Syntax highlighting colors
        private readonly Color backgroundColor = Color.FromArgb(20, 20, 20);
        private readonly Color textColor = Color.White;
        private readonly Color keywordColor = Color.FromArgb(255, 128, 0); // Orange
        private readonly Color stringColor = Color.FromArgb(255, 255, 0);   // Yellow
        private readonly Color commentColor = Color.FromArgb(0, 255, 0);    // Green
        private readonly Color functionColor = Color.FromArgb(255, 0, 0);   // Red
        private readonly Color numberColor = Color.FromArgb(200, 200, 255); // Light purple/white
        
        // Lua keywords for syntax highlighting
        private readonly string[] luaKeywords = {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", 
            "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", 
            "true", "until", "while"
        };
        
        // Lua functions for syntax highlighting
        private readonly string[] luaFunctions = {
            "print", "require", "loadstring", "load", "assert", "error", "pcall", 
            "xpcall", "getfenv", "setfenv", "rawget", "rawset", "rawequal", 
            "getmetatable", "setmetatable", "next", "pairs", "ipairs", "select", 
            "type", "tonumber", "tostring", "unpack", "table", "string", "math", 
            "coroutine", "os", "io", "debug"
        };
        
        public MainForm()
        {
            InitializeComponent();
            this.Text = "Noxon Executor v1.0";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Size = new Size(700, 500);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
        }
        
        private void InitializeComponent()
        {
            // Script Box with Syntax Highlighting
            scriptBox = new SyntaxRichTextBox();
            scriptBox.Location = new Point(12, 40);
            scriptBox.Size = new Size(660, 380);
            scriptBox.BackColor = backgroundColor;
            scriptBox.ForeColor = textColor;
            scriptBox.Font = new Font("Consolas", 10);
            scriptBox.TextChanged += ScriptBox_TextChanged;
            this.Controls.Add(scriptBox);
            
            // Execute Button
            btnExecute = new Button();
            btnExecute.Text = "Execute";
            btnExecute.Location = new Point(12, 5);
            btnExecute.Size = new Size(75, 30);
            btnExecute.BackColor = Color.Red;
            btnExecute.ForeColor = Color.White;
            btnExecute.FlatStyle = FlatStyle.Flat;
            btnExecute.Click += BtnExecute_Click;
            this.Controls.Add(btnExecute);
            
            // Clear Button
            btnClear = new Button();
            btnClear.Text = "Clear";
            btnClear.Location = new Point(93, 5);
            btnClear.Size = new Size(75, 30);
            btnClear.BackColor = Color.FromArgb(50, 50, 50);
            btnClear.ForeColor = Color.White;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);
            
            // Open Button
            btnOpen = new Button();
            btnOpen.Text = "Open";
            btnOpen.Location = new Point(174, 5);
            btnOpen.Size = new Size(75, 30);
            btnOpen.BackColor = Color.FromArgb(50, 50, 50);
            btnOpen.ForeColor = Color.White;
            btnOpen.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnOpen);
            
            // Inject Button
            btnInject = new Button();
            btnInject.Text = "Inject";
            btnInject.Location = new Point(255, 5);
            btnInject.Size = new Size(75, 30);
            btnInject.BackColor = Color.Red;
            btnInject.ForeColor = Color.White;
            btnInject.FlatStyle = FlatStyle.Flat;
            btnInject.Click += BtnInject_Click;
            this.Controls.Add(btnInject);
            
            // Console Button
            btnConsole = new Button();
            btnConsole.Text = "Console";
            btnConsole.Location = new Point(336, 5);
            btnConsole.Size = new Size(75, 30);
            btnConsole.BackColor = Color.FromArgb(50, 50, 50);
            btnConsole.ForeColor = Color.White;
            btnConsole.FlatStyle = FlatStyle.Flat;
            btnConsole.Click += BtnConsole_Click;
            this.Controls.Add(btnConsole);
            
            // Open Menu
            openMenuStrip = new ContextMenuStrip();
            var localItem = openMenuStrip.Items.Add("Local File");
            var githubItem = openMenuStrip.Items.Add("From GitHub");
            localItem.Click += LocalFileToolStripMenuItem_Click;
            githubItem.Click += GitHubToolStripMenuItem_Click;
            btnOpen.Click += (s, e) => openMenuStrip.Show(btnOpen, new Point(0, btnOpen.Height));
        }
        
        // Syntax highlighting logic
        private void ScriptBox_TextChanged(object sender, EventArgs e)
        {
            scriptBox.HighlightSyntax(luaKeywords, luaFunctions, 
                keywordColor, stringColor, commentColor, functionColor, numberColor);
        }
        
        // Custom RichTextBox with syntax highlighting
        private class SyntaxRichTextBox : RichTextBox
        {
            public void HighlightSyntax(string[] keywords, string[] functions, 
                Color keywordColor, Color stringColor, Color commentColor, 
                Color functionColor, Color numberColor)
            {
                // Save current position and selection
                int currentPosition = this.SelectionStart;
                int currentLength = this.SelectionLength;
                Color currentColor = this.SelectionColor;
                
                // Prevent flickering
                this.SuspendLayout();
                
                // Reset to default color
                this.SelectAll();
                this.SelectionColor = Color.White;
                this.DeselectAll();
                
                // Highlight strings (between quotes)
                HighlightPattern(@"(""|')(\\\1|.)*?\1", stringColor);
                
                // Highlight comments (-- to end of line)
                HighlightPattern(@"--.*$", commentColor);
                
                // Highlight multi-line comments (--[[ ... ]])
                HighlightPattern(@"--\[\[[\s\S]*?\]\]", commentColor);
                
                // Highlight numbers
                HighlightPattern(@"\b\d+\.?\d*\b", numberColor);
                
                // Highlight keywords
                foreach (string keyword in keywords)
                {
                    HighlightPattern(@"\b" + keyword + @"\b", keywordColor);
                }
                
                // Highlight functions
                foreach (string func in functions)
                {
                    HighlightPattern(@"\b" + func + @"\b", functionColor);
                }
                
                // Restore position and selection
                this.SelectionStart = currentPosition;
                this.SelectionLength = currentLength;
                this.SelectionColor = currentColor;
                this.ScrollToCaret();
                
                this.ResumeLayout();
            }
            
            private void HighlightPattern(string pattern, Color color)
            {
                MatchCollection matches = Regex.Matches(this.Text, pattern, RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    this.Select(match.Index, match.Length);
                    this.SelectionColor = color;
                }
            }
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
            // EDITED: Now supports .lua, .luau, and .txt files
            dialog.Filter = "Lua Files|*.lua|Luau Files|*.luau|Text Files|*.txt|All Files|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    scriptBox.Text = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Couldn't read that file, dickhead: {ex.Message}");
                }
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
        
        // Helper method to load file with extension detection
        private void LoadFileWithDetection(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                scriptBox.Text = content;
                
                // Auto-detect file type for syntax (though we use Lua highlighting for all)
                string extension = Path.GetExtension(filePath).ToLower();
                // You could add different highlighting schemes based on file type here
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}");
            }
        }
    }
}
