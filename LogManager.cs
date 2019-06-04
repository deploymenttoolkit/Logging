using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.IO;
using System.Reflection;

namespace DeploymentToolkit.Logging
{
    public static class LogManager
    {
        private static string _logDirectory;
        public static string LogDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_logDirectory))
                    _logDirectory = GetLogDirectory();
                return _logDirectory;
            }
        }

        private static string GetLogDirectory()
        {
            var currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(currentDirectory))
                Directory.CreateDirectory(currentDirectory);

            if (IsDirectoryWriteable(currentDirectory))
                return currentDirectory;

            var programData = Environment.GetEnvironmentVariable("PROGRAMDATA", EnvironmentVariableTarget.Machine);
            if(!string.IsNullOrEmpty(programData))
            {
                try
                {
                    var deploymentToolkitPath = Path.Combine(programData, "DeploymentToolkit");
                    if (!Directory.Exists(deploymentToolkitPath))
                        Directory.CreateDirectory(deploymentToolkitPath);

                    var logPath = Path.Combine(deploymentToolkitPath, "Logs");
                    if (!Directory.Exists(logPath))
                        Directory.CreateDirectory(logPath);

                    if (IsDirectoryWriteable(logPath))
                        return logPath;
                }
                catch (Exception) { }
            }

            var tempDirectory = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User);
            if(!string.IsNullOrEmpty(tempDirectory))
            {
                if (IsDirectoryWriteable(tempDirectory))
                    return tempDirectory;
            }

            throw new Exception("Failed to get a valid Log directory");
        }

        private static bool IsDirectoryWriteable(string path)
        {
            try
            {
                using (var fileStream = File.Create(Path.Combine(path, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
                {
                    // Do nothing
                }

                return true;
            }
            catch(UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static LoggingConfiguration _configuration;

        public static void Initialize(string loggerName)
        {
            if(!File.Exists("log.config"))
            {
                //Create logging rules if not existing

                var assembly = Assembly.GetExecutingAssembly();
                var resource = assembly.GetManifestResourceStream("DeploymentToolkit.Logging.log.config");
                using (var file = new StreamReader(resource))
                {
                    File.WriteAllText("log.config", file.ReadToEnd());
                }
            }

            _configuration = new XmlLoggingConfiguration("log.config");

            foreach(var target in _configuration.AllTargets)
            {
                if(target is AsyncTargetWrapper)
                {
                    if (!(((AsyncTargetWrapper)target).WrappedTarget is FileTarget fileTarget))
                        continue;

                    fileTarget.FileName = Path.Combine(LogDirectory,
                        fileTarget.FileName
                            .ToString()
                            .Trim('\'')
                            .Replace("DeploymentToolkit-", $"DeploymentToolkit-{loggerName}-")
                    );
                }
            }

            NLog.LogManager.Configuration = _configuration;
            NLog.LogManager.ReconfigExistingLoggers();
            NLog.LogManager.EnableLogging();
        }
    }
}
