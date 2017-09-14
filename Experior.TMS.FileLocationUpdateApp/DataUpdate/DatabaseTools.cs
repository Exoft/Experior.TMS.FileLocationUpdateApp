using System;
using System.IO;
using System.Linq;
using Experior.TMS.FileLocationUpdateApp.Configuration;
using Experior.TMS.FileLocationUpdateApp.Entities;
using NLog;

namespace Experior.TMS.FileLocationUpdateApp.DataUpdate
{
    public class DatabaseTools
    {
        private readonly AppConfiguration _appConfig;
        private readonly Logger _logger;
        private readonly FileSystemTools _fileSystemTools;

        public DatabaseTools(AppConfiguration appConfig)
        {
            _appConfig = appConfig;
            _logger = LogManager.GetCurrentClassLogger();
            _fileSystemTools = new FileSystemTools();
        }
        
        public void Process()
        {
            using (var dataContext = ApplicationDataContext.Create(_appConfig.DbConnectionString))
            {
                var previouslyLockedRecords = GetAllLockedAndNotProcessedRecords(dataContext);
                var notProcessedPreviouslyRecords = GetAllNotProcessedSuccessfullyRecords(dataContext);
                var newlyAddedRecords = GetNewlyAddedRecords(dataContext);

//                _logger.Info("Starting processing previously locked records...");
//                var values = ProcessRecordsSet(dataContext, previouslyLockedRecords);
//                _logger.Info("Processing locked records finished. Records found: {0}; Records processed: {1}", values.Item1, values.Item2);
//                
//                _logger.Info("Strarting processing records, failed on previous run...");
//                values = ProcessRecordsSet(dataContext, notProcessedPreviouslyRecords);
//                _logger.Info("Processing records, failed on previous run finished. Records found: {0}; Records processed: {1}", values.Item1, values.Item2);
//                
//                _logger.Info("Starting processing new records...");
//                values = ProcessRecordsSet(dataContext, newlyAddedRecords);
//                _logger.Info("Processing new records finished. Records found: {0}; Records processed: {1}", values.Item1, values.Item2);
                
                _logger.Info("Starting processing previously locked records...");
                ProcessRecordsSet(dataContext, previouslyLockedRecords);
                _logger.Info("Processing locked records finished.");
                
                _logger.Info("Strarting processing records, failed on previous run...");
                ProcessRecordsSet(dataContext, notProcessedPreviouslyRecords);
                _logger.Info("Processing records, failed on previous run finished.");
                
                _logger.Info("Starting processing new records...");
                 ProcessRecordsSet(dataContext, newlyAddedRecords);
                _logger.Info("Processing new records finished.");
            }
            ConfigurationManager.Instance.SaveProcessedIds();
        }
        
        private string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
        
        
        private void ProcessRecordsSet(ApplicationDataContext dataContext, IQueryable<DocumentFile> recordSet)
        {
            int foundRecordsCount = 0;
            int processedRecordsCount = 0;
            foreach (var documentFile in recordSet.OrderBy(x => x.Id))
            {
                foundRecordsCount++;
                string sourcePath = documentFile.FilePath;

                var outputFileName = GetSafeFilename(String.Join("_", documentFile.TruckmateValue, documentFile.FileDesc,
                                                         documentFile.RowTimestamp.ToString("yyyyMMdd-HHmm")) + ".pdf");

                var destinationPath = Path.Combine(_appConfig.OutputDirectory, GetSafeFilename(documentFile.TruckmateValue), outputFileName);
                string destinationCopyPath = Path.Combine(_appConfig.OutputCopyDirectory, outputFileName);

                RecordMetadata recordMetadata = _appConfig.ProcessedIds.SingleOrDefault(x => x.Id == documentFile.Id);

                if (recordMetadata == null)
                {
                    recordMetadata = new RecordMetadata {Id = documentFile.Id};
                    _appConfig.ProcessedIds.Add(recordMetadata);
                }

                try
                {
                    documentFile.FilePath = destinationPath;
                    dataContext.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error while saving changes to database. Entity id: " + documentFile.Id);
                    documentFile.FilePath = sourcePath;
                    continue;
                }

                if (_fileSystemTools.TryMoveFile(sourcePath, destinationPath, destinationCopyPath))
                {
                    _appConfig.LastProcessedRecordId = documentFile.Id;
                    recordMetadata.Processed = true;
                    processedRecordsCount++;
                    ConfigurationManager.Instance.UpdatePositionMarkerInConfig();
                }
                else
                {
                    try
                    {
                        documentFile.FilePath = sourcePath;
                        dataContext.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error while saving changes to database.");
                    }
                }
            }
            //return (foundRecordsCount, processedRecordsCount);
        }

        private IQueryable<DocumentFile> GetAllNotProcessedSuccessfullyRecords(ApplicationDataContext dataContext)
        {
            var processedMetadata = _appConfig.ProcessedIds;
            var lastProcessedRecordId = _appConfig.LastProcessedRecordId;
            
            if (processedMetadata.Any())
            {
                var minId = processedMetadata.Min(x => x.Id);
                var notProcessed = processedMetadata.Where(x => !x.Processed).Select(x => x.Id).ToArray();

                return dataContext.DocumentFiles.Where(x =>
                    x.TruckmateTable.Equals("TLORDER") &&
                    x.Id >= minId &&
                    notProcessed.Contains(x.Id) &&
                    x.Id <= lastProcessedRecordId
                );
            }
            
            return Enumerable.Empty<DocumentFile>().AsQueryable();
        }


        private IQueryable<DocumentFile> GetAllLockedAndNotProcessedRecords(ApplicationDataContext dataContext)
        {
            var processedMetadata = _appConfig.ProcessedIds;
            var lastProcessedRecordId = _appConfig.LastProcessedRecordId;
            
            if (processedMetadata.Any())
            {
                var minId = processedMetadata.Min(x => x.Id);
                var allIds = processedMetadata.Select(x => x.Id).ToArray();

                return dataContext.DocumentFiles.Where(x =>
                    x.TruckmateTable.Equals("TLORDER") &&
                    x.Id >= minId &&
                    x.Id <= lastProcessedRecordId &&
                    !allIds.Contains(x.Id)
                );
            }
            
            return Enumerable.Empty<DocumentFile>().AsQueryable();
        }

        private IQueryable<DocumentFile> GetNewlyAddedRecords(ApplicationDataContext dataContext)
        {
            var lastProcessedRecordId = _appConfig.LastProcessedRecordId;
            return dataContext.DocumentFiles.Where(x => x.TruckmateTable.Equals("TLORDER") && x.Id > lastProcessedRecordId);
        }
    }
}