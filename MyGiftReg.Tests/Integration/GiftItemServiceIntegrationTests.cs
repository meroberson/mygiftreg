using Azure;
using Azure.Data.Tables;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Services;
using MyGiftReg.Backend.Storage;
using MyGiftReg.Backend.Exceptions;
using Xunit.Abstractions;

namespace MyGiftReg.Tests.Integration
{
    public class GiftItemServiceIntegrationTests : AzuriteTestBase
    {
        private GiftItemService _giftItemService = null!;
        private GiftItemRepository _giftItemRepository = null!;
        private GiftListService _giftListService = null!;
        private GiftListRepository _giftListRepository = null!;
        private AzureTableConfig _tableConfig = null!;

        public GiftItemServiceIntegrationTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _giftListRepository = new GiftListRepository(TableServiceClient);
            _giftListService = new GiftListService(_giftListRepository);
            _giftItemRepository = new GiftItemRepository(TableServiceClient);
            _giftItemService = new GiftItemService(_giftItemRepository, _giftListRepository);
            _tableConfig = ServiceProvider.GetRequiredService<AzureTableConfig>();
        }

        [Fact]
        public async Task CreateGiftItemAsync_ValidRequest_ReturnsCreatedGiftItem()
        {
            // Arrange - First create a gift list
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "serviceuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Service Test Gift Item",
                Description = "Service Test Description",
                Url = "https://example.com/item",
                GiftListId = giftList.Id.ToString()
            };

            // Act
            var result = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Service Test Gift Item", result.Name);
            Assert.Equal("Service Test Description", result.Description);
            Assert.Equal("https://example.com/item", result.Url);
            Assert.Equal(giftList.Id, result.GiftListId);
            Assert.Null(result.ReservedBy); // Should not be reserved initially

