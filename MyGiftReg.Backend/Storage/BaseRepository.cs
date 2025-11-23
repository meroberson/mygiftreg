using Azure;
using Azure.Data.Tables;
using System.Net;

namespace MyGiftReg.Backend.Storage
{
    public abstract class BaseRepository<T> where T : class, ITableEntity, new()
    {
        protected readonly TableClient _tableClient;

        protected BaseRepository(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public async Task<T?> GetAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<T> CreateAsync(T entity)
        {
            Response response = await _tableClient.AddEntityAsync(entity);
            entity.ETag = response.Headers.ETag ?? default(ETag);
            entity.Timestamp = response.Headers.Date;
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            Response response = await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
            entity.ETag = response.Headers.ETag ?? default(ETag);
            entity.Timestamp = response.Headers.Date;
            
            return entity;
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<bool> ExistsAsync(string partitionKey, string rowKey)
        {
            var entity = await GetAsync(partitionKey, rowKey);
            return entity != null;
        }

        public async Task<List<T>> GetAllAsync()
        {
            var entities = new List<T>();
            
            await foreach (var entity in _tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }
            
            return entities;
        }

        public List<T> GetByPartitionKey(string partitionKey, IEnumerable<T> allEntities)
        {
            return allEntities.Where(e => e.PartitionKey == partitionKey).ToList();
        }
    }
}
