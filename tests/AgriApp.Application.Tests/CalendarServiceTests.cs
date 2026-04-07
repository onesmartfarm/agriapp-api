using AgriApp.Application.Services;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgriApp.Application.Tests;

[TestClass]
public sealed class CalendarServiceTests
{
    private static readonly DateTime BaseDate = new(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AgriDbContext CreateContext(string dbName, ICurrentUser? user)
    {
        var options = new DbContextOptionsBuilder<AgriDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AgriDbContext(options, user);
    }

    private static ICurrentUser SuperUser() =>
        Mock.Of<ICurrentUser>(u => u.Role == Role.SuperUser && u.CenterId == (int?)null);

    private static ICurrentUser ManagerAt(int centerId) =>
        Mock.Of<ICurrentUser>(u => u.Role == Role.Manager && u.CenterId == (int?)centerId);

    private static WorkOrder MakeWorkOrder(int centerId, int implementId, int staffId, string description) =>
        new()
        {
            CenterId = centerId,
            ImplementId = implementId,
            ResponsibleUserId = staffId,
            Description = description,
            Type = WorkOrderType.RentalBooking,
            Status = WorkStatus.Scheduled,
            ScheduledStartDate = BaseDate,
            ScheduledEndDate = BaseDate.AddDays(2),
            TotalMaterialCost = 0m,
            CreatedAt = DateTime.UtcNow
        };

    [TestMethod]
    public async Task GetCenterCapacity_ReturnsOnlyUserCenterData()
    {
        // Arrange — seed with a SuperUser context (no query filter) so both centers' rows are written
        string dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateContext(dbName, SuperUser());
        seedDb.WorkOrders.Add(MakeWorkOrder(centerId: 1, implementId: 10, staffId: 1, "Center 1 — Tractor A"));
        seedDb.WorkOrders.Add(MakeWorkOrder(centerId: 2, implementId: 20, staffId: 2, "Center 2 — Harvester B"));
        seedDb.SaveChanges();

        // Act — query as a Manager who belongs to Center 1
        using var queryDb = CreateContext(dbName, ManagerAt(centerId: 1));
        var service = new CalendarService(queryDb);

        var result = await service.GetCenterCapacityAsync(
            start: BaseDate.AddDays(-1),
            end: BaseDate.AddDays(3));

        // Assert — only Center 1 row visible
        Assert.AreEqual(1, result.TotalScheduled,
            "A Center 1 Manager must see exactly 1 work order.");
        Assert.AreEqual(1, result.WorkOrders.Count,
            "WorkOrders list must contain exactly 1 entry.");

        var slot = result.WorkOrders[0];
        Assert.AreEqual(1,                     slot.CenterId);
        Assert.AreEqual(10,                    slot.ImplementId);
        Assert.AreEqual(1,                     slot.ResponsibleUserId);
        Assert.AreEqual("Center 1 — Tractor A", slot.Description);
        Assert.AreEqual("RentalBooking",        slot.Type);
        Assert.AreEqual("Scheduled",            slot.Status);
    }

    [TestMethod]
    public async Task GetCenterCapacity_SuperUser_SeesAllCenters()
    {
        string dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateContext(dbName, SuperUser());
        seedDb.WorkOrders.Add(MakeWorkOrder(centerId: 1, implementId: 10, staffId: 1, "Center 1 Order"));
        seedDb.WorkOrders.Add(MakeWorkOrder(centerId: 2, implementId: 20, staffId: 2, "Center 2 Order"));
        seedDb.WorkOrders.Add(MakeWorkOrder(centerId: 3, implementId: 30, staffId: 3, "Center 3 Order"));
        seedDb.SaveChanges();

        using var queryDb = CreateContext(dbName, SuperUser());
        var service = new CalendarService(queryDb);

        var result = await service.GetCenterCapacityAsync(
            start: BaseDate.AddDays(-1),
            end: BaseDate.AddDays(3));

        Assert.AreEqual(3, result.TotalScheduled,
            "SuperUser must see all 3 work orders across all centers.");
    }

    [TestMethod]
    public async Task GetCenterCapacity_ExcludesCancelledOrders()
    {
        string dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateContext(dbName, SuperUser());

        var active = MakeWorkOrder(centerId: 1, implementId: 10, staffId: 1, "Active");
        var cancelled = MakeWorkOrder(centerId: 1, implementId: 11, staffId: 2, "Cancelled");
        cancelled.Status = WorkStatus.Cancelled;

        seedDb.WorkOrders.AddRange(active, cancelled);
        seedDb.SaveChanges();

        using var queryDb = CreateContext(dbName, ManagerAt(centerId: 1));
        var service = new CalendarService(queryDb);

        var result = await service.GetCenterCapacityAsync(
            start: BaseDate.AddDays(-1),
            end: BaseDate.AddDays(3));

        Assert.AreEqual(1, result.TotalScheduled,
            "Cancelled work orders must be excluded from the capacity result.");
        Assert.AreEqual("Active", result.WorkOrders[0].Description);
    }

    [TestMethod]
    public async Task GetCenterCapacity_DateRangeFilter_ExcludesOutOfRangeOrders()
    {
        string dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateContext(dbName, SuperUser());

        // Order within range: March 1–3
        var inRange = MakeWorkOrder(centerId: 1, implementId: 10, staffId: 1, "In Range");

        // Order outside range: March 10–12 (query range is March 1–5)
        var outOfRange = MakeWorkOrder(centerId: 1, implementId: 11, staffId: 2, "Out of Range");
        outOfRange.ScheduledStartDate = BaseDate.AddDays(9);
        outOfRange.ScheduledEndDate   = BaseDate.AddDays(11);

        seedDb.WorkOrders.AddRange(inRange, outOfRange);
        seedDb.SaveChanges();

        using var queryDb = CreateContext(dbName, ManagerAt(centerId: 1));
        var service = new CalendarService(queryDb);

        var result = await service.GetCenterCapacityAsync(
            start: BaseDate,
            end: BaseDate.AddDays(4));

        Assert.AreEqual(1, result.TotalScheduled,
            "Only work orders overlapping the query range must be returned.");
        Assert.AreEqual("In Range", result.WorkOrders[0].Description);
    }

    [TestMethod]
    public async Task GetCenterCapacity_ReturnsCorrectRangeBounds()
    {
        string dbName = Guid.NewGuid().ToString();
        using var queryDb = CreateContext(dbName, ManagerAt(centerId: 1));
        var service = new CalendarService(queryDb);

        DateTime queryStart = BaseDate;
        DateTime queryEnd   = BaseDate.AddDays(7);

        var result = await service.GetCenterCapacityAsync(queryStart, queryEnd);

        Assert.AreEqual(queryStart, result.RangeStart,
            "RangeStart must reflect the requested query start.");
        Assert.AreEqual(queryEnd,   result.RangeEnd,
            "RangeEnd must reflect the requested query end.");
        Assert.AreEqual(0,          result.TotalScheduled);
    }

    [TestMethod]
    public async Task GetEquipmentTimeline_IncludesWorkTravelAndBreakdown_OnCorrectLanes()
    {
        string dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateContext(dbName, SuperUser());

        var center = new Center
        {
            Name = "Timeline Center",
            Location = "Here",
            CurrencySymbol = "$",
            TimeZoneId = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        seedDb.Centers.Add(center);
        seedDb.SaveChanges();

        var user = new User
        {
            Name = "Operator",
            Email = $"u_{Guid.NewGuid():N}@test.com",
            PasswordHash = "x",
            Role = Role.Staff,
            CenterId = center.Id,
            CreatedAt = DateTime.UtcNow
        };
        seedDb.Users.Add(user);
        seedDb.SaveChanges();

        var tractor = new Equipment
        {
            Name = "Timeline Tractor",
            Category = EquipmentCategory.Tractor,
            HourlyRate = 100m,
            CenterId = center.Id,
            IsImplement = false,
            CreatedAt = DateTime.UtcNow
        };
        var implement = new Equipment
        {
            Name = "Timeline Implement",
            Category = EquipmentCategory.Tractor,
            HourlyRate = 50m,
            CenterId = center.Id,
            IsImplement = true,
            CreatedAt = DateTime.UtcNow
        };
        seedDb.Equipment.AddRange(tractor, implement);
        seedDb.SaveChanges();

        var day = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var wo = new WorkOrder
        {
            CenterId = center.Id,
            CustomerId = null,
            TractorId = tractor.Id,
            ImplementId = implement.Id,
            ResponsibleUserId = user.Id,
            Description = "Field job",
            Type = WorkOrderType.RentalBooking,
            Status = WorkStatus.Scheduled,
            ScheduledStartDate = day.AddHours(10),
            ScheduledEndDate = day.AddHours(14),
            TotalMaterialCost = 0m,
            CreatedAt = DateTime.UtcNow
        };
        seedDb.WorkOrders.Add(wo);
        seedDb.SaveChanges();

        seedDb.WorkOrderTimeLogs.Add(new WorkOrderTimeLog
        {
            WorkOrderId = wo.Id,
            StartTime = day.AddHours(12),
            EndTime = day.AddHours(12).AddMinutes(30),
            LogType = WorkTimeLogType.Breakdown,
            Notes = "Hydraulic leak",
            CreatedAt = DateTime.UtcNow
        });
        seedDb.SaveChanges();

        using var queryDb = CreateContext(dbName, ManagerAt(center.Id));
        var service = new CalendarService(queryDb);

        var timeline = await service.GetEquipmentTimelineAsync(day, day.AddDays(1));

        var tRow = timeline.Equipment.Single(e => e.EquipmentId == tractor.Id);
        var iRow = timeline.Equipment.Single(e => e.EquipmentId == implement.Id);

        Assert.IsTrue(tRow.Blocks.Any(b => b.BlockType == "Work" && b.WorkOrderId == wo.Id));
        Assert.IsTrue(tRow.Blocks.Count(b => b.BlockType == "Travel") >= 1);
        Assert.IsTrue(tRow.Blocks.Any(b => b.BlockType == "Breakdown"));
        Assert.AreEqual("wo-pair-" + wo.Id, tRow.Blocks.First(b => b.BlockType == "Work").PairingKey);

        Assert.IsTrue(iRow.Blocks.Any(b => b.BlockType == "Work"));
        Assert.IsTrue(iRow.Blocks.Any(b => b.BlockType == "Breakdown"));
    }
}
