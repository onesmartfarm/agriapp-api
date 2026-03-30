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
public sealed class InvoiceServiceTests
{
    private AgriDbContext _db = null!;
    private InvoiceService _service = null!;

    private static ICurrentUser SuperUser() =>
        Mock.Of<ICurrentUser>(u =>
            u.Role == Role.SuperUser &&
            u.CenterId == (int?)null &&
            u.UserId == 0 &&
            u.Email == "test@system");

    private static ICurrentUser CenterUser(int centerId) =>
        Mock.Of<ICurrentUser>(u =>
            u.Role == Role.Manager &&
            u.CenterId == (int?)centerId &&
            u.UserId == 1 &&
            u.Email == "mgr@center");

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgriDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AgriDbContext(options, SuperUser());
        _service = new InvoiceService(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private WorkOrder CreateCompletedWorkOrder(int id, decimal materialCost = 1000m, int centerId = 1)
    {
        var wo = new WorkOrder
        {
            Id = id,
            CenterId = centerId,
            EquipmentId = 1,
            ResponsibleUserId = 1,
            InquiryId = 1,
            ScheduledStartDate = DateTime.UtcNow.AddDays(-2),
            ScheduledEndDate = DateTime.UtcNow.AddDays(-1),
            Status = WorkStatus.Completed,
            TotalMaterialCost = materialCost
        };
        _db.WorkOrders.Add(wo);
        _db.SaveChanges();
        return wo;
    }

    // ── Test 1 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GenerateInvoice_AppliesGstCorrectly()
    {
        CreateCompletedWorkOrder(id: 1, materialCost: 1000m);

        var request = new GenerateInvoiceRequest
        {
            WorkOrderId = 1, CustomerId = 1,
            DueDate = DateTime.UtcNow.AddDays(30),
            AdditionalFees = 200m
        };

        var result = await _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1);

        Assert.AreEqual(1200m,  result.BaseAmount,   "BaseAmount = materials + additionalFees");
        Assert.AreEqual(216m,   result.GstAmount,    "GST = 18% of BaseAmount");
        Assert.AreEqual(1416m,  result.TotalAmount,  "TotalAmount = BaseAmount + GST");
        Assert.AreEqual(1416m,  result.BalanceDue,   "BalanceDue should equal TotalAmount initially");
        Assert.AreEqual("Draft", result.Status);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GenerateInvoice_ThrowsForNonCompletedWorkOrder()
    {
        var wo = new WorkOrder
        {
            Id = 2, CenterId = 1, EquipmentId = 1, ResponsibleUserId = 1, InquiryId = 1,
            ScheduledStartDate = DateTime.UtcNow.AddDays(-1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1),
            Status = WorkStatus.InProgress,
            TotalMaterialCost = 500m
        };
        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync();

        var request = new GenerateInvoiceRequest
        {
            WorkOrderId = 2, CustomerId = 1,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1));

        StringAssert.Contains(ex.Message, "Completed");
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GenerateInvoice_ThrowsForMissingWorkOrder()
    {
        var request = new GenerateInvoiceRequest
        {
            WorkOrderId = 999, CustomerId = 1,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1));
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GenerateInvoice_ThrowsOnDuplicateWorkOrder()
    {
        CreateCompletedWorkOrder(id: 10, materialCost: 500m);

        var request = new GenerateInvoiceRequest
        {
            WorkOrderId = 10, CustomerId = 1,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        await _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1));

        StringAssert.Contains(ex.Message, "already been generated");
    }

    // ── Test 5 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task IssueInvoice_TransitionsDraftToIssued()
    {
        CreateCompletedWorkOrder(id: 20, materialCost: 500m);

        var request = new GenerateInvoiceRequest
        {
            WorkOrderId = 20, CustomerId = 1,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var draft = await _service.GenerateInvoiceFromWorkOrderAsync(request, centerId: 1);
        Assert.AreEqual("Draft", draft.Status);

        var issued = await _service.IssueAsync(draft.Id);

        Assert.IsNotNull(issued);
        Assert.AreEqual("Issued", issued!.Status);
    }

    // ── Test 6 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task IssueInvoice_ThrowsIfAlreadyIssued()
    {
        CreateCompletedWorkOrder(id: 21, materialCost: 500m);
        var draft = await _service.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest { WorkOrderId = 21, CustomerId = 1, DueDate = DateTime.UtcNow.AddDays(30) }, 1);

        await _service.IssueAsync(draft.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.IssueAsync(draft.Id));

        StringAssert.Contains(ex.Message, "Draft");
    }

    // ── Test 7 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task IssueInvoice_ReturnsNullForUnknownId()
    {
        var result = await _service.IssueAsync(Guid.NewGuid());
        Assert.IsNull(result);
    }

    // ── Test 8 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GetAll_ReturnsAllInvoices()
    {
        CreateCompletedWorkOrder(id: 30, materialCost: 100m);
        CreateCompletedWorkOrder(id: 31, materialCost: 200m);

        await _service.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest { WorkOrderId = 30, CustomerId = 1, DueDate = DateTime.UtcNow.AddDays(30) }, 1);
        await _service.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest { WorkOrderId = 31, CustomerId = 1, DueDate = DateTime.UtcNow.AddDays(30) }, 1);

        var all = await _service.GetAllAsync();
        Assert.AreEqual(2, all.Count);
    }

    // ── Test 9 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GetById_ReturnsCorrectInvoice()
    {
        CreateCompletedWorkOrder(id: 40, materialCost: 400m);
        var created = await _service.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest { WorkOrderId = 40, CustomerId = 1, DueDate = DateTime.UtcNow.AddDays(30) }, 1);

        var found = await _service.GetByIdAsync(created.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual(created.Id, found!.Id);
    }

    // ── Test 10 ──────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task GetById_ReturnsNullForMissingId()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.IsNull(result);
    }
}