            // Verify it's persisted
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), result.Id.ToString(), userId);
            Assert.NotNull(retrieved);
            Assert.Equal(_testPrefix + "_Service Test Gift Item", retrieved.Name);
        }

        [Fact]
        public async Task CreateGiftItemAsync_UserNotOwnerOfGiftList_ThrowsValidationException()
        {
            // Arrange - Create gift list owned by one user
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            // Other user tries to add item
            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Unauthorized Item",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var otherUserId = "otheruser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, otherUserId));
        }

        [Fact]
        public async Task CreateGiftItemAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Test Item",
                Description = "Test",
                GiftListId = "giftlistid"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.CreateGiftItemAsync("", itemRequest, userId));
        }

        [Fact]
        public async Task CreateGiftItemAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Test Item",
                Description = "Test",
                GiftListId = "giftlistid"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ""));
        }

        [Fact]
        public async Task CreateGiftItemAsync_NonExistentGiftList_ThrowsNotFoundException()
        {
            // Arrange
            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Test Item",
                Description = "Test",
                GiftListId = "non-existent-gift-list-id"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                async () => await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId));
        }

        [Fact]
        public async Task GetGiftItemAsync_ValidItem_ReturnsGiftItem()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Get Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Get Service Test Gift Item",
                Description = "Get Service Test Description",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId);

            // Act
            var result = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Get Service Test Gift Item", result.Name);
            Assert.Equal("Get Service Test Description", result.Description);
            Assert.Equal(giftList.Id, result.GiftListId);
        }

        [Fact]
        public async Task GetGiftItemAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.GetGiftItemAsync("", "giftlistid", "itemid", "userid"));
        }

        [Fact]
        public async Task GetGiftItemAsync_EmptyGiftListId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", "", "itemid", "userid"));
        }

        [Fact]
        public async Task GetGiftItemAsync_EmptyItemId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", "giftlistid", "", "userid"));
        }

        [Fact]
        public async Task GetGiftItemAsync_NonExistingItem_ReturnsNull()
        {
            // Act
            var result = await _giftItemService.GetGiftItemAsync(_testPrefix + "_NonExistingEvent", "non-existing-gift-list-id", "non-existing-item-id", "userid");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateGiftItemAsync_ExistingItem_ReturnsUpdatedGiftItem()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Update Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Original Service Test Gift Item",
                Description = "Original Service Test Description",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId);

            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Updated Service Test Gift Item",
                Description = "Updated Service Test Description",
                Url = "https://example.com/updated-item",
                GiftListId = giftList.Id.ToString()
            };

            // Act
            var result = await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), updateRequest, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Updated Service Test Gift Item", result.Name);
            Assert.Equal("Updated Service Test Description", result.Description);
            Assert.Equal("https://example.com/updated-item", result.Url);
            Assert.Equal(createdItem.Id, result.Id); // ID should remain the same
            Assert.Equal(createdItem.ReservedBy, result.ReservedBy); // Reservation status should be preserved

            // Verify update persisted
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), userId);
            Assert.NotNull(retrieved);
            Assert.Equal(_testPrefix + "_Updated Service Test Gift Item", retrieved.Name);
            Assert.Equal("Updated Service Test Description", retrieved.Description);
        }

        [Fact]
        public async Task UpdateGiftItemAsync_UserNotOwnerOfGiftList_ThrowsValidationException()
        {
            // Arrange - Create gift list owned by one user
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Update Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Original Item",
                Description = "Original",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Unauthorized Update",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var otherUserId = "otheruser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), updateRequest, otherUserId));
        }

        [Fact]
        public async Task UpdateGiftItemAsync_NonExistingItem_ThrowsNotFoundException()
        {
            // Arrange
            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Non Existent Service Gift Item",
                Description = "This doesn't exist",
                GiftListId = "giftlistid"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                async () => await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_NonExistentEvent", "non-existing-gift-list-id", "non-existing-item-id", updateRequest, userId));
        }

        [Fact]
        public async Task UpdateGiftItemAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Test Item",
                Description = "Test",
                GiftListId = "giftlistid"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.UpdateGiftItemAsync("", "giftlistid", "itemid", updateRequest, userId));
        }

        [Fact]
        public async Task UpdateGiftItemAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Test Item",
                Description = "Test",
                GiftListId = "giftlistid"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", "giftlistid", "itemid", updateRequest, ""));
        }

        [Fact]
        public async Task DeleteGiftItemAsync_ExistingItem_ReturnsTrue()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Delete Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Delete Service Test Gift Item",
                Description = "Will be deleted",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId);

            // Act
            var result = await _giftItemService.DeleteGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), userId);

            // Assert
            Assert.True(result);

            // Verify deletion
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), userId);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DeleteGiftItemAsync_NonExistingItem_ReturnsFalse()
        {
            // Act
            var result = await _giftItemService.DeleteGiftItemAsync(_testPrefix + "_NonExistentEvent", "non-existing-gift-list-id", "non-existing-item-id", "testuser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteGiftItemAsync_UserNotOwnerOfGiftList_ThrowsValidationException()
        {
            // Arrange - Create gift list owned by one user
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Delete Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Item to Delete",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            var otherUserId = "otheruser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.DeleteGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), otherUserId));
        }

        [Fact]
        public async Task DeleteGiftItemAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.DeleteGiftItemAsync("", "giftlistid", "itemid", "testuser"));
        }

        [Fact]
        public async Task DeleteGiftItemAsync_EmptyUserId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.DeleteGiftItemAsync(_testPrefix + "_TestEvent", "giftlistid", "itemid", ""));
        }

        [Fact]
        public async Task GetGiftItemsByListAsync_OwnerViewing_HidesReservationStatus()
        {
            // Arrange - Create gift list and items
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owner View Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequests = new List<CreateGiftItemRequest>
            {
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 1", Description = "Item 1", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 2", Description = "Item 2", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 3", Description = "Item 3", GiftListId = giftList.Id.ToString() }
            };

            var createdItems = new List<GiftItem>();
            foreach (var request in itemRequests)
            {
                var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", request, ownerUserId);
                createdItems.Add(item);
            }

            // Reserve one item as a different user
            var otherUserId = "otheruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItems[1].Id.ToString(), otherUserId, "Test User");

            // Act - Owner views their own list
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), ownerUserId);

            // Assert
            Assert.Equal(3, result.Count);
            
            // All items should have null ReservedBy (reservation status hidden from owner)
            Assert.All(result, item => Assert.Null(item.ReservedBy));
        }

        [Fact]
        public async Task GetGiftItemsByListAsync_NonOwnerViewing_ShowsReservationStatus()
        {
            // Arrange - Create gift list and items
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Non-Owner View Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequests = new List<CreateGiftItemRequest>
            {
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 1", Description = "Item 1", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 2", Description = "Item 2", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Item 3", Description = "Item 3", GiftListId = giftList.Id.ToString() }
            };

            var createdItems = new List<GiftItem>();
            foreach (var request in itemRequests)
            {
                var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", request, ownerUserId);
                createdItems.Add(item);
            }

            // Reserve items as different users
            var otherUserId1 = "otheruser1";
            var otherUserId2 = "otheruser2";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItems[1].Id.ToString(), otherUserId1, "Test User 1");
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItems[2].Id.ToString(), otherUserId2, "Test User 2");

            // Act - Other user views the list
            var viewerUserId = "vieweruser";
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), viewerUserId);

            // Assert - Check reservation status regardless of order
            Assert.Equal(3, result.Count);
            
            // First item should not be reserved
            var unreservedItem = result.FirstOrDefault(item => item.ReservedBy == null);
            Assert.NotNull(unreservedItem);
            
            // Other items should show reservation status
            var reservedItem1 = result.FirstOrDefault(item => item.ReservedBy == otherUserId1);
            Assert.NotNull(reservedItem1);
            
            var reservedItem2 = result.FirstOrDefault(item => item.ReservedBy == otherUserId2);
            Assert.NotNull(reservedItem2);
        }

        [Fact]
        public async Task ReserveGiftItemAsync_ValidRequest_ReturnsReservedItem()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Reserve Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Reserve Service Test Gift Item",
                Description = "Will be reserved",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            var reserverUserId = "reserveruser";

            // Act
            var result = await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId, "Test User");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reserverUserId, result.ReservedBy);
            Assert.Equal("Test User", result.ReservedByDisplayName);
            Assert.Equal(createdItem.Id, result.Id);

            // Verify reservation persisted
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId);
            Assert.NotNull(retrieved);
            Assert.Equal(reserverUserId, retrieved.ReservedBy);
            Assert.Equal("Test User", retrieved.ReservedByDisplayName);
        }

        [Fact]
        public async Task ReserveGiftItemAsync_UserOwningList_ThrowsValidationException()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owner Reserve Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Item for Owner Reserve Test",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Act & Assert - Owner tries to reserve their own item
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), ownerUserId, "Test User"));
        }

        [Fact]
        public async Task ReserveGiftItemAsync_AlreadyReserved_ThrowsValidationException()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Double Reserve Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Item for Double Reserve Test",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // First user reserves
            var firstReserverUserId = "firstreserver";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), firstReserverUserId, "Test User");

            // Act & Assert - Second user tries to reserve
            var secondReserverUserId = "secondreserver";
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), secondReserverUserId, "Test User"));
        }

        [Fact]
        public async Task UnreserveGiftItemAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange - Create gift list, item, and reserve it
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Unreserve Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Unreserve Service Test Gift Item",
                Description = "Will be unreserved",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            var reserverUserId = "reserveruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId, "Test User");

            // Act
            var result = await _giftItemService.UnreserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId);

            // Assert
            Assert.True(result);

            // Verify unreservation persisted
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId);
            Assert.NotNull(retrieved);
            Assert.Null(retrieved.ReservedBy);
        }

        [Fact]
        public async Task UnreserveGiftItemAsync_UserDidNotReserve_ThrowsValidationException()
        {
            // Arrange - Create gift list, item, and reserve it
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Wrong User Unreserve Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Item for Wrong User Unreserve Test",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // One user reserves
            var reserverUserId = "reserveruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId, "Test User");

            // Act & Assert - Different user tries to unreserve
            var otherUserId = "otheruser";
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.UnreserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), otherUserId));
        }

        [Fact]
        public async Task GetGiftItemsByListAsync_MultipleItems_ReturnsAllItems()
        {
            // Arrange - Create gift list and multiple items
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Multiple Items Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequests = new List<CreateGiftItemRequest>
            {
                new CreateGiftItemRequest { Name = _testPrefix + "_Service Item 1", Description = "Description 1", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Service Item 2", Description = "Description 2", GiftListId = giftList.Id.ToString() },
                new CreateGiftItemRequest { Name = _testPrefix + "_Service Item 3", Description = "Description 3", GiftListId = giftList.Id.ToString() }
            };

            var createdItems = new List<GiftItem>();
            foreach (var request in itemRequests)
            {
                var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", request, userId);
                createdItems.Add(item);
            }

            // Act
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), userId);

            // Assert
            var ourItems = result.Where(item => item.Name.StartsWith(_testPrefix + "_Service Item")).ToList();
            Assert.Equal(3, ourItems.Count);

            // Verify our specific items are present
            var ourItemNames = ourItems.Select(item => item.Name).ToList();
            Assert.Contains(_testPrefix + "_Service Item 1", ourItemNames);
            Assert.Contains(_testPrefix + "_Service Item 2", ourItemNames);
            Assert.Contains(_testPrefix + "_Service Item 3", ourItemNames);
        }

        [Fact]
        public async Task GetGiftItemsByListAsync_NoItems_ReturnsEmptyList()
        {
            // Arrange - Create empty gift list
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Empty Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            // Act
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Where(item => item.Name.StartsWith(_testPrefix + "_")));
        }

        [Fact]
        public async Task ServiceAndRepository_ConsistencyTest()
        {
            // Arrange - Create via service
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Consistency Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, userId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Consistency Test Gift Item",
                Description = "Testing service-repository consistency",
                GiftListId = giftList.Id.ToString()
            };
            var serviceResult = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, userId);

            // Act - Retrieve via repository
            var repoResult = await _giftItemRepository.GetAsync(giftList.Id.ToString(), serviceResult.Id.ToString());

            // Assert
            Assert.NotNull(serviceResult);
            Assert.NotNull(repoResult);
            Assert.Equal(serviceResult.Name, repoResult.Name);
            Assert.Equal(serviceResult.Description, repoResult.Description);
            Assert.Equal(serviceResult.Url, repoResult.Url);
            Assert.Equal(serviceResult.GiftListId, repoResult.GiftListId);
            Assert.Equal(serviceResult.PartitionKey, repoResult.PartitionKey);
            Assert.Equal(serviceResult.RowKey, repoResult.RowKey);
        }

        [Fact]
        public async Task AuthorizationTest_UserCannotEditOthersGiftItems()
        {
            // Arrange
            var ownerUserId = "owneruser";
            var otherUserId = "otheruser";

            // Create gift list and item owned by one user
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Gift Item Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Owned Gift Item",
                Description = "Original description",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Unauthorized Update Attempt",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };

            // Act & Assert - Other user cannot update
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), updateRequest, otherUserId));
        }

        [Fact]
        public async Task AuthorizationTest_UserCannotDeleteOthersGiftItems()
        {
            // Arrange
            var ownerUserId = "owneruser";
            var otherUserId = "otheruser";

            // Create gift list and item owned by one user
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Delete Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Owned Gift Item for Delete",
                Description = "Should fail",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Act & Assert - Other user cannot delete
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftItemService.DeleteGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), otherUserId));
        }

        [Fact]
        public async Task ReservationVisibilityTest_OwnerCannotSeeReservationStatus()
        {
            // Arrange - Create gift list and items
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Reservation Visibility Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Reservation Test Item",
                Description = "Test reservation visibility",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Another user reserves the item
            var reserverUserId = "reserveruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), reserverUserId, "Test User");

            // Act - Owner views their list
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), ownerUserId);

            // Assert - Owner should not see reservation status
            Assert.Single(result);
            Assert.Null(result[0].ReservedBy);
        }

        [Fact]
        public async Task ReservationVisibilityTest_OthersCanSeeReservationStatus()
        {
            // Arrange - Create gift list and items
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Others Visibility Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Others Visibility Test Item",
                Description = "Test others visibility",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Reserve the item
            var reserverUserId = "reserveruser";
            var reserverUserDisplayName = "Reserver User";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), reserverUserId, reserverUserDisplayName);

            // Act - Another user views the list
            var viewerUserId = "vieweruser";
            var result = await _giftItemService.GetGiftItemsByListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), viewerUserId);

            // Assert - Others should see reservation status
            Assert.Single(result);
            Assert.Equal(reserverUserId, result[0].ReservedBy);
            Assert.Equal(reserverUserDisplayName, result[0].ReservedByDisplayName);
        }

        [Fact]
        public async Task GetGiftItemAsync_OwnerViewing_HidesReservationStatus()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Single Item Owner View Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Single Item Owner View Test Item",
                Description = "Test single item owner view",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Another user reserves the item
            var reserverUserId = "reserveruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), reserverUserId, "Test User");

            // Act - Owner views the specific item
            var result = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), ownerUserId);

            // Assert - Owner should not see reservation status
            Assert.NotNull(result);
            Assert.Null(result.ReservedBy);
            Assert.Null(result.ReservedByDisplayName);
        }

        [Fact]
        public async Task GetGiftItemAsync_NonOwnerViewing_ShowsReservationStatus()
        {
            // Arrange - Create gift list and item
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Single Item Non-Owner View Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Single Item Non-Owner View Test Item",
                Description = "Test single item non-owner view",
                GiftListId = giftList.Id.ToString()
            };
            var item = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Reserve the item
            var reserverUserId = "reserveruser";
            var reserverUserDisplayName = "Reserver User";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), reserverUserId, reserverUserDisplayName);

            // Act - Another user views the specific item
            var viewerUserId = "vieweruser";
            var result = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), item.Id.ToString(), viewerUserId);

            // Assert - Others should see reservation status
            Assert.NotNull(result);
            Assert.Equal(reserverUserId, result.ReservedBy);
            Assert.Equal(reserverUserDisplayName, result.ReservedByDisplayName);
        }
    }
}
