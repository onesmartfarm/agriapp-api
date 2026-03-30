using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgriApp.Application.Tests;

[TestClass]
public sealed class WorkOrderServiceTests
{
    private AgriDbContext _db = null!;
    private WorkOrderService _service = null!;

    private static readonly DateTime Monday    = new(2025, 1, 6,  0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Tuesday   = new(2025, 1, 7,  0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Wednesday = new(2025, 1, 8,  0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Thursday  = new(2025, 1, 9,  0, 0, 0, DateTimeKind.Utc);

    // EF Core InMemory evaluates Global Query Filters as real LINQ (no SQL null short-circuit).
    // Passing null as currentUser causes NullReferenceException when the OR expression reaches
    // _currentUser.Role. Use a SuperUser mock instead so the filter resolves to true without
    // ever evaluating _currentUser.CenterId.
    private static ICurrentUser SuperUser() =>
        Mock.Of<ICurrentUser>(u =>
            u.Role == Role.SuperUser &&
            u.CenterId == (int?)null &&
            u.UserId == 0 &&
            u.Email == "test@system");

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgriDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AgriDbContext(options, SuperUser());
        _service = new WorkOrderService(_db);
    }

    [TestCleanup]
    public void Teardown() => _db.Dispose();

    private WorkOrder SeedWorkOrder(
        int centerId,
        int? equipmentId,
        int responsibleUserId,
        DateTime start,
        DateTime end,
        WorkStatus status = WorkStatus.Scheduled)
    {
        var wo = new WorkOrder
        {
            CenterId = centerId,
            EquipmentId = equipmentId,
            ResponsibleUserId = responsibleUserId,
            Description = "Seeded order",
            Type = WorkOrderType.RentalBooking,
            Status = status,
            ScheduledStartDate = start,
            ScheduledEndDate = end,
            TotalMaterialCost = 0m,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOrders.Add(wo);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return wo;
    }

    [TestMethod]
    public async Task CreateWorkOrder_WithValidDates_SavesSuccessfully()
    {
        var request = new CreateWorkOrderRequest
        {
            ResponsibleUserId = 1,
            EquipmentId = 10,
            Description = "Harvest combine rental",
            Type = "RentalBooking",
            ScheduledStartDate = Monday,
            ScheduledEndDate = Wednesday,
            TotalMaterialCost = 1500.75m
        };

        var result = await _service.CreateWorkOrderAsync(request, centerId: 1);

        Assert.AreEqual(1,           result.CenterId);
        Assert.AreEqual("RentalBooking", result.Type);
        Assert.AreEqual("Scheduled", result.Status);
        Assert.AreEqual(1500.75m,    result.TotalMaterialCost);
        Assert.AreEqual(Monday,      result.ScheduledStartDate);
        Assert.AreEqual(Wednesday,   result.ScheduledEndDate);
        Assert.IsNull(result.ActualStartDate);
        Assert.IsNull(result.ActualEndDate);

        int savedCount = _db.WorkOrders.IgnoreQueryFilters().Count();
        Assert.AreEqual(1, savedCount, "Exactly one WorkOrder must be persisted.");
    }

    [TestMethod]
    public async Task CreateWorkOrder_EquipmentDoubleBooking_ThrowsInvalidOperationException()
    {
        // Equipment 1 is already booked Mon → Wed by a different staff member
        SeedWorkOrder(centerId: 1, equipmentId: 1, responsibleUserId: 99,
                      start: Monday, end: Wednesday);

        var request = new CreateWorkOrderRequest
        {
            ResponsibleUserId = 2,   // different staff — but same equipment
            EquipmentId = 1,         // same equipment
            Description = "Overlapping rental attempt",
            Type = "RentalBooking",
            ScheduledStartDate = Tuesday,    // Tue → Thu overlaps Mon → Wed
            ScheduledEndDate = Thursday,
            TotalMaterialCost = 0m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateWorkOrderAsync(request, centerId: 1));
    }

    [TestMethod]
    public async Task CreateWorkOrder_StaffDoubleBooking_ThrowsInvalidOperationException()
    {
        // Staff 5 is busy Mon → Wed (no equipment — InternalProject)
        SeedWorkOrder(centerId: 1, equipmentId: null, responsibleUserId: 5,
                      start: Monday, end: Wednesday);

        var request = new CreateWorkOrderRequest
        {
            ResponsibleUserId = 5,   // same staff
            EquipmentId = null,      // no equipment
            Description = "Staff overlap attempt",
            Type = "InternalProject",
            ScheduledStartDate = Tuesday,    // Tue → Thu overlaps Mon → Wed
            ScheduledEndDate = Thursday,
            TotalMaterialCost = 0m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateWorkOrderAsync(request, centerId: 1));
    }

    [TestMethod]
    public async Task CreateWorkOrder_OverlappingCancelledOrder_SavesSuccessfully()
    {
        // An overlapping order exists but is CANCELLED — must be ignored by the firewall
        SeedWorkOrder(centerId: 1, equipmentId: 1, responsibleUserId: 5,
                      start: Monday, end: Wednesday, status: WorkStatus.Cancelled);

        var request = new CreateWorkOrderRequest
        {
            ResponsibleUserId = 5,
            EquipmentId = 1,          // same equipment + same dates as cancelled order
            Description = "Should succeed past cancelled order",
            Type = "RentalBooking",
            ScheduledStartDate = Monday,
            ScheduledEndDate = Wednesday,
            TotalMaterialCost = 250.00m
        };

        var result = await _service.CreateWorkOrderAsync(request, centerId: 1);

        Assert.AreEqual("Scheduled", result.Status);
        Assert.AreEqual(250.00m,     result.TotalMaterialCost);

        int totalRows = _db.WorkOrders.IgnoreQueryFilters().Count();
        Assert.AreEqual(2, totalRows, "DB must contain the cancelled seed + new active order.");
    }

    [TestMethod]
    public async Task CreateWorkOrder_BackToBackBooking_SavesSuccessfully()
    {
        // Equipment 1 finishes exactly at Wednesday 00:00
        SeedWorkOrder(centerId: 1, equipmentId: 1, responsibleUserId: 10,
                      start: Monday, end: Wednesday);

        // Next booking starts exactly at Wednesday 00:00 — overlap formula: Start < End && End > Start
        // Monday < Wednesday && Wednesday > Wednesday → false, so NO conflict
        var request = new CreateWorkOrderRequest
        {
            ResponsibleUserId = 11,
            EquipmentId = 1,
            Description = "Back-to-back booking",
            Type = "RentalBooking",
            ScheduledStartDate = Wednesday,   // starts exactly when previous ends
            ScheduledEndDate = Thursday,
            TotalMaterialCost = 0m
        };

        var result = await _service.CreateWorkOrderAsync(request, centerId: 1);

        Assert.AreEqual("Scheduled", result.Status,
            "Back-to-back bookings (touching but not overlapping) must succeed.");
    }

    [TestMethod]
    public async Task UpdateStatus_ToInProgress_SetsActualStartDate()
    {
        var seeded = SeedWorkOrder(centerId: 1, equipmentId: 1, responsibleUserId: 7,
                                   start: Monday, end: Wednesday);

        var result = await _service.UpdateStatusAsync(seeded.Id, WorkStatus.InProgress);

        Assert.IsNotNull(result);
        Assert.AreEqual("InProgress", result!.Status);
        Assert.IsNotNull(result.ActualStartDate, "ActualStartDate must be stamped on InProgress transition.");
        Assert.IsNull(result.ActualEndDate,      "ActualEndDate must still be null until Completed.");
    }

    [TestMethod]
    public async Task UpdateStatus_ToCompleted_SetsActualEndDate()
    {
        var seeded = SeedWorkOrder(centerId: 1, equipmentId: 1, responsibleUserId: 8,
                                   start: Monday, end: Wednesday);

        await _service.UpdateStatusAsync(seeded.Id, WorkStatus.InProgress);
        var result = await _service.UpdateStatusAsync(seeded.Id, WorkStatus.Completed);

        Assert.IsNotNull(result);
        Assert.AreEqual("Completed", result!.Status);
        Assert.IsNotNull(result.ActualStartDate, "ActualStartDate must remain set.");
        Assert.IsNotNull(result.ActualEndDate,   "ActualEndDate must be stamped on Completed transition.");
    }
}
