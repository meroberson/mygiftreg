using Azure;
using Azure.Data.Tables;
using System.Text.Json;
using System.Runtime.Serialization;

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
        public string? ReservedBy
        {
            get
            {
                return null;
            }

            set
            {
                if (value != null)
                {
                    if (Reservations.Count == 0)
                    {
                        Reservations.Add(new Reservation() { Quantity = 1, UserId = value, UserDisplayName = "" });
                    } else
                    {
                         Reservations.First().UserId = value;
                    }
                } 
            }
        } // For backward compatibility

        public string? ReservedByDisplayName
        {
            get
            {
                return null;
            }

            set
            {
                if (value != null)
                {
                    if (Reservations.Count == 0)
                    {
                        Reservations.Add(new Reservation() { Quantity = 1, UserId = "", UserDisplayName = value });
                    } else
                    {
                         Reservations.First().UserDisplayName = value;
                    }
                } 
            }
        } // For backward compatibility

        public int Quantity { get; set; } = 1;
        
        public string? ReservationsJson
        {
            get
            {
                return JsonSerializer.Serialize(Reservations);
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        Reservations = JsonSerializer.Deserialize<List<Reservation>>(value) ?? [];
                    }
                    catch
                    {
                        Reservations = new List<Reservation>();
                    }
                }
            }
        } // JSON representation of reservations

        public Guid GiftListId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Computed property - not stored directly, so ignore it for Azure Table Storage
        [IgnoreDataMember]
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Helper property to get total reserved quantity
        [IgnoreDataMember]
        public int TotalReserved => Reservations.Sum(r => r.Quantity);

        // Helper property to check if item is fully reserved
        [IgnoreDataMember]
        public bool IsFullyReserved => TotalReserved >= Quantity;
    }
}
