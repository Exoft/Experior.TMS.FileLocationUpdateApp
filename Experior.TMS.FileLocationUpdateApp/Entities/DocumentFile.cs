using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Experior.TMS.FileLocationUpdateApp.Entities
{
        [Table("TMWIN.DOCUMENT_FILES")]
    public class DocumentFile
    {
        [Column("ID")]
        [Key]
        public int Id { get; set; }
        [Column("TRUCKMATE_TABLE")]
        public string TruckmateTable { get; set; }
        [Column("TRUCKMATE_VALUE")]
        public string TruckmateValue { get; set; }
        [Column("FILE_DESC")]
        public string FileDesc { get; set; }
        [Column("FILE_PATH")]
        public string FilePath { get; set; }
        [Column("ROW_TIMESTAMP")]
        public DateTime RowTimestamp { get; set; }
    }
}