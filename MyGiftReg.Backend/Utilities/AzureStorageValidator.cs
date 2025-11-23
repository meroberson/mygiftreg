using MyGiftReg.Backend.Exceptions;

namespace MyGiftReg.Backend.Utilities
{
    /// <summary>
    /// Provides validation methods for Azure Table Storage naming restrictions.
    /// </summary>
    public static class AzureStorageValidator
    {
        /// <summary>
        /// Validates that an event name conforms to Azure Table Storage PartitionKey and RowKey naming restrictions.
        /// </summary>
        /// <param name="eventName">The event name to validate</param>
        /// <exception cref="ValidationException">Thrown when the event name is invalid for Azure Storage</exception>
        public static void ValidateEventNameForAzureStorage(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ValidationException("Event name cannot be null, empty, or whitespace.");
            }

            // Check maximum length (1024 characters for Azure Table Storage)
            if (eventName.Length > 1024)
            {
                throw new ValidationException($"Event name exceeds maximum length of 1024 characters. Current length: {eventName.Length}");
            }

            // Azure Table Storage forbidden characters in PartitionKey and RowKey
            var forbiddenCharacters = new[] { '/', '\\', '#', '?' };
            
            foreach (var forbiddenChar in forbiddenCharacters)
            {
                if (eventName.Contains(forbiddenChar))
                {
                    throw new ValidationException($"Event name cannot contain the character '{forbiddenChar}' as it is not allowed in Azure Table Storage PartitionKey and RowKey.");
                }
            }

            // Check for control characters (U+0000 to U+001F and U+007F to U+009F)
            foreach (char c in eventName)
            {
                var code = (int)c;
                if ((code >= 0x0000 && code <= 0x001F) || (code >= 0x007F && code <= 0x009F))
                {
                    throw new ValidationException($"Event name cannot contain control character U+{code:X4} which is not allowed in Azure Table Storage PartitionKey and RowKey.");
                }
            }

            // Check that the name doesn't start or end with spaces (common issue)
            if (eventName.StartsWith(' ') || eventName.EndsWith(' '))
            {
                throw new ValidationException("Event name cannot start or end with whitespace characters.");
            }
        }
    }
}
