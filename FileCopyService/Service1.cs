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
using Newtonsoft.Json;

//cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
//InstallUtil.exe C:\Projects\FileCopyService\FileCopyService\bin\Debug\FileCopyService.exe
//InstallUtil.exe -u C:\Projects\FileCopyService\FileCopyService\bin\Debug\FileCopyService.exe

namespace FileCopyService
{
    public partial class Service1 : ServiceBase
    {
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private string configFilePath = @"C:\Projects\FileCopyService\FileCopyService\config.json";
        private string logSource = "FileCopyServiceSource";
        private string logName = "FileCopyServiceLog";

        public Service1()
        {
            try
            {
                // Set up Event Log if it doesn't already exist
                if (!EventLog.SourceExists(logSource))
                {
                    EventLog.CreateEventSource(logSource, logName);
                }

                EventLog.Source = logSource;
                EventLog.Log = logName;

                // Log service initialization
                EventLog.WriteEntry("FileCopyService initialized.");
            }
            catch (Exception ex)
            {
                // Log initialization error
                File.AppendAllText(@"C:\Projects\FileCopyService\FileCopyService\logs\service_init_error.txt", $"{DateTime.Now}: Error during service initialization - {ex.Message}\n");
                throw; // Re-throw the exception to prevent service from starting
            }

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                var directoryPairs = LoadConfiguration(configFilePath);

                foreach (var pair in directoryPairs)
                {
                    var watcher = new FileSystemWatcher(pair.SourceDir)
                    {
                        Filter = "*.*", // Watch all files; change to "*.txt" or other types as needed
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };

                    // Hook up event handlers with dynamic targetDir
                    watcher.Created += (sender, e) => OnFileCreated(sender, e, pair.TargetDir);
                    watchers.Add(watcher);
                }

                // Log service start
                EventLog.WriteEntry("FileCopyService started successfully.");
            }
            catch (Exception ex)
            {
                // Log error during service start
                EventLog.WriteEntry($"Error during service start: {ex.Message}", EventLogEntryType.Error);
                Stop(); // Stop service if something goes wrong during start
            }
        }

        protected override void OnStop()
        {
            try
            {
                foreach (var watcher in watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                // Log service stop
                EventLog.WriteEntry("FileCopyService stopped successfully.");
            }
            catch (Exception ex)
            {
                // Log error during service stop
                EventLog.WriteEntry($"Error during service stop: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e, string targetDir)
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

        private List<DirectoryPair> LoadConfiguration(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("Configuration file not found.");

                string json = File.ReadAllText(path);
                var directoryPairs = JsonConvert.DeserializeObject<List<DirectoryPair>>(json);

                return directoryPairs;
            }
            catch (Exception ex)
            {
                // Log configuration loading error
                File.AppendAllText(@"C:\Projects\FileCopyService\FileCopyService\logs\config_load_error.txt", $"{DateTime.Now}: Error loading configuration - {ex.Message}\n");
                throw;
            }
        }
    }

    public class DirectoryPair
    {
        public string SourceDir { get; set; }
        public string TargetDir { get; set; }
    }
}
