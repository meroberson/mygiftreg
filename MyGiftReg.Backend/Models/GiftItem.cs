using Azure;
using Azure.Data.Tables;

namespace MyGiftReg.Backend.Models
{
    public class GiftItem : ITableEntity
    {
        // Azure Table Storage required properties
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Domain model properties
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? ReservedBy { get; set; }
        public Guid GiftListId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsReserved => !string.IsNullOrEmpty(ReservedBy);
    }
}
