using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Timers;
using Experior.TMS.FileLocationUpdateApp.Entities;
using NLog;

namespace Experior.TMS.FileLocationUpdateApp
{
    internal class Program
    {
        private static AppConfiguration _appConfig;
        private static Logger _logger;
        private static bool _isCheckInProgress = false;
        private static readonly object SyncObject = new Object();

        public static bool IsCheckInProgress
        {
            get
            {
                lock (SyncObject)
                {
                    return _isCheckInProgress;
                }
            }
            set
            {
                lock (SyncObject)
                {
                    _isCheckInProgress = value;
                }
            }
        }

        public static void Main(string[] args)
        {
            _appConfig = ParseConfiguration();
            _logger = LogManager.GetCurrentClassLogger();
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            var timer = new Timer(_appConfig.Timeout);
            timer.Elapsed += TimerElapsed;
            timer.Start();
            PerformCheckForNewRecords();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            timer.Stop();
            timer.Dispose();
        }


        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e.ExceptionObject as Exception, "Unhandled exception occured: ");
            LogManager.Flush();
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (IsCheckInProgress) return;
                IsCheckInProgress = true;
                PerformCheckForNewRecords();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while checking for new records");
                LogManager.Flush();
            }
            finally
            {
                _isCheckInProgress = false;
            }
        }

        private static void PerformCheckForNewRecords()
        {
            if (_appConfig.LastProcessedRecordId == 0)
            {
                _logger.Warn("LastProcessedRecordId = 0. Please specify correct Id in app.config");
                return;
            }

            _logger.Info("Check for newly added records started...");

            int foundRecordsCount = 0;
            int updatedRecordsCount = 0;

            var dataContext = ApplicationDataContext.Create(_appConfig.DbConnectionString);

            var entitiesToProcess = dataContext.DocumentFiles.Where(x =>
                x.TruckmateTable.Equals("TLORDER") && x.Id > _appConfig.LastProcessedRecordId);

            foreach (var documentFile in entitiesToProcess.OrderBy(x => x.Id))
            {
                foundRecordsCount++;
                if (TryMoveFileAndUpdateLocation(documentFile)) updatedRecordsCount++;
                _appConfig.LastProcessedRecordId = documentFile.Id;
            }

            try
            {
                dataContext.SaveChanges();
                UpdatePositionMarkerInConfig();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while saving changes to database.");
            }
            finally
            {
                dataContext.Dispose();
            }
            _logger.Info("Check finished. Found records count: {0}. Succesfully updated: {1}", foundRecordsCount,
                updatedRecordsCount);
        }

        private static bool TryMoveFileAndUpdateLocation(DocumentFile documentFile)
        {
            if (string.IsNullOrEmpty(documentFile.FilePath))
            {
                _logger.Warn("Empty file path: Item ID {0}", documentFile.Id);
                return false;
            }

            try
            {
                _logger.Info("Prepare to move: Item ID {0}, source path: {1}", documentFile.Id,
                    documentFile.FilePath);

                var outputFileName = String.Join("_", documentFile.TruckmateValue, documentFile.FileDesc,
                                         documentFile.RowTimestamp.ToString("yyyyMMdd-HHmm")) + ".pdf";


                string outputPath;
                bool result = TryMoveFile(documentFile, outputFileName, out outputPath);
                if (result)
                {
                    documentFile.FilePath = outputPath;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error moving file {0}, record ID: {1} ", documentFile.FilePath, documentFile.Id);
                return false;
            }
            return true;
        }

        private static void UpdatePositionMarkerInConfig()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            config.AppSettings.Settings["LastProcessedRecordId"].Value = _appConfig.LastProcessedRecordId.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        private static bool TryMoveFile(DocumentFile documentFile, string outputFileName, out string outputPath)
        {
            outputPath = string.Empty;

            if (!File.Exists(documentFile.FilePath))
            {
                _logger.Warn("File not found: {0}", documentFile.FilePath);
                return false;
            }
            var destinationPath = Path.Combine(_appConfig.OutputDirectory, documentFile.TruckmateValue, outputFileName);
            string destinationCopyPath = Path.Combine(_appConfig.OutputCopyDirectory, outputFileName);
            try
            {
                var directoryName = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                File.Move(documentFile.FilePath, destinationPath);
                outputPath = destinationPath;
                _logger.Info("File moved successfully from {0} to {1}", documentFile.FilePath, destinationPath);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Error moving file {0} to {1}", documentFile.FilePath, destinationPath);
                return false;
            }

            try
            {
                var directoryName = Path.GetDirectoryName(destinationCopyPath);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                File.Copy(destinationPath, destinationCopyPath);
                _logger.Info("File copied successfully from {0} to {1}", documentFile.FilePath, destinationPath);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Error copying file {0} to {1}", documentFile.FilePath, destinationPath);
                return false;
            }
            return true;
        }


        private static AppConfiguration ParseConfiguration()
        {
            int timeout;
            int lastProcessedRecordId;
            if (!int.TryParse(ConfigurationManager.AppSettings["Timeout"], out timeout))
                timeout = 5 * 60;
            if (!int.TryParse(ConfigurationManager.AppSettings["LastProcessedRecordId"], out lastProcessedRecordId))
                lastProcessedRecordId = 0;
            string outputDir = ConfigurationManager.AppSettings["OutputDirectory"];
            string outputCopyDir = ConfigurationManager.AppSettings["OutputCopyDirectory"];
            string connectionString = ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            return new AppConfiguration
            {
                Timeout = timeout * 1000,
                DbConnectionString = connectionString,
                OutputDirectory = outputDir,
                OutputCopyDirectory = outputCopyDir,
                LastProcessedRecordId = lastProcessedRecordId
            };
        }
    }


    struct AppConfiguration
    {
        public string DbConnectionString { get; set; }
        public int Timeout { get; set; }
        public string OutputDirectory { get; set; }
        public string OutputCopyDirectory { get; set; }
        public int LastProcessedRecordId { get; set; }
    }
}