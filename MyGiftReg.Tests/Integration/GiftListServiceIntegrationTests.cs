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
    public class GiftListServiceIntegrationTests : AzuriteTestBase
    {
        private GiftListService _giftListService = null!;
        private GiftListRepository _giftListRepository = null!;
        private AzureTableConfig _tableConfig = null!;

        public GiftListServiceIntegrationTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _giftListRepository = new GiftListRepository(TableServiceClient);
            _giftListService = new GiftListService(_giftListRepository);
            _tableConfig = ServiceProvider.GetRequiredService<AzureTableConfig>();
        }

        [Fact]
        public async Task CreateGiftListAsync_ValidRequest_ReturnsCreatedGiftList()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "serviceuser";

            // Act
            var result = await _giftListService.CreateGiftListAsync(request, userId, "Service User");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Service Test Gift List", result.Name);
            Assert.Equal(_testPrefix + "_TestEvent", result.EventName);
            Assert.Equal(userId, result.Owner);
            Assert.Equal("Service User", result.OwnerDisplayName);
            Assert.NotNull(result.CreatedDate);

            // Verify it's persisted
            var retrieved = await _giftListService.GetGiftListAsync(_testPrefix + "_TestEvent", result.Id.ToString());
            Assert.NotNull(retrieved);
            Assert.Equal(_testPrefix + "_Service Test Gift List", retrieved.Name);
        }

        [Fact]
        public async Task CreateGiftListAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Empty User Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.CreateGiftListAsync(request, "", "Test User"));
        }

        [Fact]
        public async Task CreateGiftListAsync_NullUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Null User Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.CreateGiftListAsync(request, null!, "Test User"));
        }

        [Fact]
        public async Task CreateGiftListAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Test Gift List",
                EventName = ""
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.CreateGiftListAsync(request, userId, "Test User"));
        }

        [Fact]
        public async Task GetGiftListAsync_ValidGiftList_ReturnsGiftList()
        {
            // Arrange
            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Get Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var createdGiftList = await _giftListService.CreateGiftListAsync(createRequest, userId, "Test User");

            // Act
            var result = await _giftListService.GetGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Get Service Test Gift List", result.Name);
            Assert.Equal(_testPrefix + "_TestEvent", result.EventName);
            Assert.Equal(userId, result.Owner);
            Assert.Equal("Test User", result.OwnerDisplayName);
        }

        [Fact]
        public async Task GetGiftListAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.GetGiftListAsync("", "giftlistid"));
        }

        [Fact]
        public async Task GetGiftListAsync_EmptyGiftListId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.GetGiftListAsync(_testPrefix + "_TestEvent", ""));
        }

        [Fact]
        public async Task GetGiftListAsync_NonExistingGiftList_ReturnsNull()
        {
            // Act
            var result = await _giftListService.GetGiftListAsync(_testPrefix + "_NonExistingEvent", "non-existing-gift-list-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateGiftListAsync_ExistingGiftList_ReturnsUpdatedGiftList()
        {
            // Arrange
            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Original Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "originaluser";
            var createdGiftList = await _giftListService.CreateGiftListAsync(createRequest, userId, "Test User");

            var updateRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Updated Service Test Gift List",
                EventName = _testPrefix + "_TestEvent" // Keep same event name to avoid PartitionKey issues
            };
            var updateUserId = "originaluser";

            // Act
            var result = await _giftListService.UpdateGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString(), updateRequest, updateUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Updated Service Test Gift List", result.Name);
            Assert.Equal(_testPrefix + "_TestEvent", result.EventName);
            Assert.Equal(userId, result.Owner); // Owner should remain original
            Assert.Equal("Test User", result.OwnerDisplayName); // Owner display name should remain original

            // Verify update persisted
            var retrieved = await _giftListService.GetGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString());
            Assert.NotNull(retrieved);
            Assert.Equal(_testPrefix + "_Updated Service Test Gift List", retrieved.Name);
            Assert.Equal(_testPrefix + "_TestEvent", retrieved.EventName);
        }

        [Fact]
        public async Task UpdateGiftListAsync_NonExistingGiftList_ThrowsNotFoundException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Non Existent Service Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                async () => await _giftListService.UpdateGiftListAsync(_testPrefix + "_NonExistentEvent", "non-existing-gift-list-id", request, userId));
        }

        [Fact]
        public async Task UpdateGiftListAsync_UserNotOwner_ThrowsValidationException()
        {
            // Arrange
            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var createdGiftList = await _giftListService.CreateGiftListAsync(createRequest, ownerUserId, "Test User");

            var updateRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Attempted Update",
                EventName = _testPrefix + "_TestEvent"
            };
            var otherUserId = "otheruser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.UpdateGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString(), updateRequest, otherUserId));
        }

        [Fact]
        public async Task UpdateGiftListAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.UpdateGiftListAsync("", "giftlistid", request, userId));
        }

        [Fact]
        public async Task UpdateGiftListAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.UpdateGiftListAsync(_testPrefix + "_TestEvent", "giftlistid", request, ""));
        }

        [Fact]
        public async Task DeleteGiftListAsync_ExistingGiftList_ReturnsTrue()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Delete Service Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var createdGiftList = await _giftListService.CreateGiftListAsync(request, userId, "Test User");

            // Act
            var result = await _giftListService.DeleteGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString(), userId);

            // Assert
            Assert.True(result);

            // Verify deletion
            var retrieved = await _giftListService.GetGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString());
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DeleteGiftListAsync_NonExistingGiftList_ReturnsFalse()
        {
            // Act
            var result = await _giftListService.DeleteGiftListAsync(_testPrefix + "_NonExistentEvent", "non-existing-gift-list-id", "testuser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteGiftListAsync_UserNotOwner_ThrowsValidationException()
        {
            // Arrange
            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Delete Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var createdGiftList = await _giftListService.CreateGiftListAsync(createRequest, ownerUserId, "Test User");

            var otherUserId = "otheruser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.DeleteGiftListAsync(_testPrefix + "_TestEvent", createdGiftList.Id.ToString(), otherUserId));
        }

        [Fact]
        public async Task DeleteGiftListAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.DeleteGiftListAsync("", "giftlistid", "testuser"));
        }

        [Fact]
        public async Task DeleteGiftListAsync_EmptyUserId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.DeleteGiftListAsync(_testPrefix + "_TestEvent", "giftlistid", ""));
        }

        [Fact]
        public async Task GetGiftListsByEventAsync_MultipleGiftLists_ReturnsAllGiftLists()
        {
            // Arrange
            var requests = new List<(CreateGiftListRequest request, string userId)>
            {
                (new CreateGiftListRequest { Name = _testPrefix + "_Service Gift List 1", EventName = _testPrefix + "_TestEvent" }, "user1"),
                (new CreateGiftListRequest { Name = _testPrefix + "_Service Gift List 2", EventName = _testPrefix + "_TestEvent" }, "user2"),
                (new CreateGiftListRequest { Name = _testPrefix + "_Service Gift List 3", EventName = _testPrefix + "_TestEvent" }, "user3")
            };

            foreach (var (request, userId) in requests)
            {
                await _giftListService.CreateGiftListAsync(request, userId, "Test User");
            }

            // Act
            var result = await _giftListService.GetGiftListsByEventAsync(_testPrefix + "_TestEvent");

            // Assert - Filter for only our test gift lists to avoid conflicts
            var ourGiftLists = result.Where(gl => gl.Name.StartsWith(_testPrefix + "_Service Gift List")).ToList();
            Assert.Equal(3, ourGiftLists.Count);

            // Verify our specific gift lists are present
            var ourGiftListNames = ourGiftLists.Select(gl => gl.Name).ToList();
            Assert.Contains(_testPrefix + "_Service Gift List 1", ourGiftListNames);
            Assert.Contains(_testPrefix + "_Service Gift List 2", ourGiftListNames);
            Assert.Contains(_testPrefix + "_Service Gift List 3", ourGiftListNames);
        }

        [Fact]
        public async Task GetGiftListsByEventAsync_NoGiftLists_ReturnsEmptyList()
        {
            // Act
            var result = await _giftListService.GetGiftListsByEventAsync(_testPrefix + "_EmptyTestEvent");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Where(gl => gl.Name.StartsWith(_testPrefix + "_"))); // Only check our events
        }

        [Fact]
        public async Task GetGiftListsByEventAndUserAsync_SpecificUser_ReturnsUserGiftLists()
        {
            // Arrange
            var targetUserId = "targetuser";
            var otherUserId = "otheruser";

            var requests = new List<(CreateGiftListRequest request, string userId)>
            {
                (new CreateGiftListRequest { Name = _testPrefix + "_Target User List 1", EventName = _testPrefix + "_TestEvent" }, targetUserId),
                (new CreateGiftListRequest { Name = _testPrefix + "_Target User List 2", EventName = _testPrefix + "_TestEvent" }, targetUserId),
                (new CreateGiftListRequest { Name = _testPrefix + "_Other User List", EventName = _testPrefix + "_TestEvent" }, otherUserId)
            };

            foreach (var (request, userId) in requests)
            {
                await _giftListService.CreateGiftListAsync(request, userId, "Test User");
            }

            // Act
            var result = await _giftListService.GetGiftListsByEventAndUserAsync(_testPrefix + "_TestEvent", targetUserId);

            // Assert
            var ourGiftLists = result.Where(gl => gl.Name.StartsWith(_testPrefix + "_")).ToList();
            Assert.Equal(2, ourGiftLists.Count);

            // Verify all returned gift lists belong to target user
            Assert.All(ourGiftLists, gl => Assert.Equal(targetUserId, gl.Owner));

            var ourGiftListNames = ourGiftLists.Select(gl => gl.Name).ToList();
            Assert.Contains(_testPrefix + "_Target User List 1", ourGiftListNames);
            Assert.Contains(_testPrefix + "_Target User List 2", ourGiftListNames);
            Assert.DoesNotContain(_testPrefix + "_Other User List", ourGiftListNames);
        }

        [Fact]
        public async Task GetGiftListsByEventForOthersAsync_SpecificUser_ReturnsOtherUsersGiftLists()
        {
            // Arrange
            var targetUserId = "targetuser";
            var otherUserId1 = "otheruser1";
            var otherUserId2 = "otheruser2";

            var requests = new List<(CreateGiftListRequest request, string userId)>
            {
                (new CreateGiftListRequest { Name = _testPrefix + "_Target User List", EventName = _testPrefix + "_TestEvent" }, targetUserId),
                (new CreateGiftListRequest { Name = _testPrefix + "_Other User List 1", EventName = _testPrefix + "_TestEvent" }, otherUserId1),
                (new CreateGiftListRequest { Name = _testPrefix + "_Other User List 2", EventName = _testPrefix + "_TestEvent" }, otherUserId2)
            };

            foreach (var (request, userId) in requests)
            {
                await _giftListService.CreateGiftListAsync(request, userId, "Test User");
            }

            // Act
            var result = await _giftListService.GetGiftListsByEventForOthersAsync(_testPrefix + "_TestEvent", targetUserId);

            // Assert
            var ourGiftLists = result.Where(gl => gl.Name.StartsWith(_testPrefix + "_")).ToList();
            Assert.Equal(2, ourGiftLists.Count);

            // Verify all returned gift lists belong to other users
            Assert.All(ourGiftLists, gl => Assert.NotEqual(targetUserId, gl.Owner));

            var ourGiftListNames = ourGiftLists.Select(gl => gl.Name).ToList();
            Assert.Contains(_testPrefix + "_Other User List 1", ourGiftListNames);
            Assert.Contains(_testPrefix + "_Other User List 2", ourGiftListNames);
            Assert.DoesNotContain(_testPrefix + "_Target User List", ourGiftListNames);
        }

        [Fact]
        public async Task ServiceAndRepository_ConsistencyTest()
        {
            // Arrange - Create via service
            var serviceRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Consistency Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var userId = "testuser";
            var serviceResult = await _giftListService.CreateGiftListAsync(serviceRequest, userId, "Test User");

            // Act - Retrieve via repository
            var repoResult = await _giftListRepository.GetAsync(_testPrefix + "_TestEvent", serviceResult.Id.ToString());

            // Assert
            Assert.NotNull(serviceResult);
            Assert.NotNull(repoResult);
            Assert.Equal(serviceResult.Name, repoResult.Name);
            Assert.Equal(serviceResult.EventName, repoResult.EventName);
            Assert.Equal(serviceResult.Owner, repoResult.Owner);
            Assert.Equal(serviceResult.OwnerDisplayName, repoResult.OwnerDisplayName);
            Assert.Equal(serviceResult.PartitionKey, repoResult.PartitionKey);
            Assert.Equal(serviceResult.RowKey, repoResult.RowKey);
        }

        [Fact]
        public async Task AuthorizationTest_UserCannotEditOthersGiftLists()
        {
            // Arrange
            var ownerUserId = "owneruser";
            var otherUserId = "otheruser";

            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var giftList = await _giftListService.CreateGiftListAsync(createRequest, ownerUserId, "Test User");

            var updateRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Unauthorized Update Attempt",
                EventName = _testPrefix + "_TestEvent"
            };

            // Act & Assert - Other user cannot update
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.UpdateGiftListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), updateRequest, otherUserId));
        }

        [Fact]
        public async Task AuthorizationTest_UserCannotDeleteOthersGiftLists()
        {
            // Arrange
            var ownerUserId = "owneruser";
            var otherUserId = "otheruser";

            var createRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Owned Gift List for Delete",
                EventName = _testPrefix + "_TestEvent"
            };
            var giftList = await _giftListService.CreateGiftListAsync(createRequest, ownerUserId, "Test User");

            // Act & Assert - Other user cannot delete
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _giftListService.DeleteGiftListAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), otherUserId));
        }
    }
}
