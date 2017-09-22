using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;

namespace Experior.TMS.FileLocationUpdateApp.Configuration
{
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;

        protected ConfigurationManager()
        {
            ApplicationConfiguration = ParseConfiguration();
        }

        public static ConfigurationManager Instance
        {
            get { return _instance ?? (_instance = new ConfigurationManager()); }
        }

        public AppConfiguration ApplicationConfiguration { get; private set; }
        
        public void UpdatePositionMarkerInConfig()
        {
            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            config.AppSettings.Settings["LastProcessedRecordId"].Value = ApplicationConfiguration.LastProcessedRecordId.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
        }

        public void SaveProcessedIds()
        {
            var serializer = new ConfigurationSerializer();
            
            serializer.Serialize(ApplicationConfiguration.ProcessedIds, "processedIds.txt");
        }

        private AppConfiguration ParseConfiguration()
        {
            int timeout;
            int lastProcessedRecordId;
            if (!Int32.TryParse(System.Configuration.ConfigurationManager.AppSettings["Timeout"], out timeout))
                timeout = 5 * 60;
            if (!Int32.TryParse(System.Configuration.ConfigurationManager.AppSettings["LastProcessedRecordId"], out lastProcessedRecordId))
                lastProcessedRecordId = 0;
            string outputDir = System.Configuration.ConfigurationManager.AppSettings["OutputDirectory"];
            string outputCopyDir = System.Configuration.ConfigurationManager.AppSettings["OutputCopyDirectory"];
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            var serializer = new ConfigurationSerializer();
            var processedIds = serializer.Deserialize<List<RecordMetadata>>("processedIds.txt");

            return new AppConfiguration
            {
                Timeout = timeout * 1000,
                DbConnectionString = connectionString,
                OutputDirectory = outputDir,
                OutputCopyDirectory = outputCopyDir,
                LastProcessedRecordId = lastProcessedRecordId,
                ProcessedIds = processedIds ?? new List<RecordMetadata>()
            };
        }
    }
}