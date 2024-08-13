using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FileCopyService
{
    public partial class Service1 : ServiceBase
    {
        private FileSystemWatcher watcher;
        private string sourceDir = @"C:\Users\Nya\Downloads";
        private string targetDir = @"C:\Users\Nya\Documents";
        private string logSource = "C:\\Projects\\FileCopyService\\FileCopyService\\logs";
        private string logName = "FileCopyServiceLog";

        public Service1()
        {
            // Set up Event Log if it doesn't already exist
            if (!EventLog.SourceExists(logSource))
            {
                EventLog.CreateEventSource(logSource, logName);
            }

            EventLog.Source = logSource;
            EventLog.Log = logName;

            // Log service start
            EventLog.WriteEntry("FileCopyService initialized.");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Set up FileSystemWatcher
                watcher = new FileSystemWatcher(sourceDir)
                {
                    Filter = "*.*", // Watch all files; change to "*.txt" or other types as needed
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                // Hook up event handlers
                watcher.Created += OnFileCreated;

                // Log service start
                EventLog.WriteEntry("FileCopyService started successfully.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error during service start: {ex.Message}", EventLogEntryType.Error);
                Stop(); // Stop service if something goes wrong during start
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                // Log service stop
                EventLog.WriteEntry("FileCopyService stopped successfully.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error during service stop: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Delay to ensure file is fully written before copying
                System.Threading.Thread.Sleep(500);

                string targetPath = Path.Combine(targetDir, Path.GetFileName(e.FullPath));

                // Check if the file exists before attempting to copy
                if (File.Exists(e.FullPath))
                {
                    File.Copy(e.FullPath, targetPath);

                    // Log success
                    EventLog.WriteEntry($"File {e.FullPath} copied to {targetPath}.", EventLogEntryType.Information);
                }
                else
                {
                    // Log warning if file doesn't exist
                    EventLog.WriteEntry($"File {e.FullPath} does not exist or could not be found.", EventLogEntryType.Warning);
                }
            }
            catch (IOException ioEx)
            {
                // Handle IO exceptions (e.g., file being used by another process)
                EventLog.WriteEntry($"IO Error copying file {e.FullPath}: {ioEx.Message}", EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                EventLog.WriteEntry($"Error copying file {e.FullPath}: {ex.Message}", EventLogEntryType.Error);
            }
        }
    }
}
