using Azure;
using Azure.Data.Tables;

namespace MyGiftReg.Backend.Storage
{
    public class AzureTableConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string EventTableName { get; set; } = "EventTable";
        public string GiftListTableName { get; set; } = "GiftListTable";
        public string GiftItemTableName { get; set; } = "GiftItemTable";

        public TableClient CreateTableClient(string tableName)
        {
            var serviceClient = new TableServiceClient(ConnectionString);
            return serviceClient.GetTableClient(tableName);
        }
    }
}
