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

        private readonly Color backgroundColor = Color.FromArgb(25, 25, 25);
        private readonly Color textColor = Color.White;
        private readonly Color keywordColor = Color.FromArgb(255, 128, 0);
        private readonly Color stringColor = Color.Yellow;
        private readonly Color commentColor = Color.Lime;
        private readonly Color functionColor = Color.Red;
        private readonly Color numberColor = Color.FromArgb(200, 200, 255);

        private readonly string[] luaKeywords = {
            "and","break","do","else","elseif","end","false","for","function",
            "if","in","local","nil","not","or","repeat","return","then",
            "true","until","while"
        };

        private readonly string[] luaFunctions = {
            "print","require","loadstring","load","assert","error","pcall",
            "xpcall","getfenv","setfenv","rawget","rawset","rawequal",
            "getmetatable","setmetatable","next","pairs","ipairs","select",
            "type","tonumber","tostring","unpack","table","string","math",
            "coroutine","os","io","debug"
        };

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Noxon Executor v1.0";
            this.Icon = new Icon(Path.Combine(Application.StartupPath, "logo.ico"));
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Size = new Size(750, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            // Script Box
            scriptBox = new SyntaxRichTextBox();
            scriptBox.Location = new Point(12, 50);
            scriptBox.Size = new Size(710, 400);
            scriptBox.BackColor = backgroundColor;
            scriptBox.ForeColor = textColor;
            scriptBox.Font = new Font("Consolas", 11, FontStyle.Regular);
            scriptBox.BorderStyle = BorderStyle.FixedSingle;
            scriptBox.TextChanged += ScriptBox_TextChanged;
            this.Controls.Add(scriptBox);

            // Button style helper
            Button CreateButton(string text, int x, Color color, EventHandler onClick)
            {
                var btn = new Button();
                btn.Text = text;
                btn.Location = new Point(x, 10);
                btn.Size = new Size(90, 30);
                btn.BackColor = color;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                if (onClick != null) btn.Click += onClick;
                this.Controls.Add(btn);
                return btn;
            }

            btnExecute = CreateButton("Execute", 12, Color.FromArgb(200, 50, 50), BtnExecute_Click);
            btnClear   = CreateButton("Clear", 114, Color.FromArgb(60, 60, 60), BtnClear_Click);
            btnOpen    = CreateButton("Open", 216, Color.FromArgb(60, 60, 60), null);
            btnInject  = CreateButton("Inject", 318, Color.FromArgb(200, 50, 50), BtnInject_Click);
            btnConsole = CreateButton("Console", 420, Color.FromArgb(60, 60, 60), BtnConsole_Click);

            // Context menu for Open
            openMenuStrip = new ContextMenuStrip();
            var localItem = openMenuStrip.Items.Add("Local File");
            var githubItem = openMenuStrip.Items.Add("From GitHub");
            localItem.Click += LocalFileToolStripMenuItem_Click;
            githubItem.Click += GitHubToolStripMenuItem_Click;
            btnOpen.Click += (s, e) => openMenuStrip.Show(btnOpen, new Point(0, btnOpen.Height));
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
                    MessageBox.Show("Open Roblox first, dumbass.");
                    return;
                }

                string dllPath = Path.Combine(Application.StartupPath, "API.dll");
                if (!File.Exists(dllPath))
                {
                    MessageBox.Show($"API.dll not found at: {dllPath}");
                    return;
                }

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
            dialog.Filter = "Lua Files|*.lua|Luau Files|*.luau|Text Files|*.txt|All Files|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    scriptBox.Text = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Couldn't read that file: {ex.Message}");
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
