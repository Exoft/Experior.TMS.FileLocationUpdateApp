using System.Collections.Generic;

namespace Experior.TMS.FileLocationUpdateApp.Configuration
{
    public class AppConfiguration
    {
        public string DbConnectionString { get; set; }
        public int Timeout { get; set; }
        public string OutputDirectory { get; set; }
        public string OutputCopyDirectory { get; set; }
        public int LastProcessedRecordId { get; set; }

        public List<RecordMetadata> ProcessedIds { get; set; }
    }

    public class RecordMetadata
    {
        public int Id { get; set; }
        public bool Processed { get; set; }
    }
}