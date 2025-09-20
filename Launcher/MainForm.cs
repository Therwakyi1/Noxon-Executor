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
        [DllImport("Injector.dll")]
        private static extern bool Inject(int processId, string dllPath);

        private SyntaxRichTextBox scriptBox;
        private Button btnExecute, btnClear, btnOpen, btnInject, btnConsole;
        private ContextMenuStrip openMenuStrip;
        private Panel topPanel;

        private readonly Color backgroundColor = Color.FromArgb(20, 20, 20);
        private readonly Color textColor = Color.White;
        private readonly Color keywordColor = Color.FromArgb(255, 128, 0);
        private readonly Color stringColor = Color.Yellow;
        private readonly Color commentColor = Color.Lime;
        private readonly Color functionColor = Color.Red;
        private readonly Color numberColor = Color.FromArgb(200, 200, 255);

        private readonly string[] luaKeywords =
        {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function",
            "if", "in", "local", "nil", "not", "or", "repeat", "return", "then",
            "true", "until", "while"
        };

        private readonly string[] luaFunctions =
        {
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
            this.Size = new Size(720, 520);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            try
            {
                this.Icon = new Icon("noxonlogo.ico"); // requires PNG converted to ICO
            }
            catch { }
        }

        private void InitializeComponent()
        {
            // --- Top Panel for Buttons ---
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 40;
            topPanel.BackColor = Color.FromArgb(40, 40, 40);
            this.Controls.Add(topPanel);

            // Execute
            btnExecute = CreateStyledButton("Execute", Color.Red, new Point(10, 5));
            btnExecute.Click += BtnExecute_Click;
            topPanel.Controls.Add(btnExecute);

            // Clear
            btnClear = CreateStyledButton("Clear", Color.FromArgb(64, 64, 64), new Point(95, 5));
            btnClear.Click += BtnClear_Click;
            topPanel.Controls.Add(btnClear);

            // Open
            btnOpen = CreateStyledButton("Open", Color.FromArgb(64, 64, 64), new Point(180, 5));
            topPanel.Controls.Add(btnOpen);

            // Inject
            btnInject = CreateStyledButton("Inject", Color.Red, new Point(265, 5));
            btnInject.Click += BtnInject_Click;
            topPanel.Controls.Add(btnInject);

            // Console
            btnConsole = CreateStyledButton("Console", Color.FromArgb(64, 64, 64), new Point(350, 5));
            btnConsole.Click += BtnConsole_Click;
            topPanel.Controls.Add(btnConsole);

            // --- Script Editor ---
            scriptBox = new SyntaxRichTextBox();
            scriptBox.Dock = DockStyle.Fill;
            scriptBox.BackColor = backgroundColor;
            scriptBox.ForeColor = textColor;
            scriptBox.Font = new Font("Consolas", 10);
            scriptBox.BorderStyle = BorderStyle.None;
            scriptBox.TextChanged += ScriptBox_TextChanged;
            this.Controls.Add(scriptBox);

            // --- Context Menu ---
            openMenuStrip = new ContextMenuStrip();
            var localItem = openMenuStrip.Items.Add("Local File");
            var githubItem = openMenuStrip.Items.Add("From GitHub");
            localItem.Click += LocalFileToolStripMenuItem_Click;
            githubItem.Click += GitHubToolStripMenuItem_Click;
            btnOpen.Click += (s, e) => openMenuStrip.Show(btnOpen, new Point(0, btnOpen.Height));
        }

        private Button CreateStyledButton(string text, Color backColor, Point location)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(75, 30),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
        }

        private void ScriptBox_TextChanged(object sender, EventArgs e)
        {
            scriptBox.HighlightSyntax(luaKeywords, luaFunctions,
                keywordColor, stringColor, commentColor, functionColor, numberColor);
        }

        private class SyntaxRichTextBox : RichTextBox
        {
            public void HighlightSyntax(string[] keywords, string[] functions,
                Color keywordColor, Color stringColor, Color commentColor,
                Color functionColor, Color numberColor)
            {
                int currentPosition = this.SelectionStart;
                int currentLength = this.SelectionLength;
                Color currentColor = this.SelectionColor;

                this.SuspendLayout();

                this.SelectAll();
                this.SelectionColor = Color.White;
                this.DeselectAll();

                HighlightPattern(@"(""|')(\\\1|.)*?\1", stringColor);
                HighlightPattern(@"--.*$", commentColor);
                HighlightPattern(@"--\[\[[\s\S]*?\]\]", commentColor);
                HighlightPattern(@"\b\d+\.?\d*\b", numberColor);

                foreach (string keyword in keywords)
                    HighlightPattern(@"\b" + keyword + @"\b", keywordColor);

                foreach (string func in functions)
                    HighlightPattern(@"\b" + func + @"\b", functionColor);

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
                var processes = Process.GetProcessesByName("RobloxPlayerBeta");
                if (processes.Length == 0)
                {
                    MessageBox.Show("Open Roblox first.");
                    return;
                }

                string dllPath = Path.Combine(Application.StartupPath, "API.dll");

                if (!File.Exists(dllPath))
                {
                    MessageBox.Show($"API.dll not found at: {dllPath}");
                    return;
                }

                bool success = Inject(processes[0].Id, dllPath);
                MessageBox.Show(success ? "Injected successfully!" : "Injection failed.");
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
                MessageBox.Show("Open Roblox first.");
            }
        }

        private void LocalFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Lua Files|*.lua|Luau Files|*.luau|Text Files|*.txt|All Files|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    scriptBox.Text = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Couldn't read file: {ex.Message}");
                }
            }
        }

        private void GitHubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("GitHub integration would go here.");
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
