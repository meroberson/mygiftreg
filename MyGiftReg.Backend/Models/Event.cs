using Azure;
using Azure.Data.Tables;

namespace MyGiftReg.Backend.Models
{
    public class Event : ITableEntity
    {
        // Azure Table Storage required properties
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Domain model properties
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? EventDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedByDisplayName { get; set; } = string.Empty;
    }
}
