using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Experior.TMS.FileLocationUpdateApp.Entities
{
    [Table("TMWIN.DOCUMENT_FILES_USERS_AUDIT")]
    public class DocumentFilesUsersAudit
    {
        [Column("ID")]
        [Key]
        public int Id { get; set; }

        [Column("DOCUMENT_FILES_ID")]
        public int DocumentFilesId { get; set; }

        [Column("USERNAME")]
        public string UserName { get; set; }
    }
}