using System;
using System.Windows.Forms;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NoxonExecutor
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [HandleProcessCorruptedStateExceptions] // This allows catching nasty exceptions like AccessViolation
        static void Main()
        {
            // Fuck default exception handling - we want to catch everything
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;

            // Enable visual styles and shit
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if required DLLs exist
            if (!CheckDependencies())
            {
                MessageBox.Show("Missing required DLLs. Make sure Injector.dll and API.dll are in the same directory as this executable.", 
                    "Noxon Executor - Dependency Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return;
            }

            // Run the main form
            Application.Run(new MainForm());
        }

        /// <summary>
        /// Checks if all required native DLLs are present
        /// </summary>
        private static bool CheckDependencies()
        {
            try
            {
                // Check for Injector.dll
                if (!System.IO.File.Exists("Injector.dll"))
                {
                    Console.WriteLine("Injector.dll not found!");
                    return false;
                }

                // Check for API.dll  
                if (!System.IO.File.Exists("API.dll"))
                {
                    Console.WriteLine("API.dll not found!");
                    return false;
                }

                // Optional: Check if DLLs are valid PE files
                if (!IsValidPeFile("Injector.dll") || !IsValidPeFile("API.dll"))
                {
                    Console.WriteLine("One or more DLLs are corrupted or invalid!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dependency check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Basic check if a file is a valid PE (Portable Executable) file
        /// </summary>
        private static bool IsValidPeFile(string filePath)
        {
            try
            {
                // Read first two bytes to check for "MZ" signature
                byte[] buffer = new byte[2];
                using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    fs.Read(buffer, 0, 2);
                }
                return buffer[0] == 0x4D && buffer[1] == 0x5A; // "MZ" signature
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Global exception handler for unhandled exceptions
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string errorMsg = $"Fatal error, you fucked up:\n{(ex != null ? ex.ToString() : "Unknown error")}";

            MessageBox.Show(errorMsg, "Noxon Executor - Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            // Log to file for debugging
            try
            {
                System.IO.File.WriteAllText("crash_log.txt", $"{DateTime.Now}: {errorMsg}");
            }
            catch { /* We're already crashing, who gives a fuck */ }

            Environment.Exit(666);
        }

        /// <summary>
        /// UI thread exception handler
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string errorMsg = $"UI thread error:\n{e.Exception}";

            MessageBox.Show(errorMsg, "Noxon Executor - UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            try
            {
                System.IO.File.AppendAllText("ui_errors.txt", $"{DateTime.Now}: {errorMsg}\n");
            }
            catch { /* Whatever */ }
        }

        /// <summary>
        /// Checks if another instance is already running
        /// </summary>
        private static bool IsAlreadyRunning()
        {
            // Use a mutex to prevent multiple instances
            bool createdNew;
            var mutex = new Mutex(true, "NoxonExecutorMutex", out createdNew);
            return !createdNew;
        }
    }
}
