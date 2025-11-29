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
    public class GiftItemQuantityTests : AzuriteTestBase
    {
        private GiftItemService _giftItemService = null!;
        private GiftItemRepository _giftItemRepository = null!;
        private GiftListService _giftListService = null!;
        private GiftListRepository _giftListRepository = null!;

        public GiftItemQuantityTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _giftListRepository = new GiftListRepository(TableServiceClient);
            _giftListService = new GiftListService(_giftListRepository);
            _giftItemRepository = new GiftItemRepository(TableServiceClient);
            _giftItemService = new GiftItemService(_giftItemRepository, _giftListRepository);
        }

        [Fact]
        public async Task UpdateGiftItemAsync_QuantityChange_PreservesReservations()
        {
            // Arrange - Create gift list and item with initial quantity
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Quantity Update Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Quantity Update Test Item",
                Description = "Item to test quantity changes",
                GiftListId = giftList!.Id.ToString(),
                Quantity = 2 // Initial quantity
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Add a reservation before quantity change
            var reserverUserId = "reserveruser";
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem!.Id.ToString(), reserverUserId, "Test User");

            // Update the quantity to a higher value
            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Quantity Update Test Item",
                Description = "Item to test quantity changes",
                GiftListId = giftList.Id.ToString(),
                Quantity = 5 // New quantity (higher than original)
            };

            // Act
            var result = await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), updateRequest, ownerUserId);

            // Assert - Quantity should be updated but reservations preserved
            Assert.NotNull(result);
            Assert.Equal(5, result.Quantity); // Quantity should be updated
            Assert.Single(result.Reservations); // Reservation should still exist
            Assert.Equal(reserverUserId, result.Reservations[0].UserId);
            Assert.Equal(1, result.Reservations[0].Quantity); // Reservation quantity should be unchanged

            // Verify the change persisted in storage
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId);
            Assert.NotNull(retrieved);
            Assert.Equal(5, retrieved.Quantity); // Verify persistence
            Assert.Single(retrieved.Reservations); // Verify reservations persisted
        }

        [Fact]
        public async Task UpdateGiftItemAsync_QuantityCanExceedReservations()
        {
            // Arrange - Create gift list and item with initial quantity
            var giftListRequest = new CreateGiftListRequest
            {
                Name = _testPrefix + "_Quantity Exceed Test Gift List",
                EventName = _testPrefix + "_TestEvent"
            };
            var ownerUserId = "owneruser";
            var giftList = await _giftListService.CreateGiftListAsync(giftListRequest, ownerUserId, "Test User");

            var itemRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Quantity Exceed Test Item",
                Description = "Item to test quantity that can be exceeded by reservations",
                GiftListId = giftList!.Id.ToString(),
                Quantity = 3 // Initial quantity
            };
            var createdItem = await _giftItemService.CreateGiftItemAsync(_testPrefix + "_TestEvent", itemRequest, ownerUserId);

            // Add multiple reservations that would exceed the original quantity
            var reserverUserId1 = "reserveruser1";
            var reserverUserId2 = "reserveruser2";
            
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem!.Id.ToString(), reserverUserId1, "Test User 1");
            await _giftItemService.ReserveGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId2, "Test User 2");

            // Now update quantity to lower than total reservations (2 reservations > 1 new quantity)
            var updateRequest = new CreateGiftItemRequest
            {
                Name = _testPrefix + "_Quantity Exceed Test Item",
                Description = "Item to test quantity that can be exceeded by reservations",
                GiftListId = giftList.Id.ToString(),
                Quantity = 1 // New quantity (less than total reservations)
            };

            // Act
            var result = await _giftItemService.UpdateGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), updateRequest, ownerUserId);

            // Assert - Quantity should be updated even though reservations exceed it
            Assert.NotNull(result);
            Assert.Equal(1, result.Quantity); // Quantity should be updated
            Assert.Equal(2, result.Reservations.Count); // Both reservations should be preserved
            Assert.Equal(2, result.TotalReserved); // Total reserved should exceed available quantity
            Assert.True(result.IsFullyReserved); // Should be marked as fully reserved
            
            // Verify the change persisted in storage
            var retrieved = await _giftItemService.GetGiftItemAsync(_testPrefix + "_TestEvent", giftList.Id.ToString(), createdItem.Id.ToString(), reserverUserId1);
            Assert.NotNull(retrieved);
            Assert.Equal(1, retrieved.Quantity); // Verify persistence
            Assert.Equal(2, retrieved.Reservations.Count); // Verify reservations persisted
        }
    }
}
