using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
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
                    _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                return _logDirectory;
            }
        }

        private static LoggingConfiguration _configuration;

        public static void Initialize(string loggerName)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

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
                    var fileTarget = ((AsyncTargetWrapper)target).WrappedTarget as FileTarget;

                    if (fileTarget == null)
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
