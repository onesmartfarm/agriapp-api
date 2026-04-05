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
public sealed class PaymentServiceTests
{
    private AgriDbContext _db = null!;
    private InvoiceService _invoiceService = null!;
    private PaymentService _paymentService = null!;

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
        SeedInvoiceLookupGraph();
        _invoiceService = new InvoiceService(_db);
        _paymentService = new PaymentService(_db);
    }

    private void SeedInvoiceLookupGraph()
    {
        _db.Centers.Add(new Center
        {
            Id = 1,
            Name = "Test Center",
            Location = "Test",
            CurrencySymbol = "₹",
            TimeZoneId = "India Standard Time",
            CreatedAt = DateTime.UtcNow
        });
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Test Customer",
            CenterId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _db.Users.Add(new User
        {
            Id = 1,
            Name = "Staff",
            Email = $"staff-{Guid.NewGuid():N}@test.com",
            PasswordHash = "x",
            Role = Role.Staff,
            CenterId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _db.Equipment.Add(new Equipment
        {
            Id = 1,
            Name = "Tractor",
            Category = EquipmentCategory.Tractor,
            HourlyRate = 100m,
            CenterId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private async Task<InvoiceResponse> CreateIssuedInvoiceAsync(
        int workOrderId, decimal materialCost = 1000m)
    {
        _db.WorkOrders.Add(new WorkOrder
        {
            Id = workOrderId, CenterId = 1, ImplementId = 1, ResponsibleUserId = 1, InquiryId = 1,
            ScheduledStartDate = DateTime.UtcNow.AddDays(-2),
            ScheduledEndDate = DateTime.UtcNow.AddDays(-1),
            Status = WorkStatus.Completed,
            TotalMaterialCost = materialCost
        });
        await _db.SaveChangesAsync();

        var invoice = await _invoiceService.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest
            {
                WorkOrderId = workOrderId, CustomerId = 1,
                DueDate = DateTime.UtcNow.AddDays(30)
            }, centerId: 1);

        return (await _invoiceService.IssueAsync(invoice.Id))!;
    }

    // ── Test 1 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_PartialPayment_SetsPartiallyPaid()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 1, materialCost: 1000m);
        // TotalAmount = 1000 * 1.18 = 1180

        var result = await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 500m,
            PaymentMethod = "UPI", TransactionReference = "UPI-001"
        }, centerId: 1);

        Assert.AreEqual("PartiallyPaid", result.InvoiceStatus);
        Assert.AreEqual(680m, result.InvoiceBalanceDue);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_FullPayment_SetsInvoicePaid()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 2, materialCost: 1000m);
        // TotalAmount = 1180

        var result = await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 1180m,
            PaymentMethod = "BankTransfer", TransactionReference = "NEFT-100"
        }, centerId: 1);

        Assert.AreEqual("Paid", result.InvoiceStatus);
        Assert.AreEqual(0m, result.InvoiceBalanceDue);
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_OverPayment_SetsInvoicePaid()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 3, materialCost: 1000m);

        var result = await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 2000m,
            PaymentMethod = "Cash", TransactionReference = "CASH-X01"
        }, centerId: 1);

        Assert.AreEqual("Paid", result.InvoiceStatus);
        // BalanceDue goes negative (overpayment tracked)
        Assert.IsTrue(result.InvoiceBalanceDue < 0);
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_DuplicateTransactionReference_Throws()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 4, materialCost: 500m);

        await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 100m,
            PaymentMethod = "UPI", TransactionReference = "DUPE-001"
        }, centerId: 1);

        var invoice2 = await CreateIssuedInvoiceAsync(workOrderId: 5, materialCost: 500m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = invoice2.Id, Amount = 100m,
                PaymentMethod = "UPI", TransactionReference = "DUPE-001"
            }, centerId: 1));

        StringAssert.Contains(ex.Message, "Duplicate");
    }

    // ── Test 5 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_AgainstPaidInvoice_Throws()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 6, materialCost: 1000m);

        await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 1180m,
            PaymentMethod = "Cash", TransactionReference = "FULL-001"
        }, centerId: 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = invoice.Id, Amount = 100m,
                PaymentMethod = "Cash", TransactionReference = "FULL-002"
            }, centerId: 1));

        StringAssert.Contains(ex.Message, "already fully paid");
    }

    // ── Test 6 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_AgainstDraftInvoice_Throws()
    {
        _db.WorkOrders.Add(new WorkOrder
        {
            Id = 7, CenterId = 1, ImplementId = 1, ResponsibleUserId = 1, InquiryId = 1,
            ScheduledStartDate = DateTime.UtcNow.AddDays(-2),
            ScheduledEndDate = DateTime.UtcNow.AddDays(-1),
            Status = WorkStatus.Completed,
            TotalMaterialCost = 500m
        });
        await _db.SaveChangesAsync();

        var draft = await _invoiceService.GenerateInvoiceFromWorkOrderAsync(
            new GenerateInvoiceRequest { WorkOrderId = 7, CustomerId = 1, DueDate = DateTime.UtcNow.AddDays(30) }, 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = draft.Id, Amount = 100m,
                PaymentMethod = "Cash", TransactionReference = "DRAFT-PAY-001"
            }, centerId: 1));

        StringAssert.Contains(ex.Message, "Draft");
    }

    // ── Test 7 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_InvalidMethod_Throws()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 8, materialCost: 500m);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = invoice.Id, Amount = 100m,
                PaymentMethod = "CryptoWallet", TransactionReference = "CRYPTO-001"
            }, centerId: 1));

        StringAssert.Contains(ex.Message, "Invalid PaymentMethod");
    }

    // ── Test 8 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_MissingInvoice_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = Guid.NewGuid(), Amount = 100m,
                PaymentMethod = "Cash", TransactionReference = "NOTFOUND-001"
            }, centerId: 1));
    }

    // ── Test 9 ───────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_ZeroAmount_Throws()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 9, materialCost: 500m);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _paymentService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = invoice.Id, Amount = 0m,
                PaymentMethod = "Cash", TransactionReference = "ZERO-001"
            }, centerId: 1));

        StringAssert.Contains(ex.Message, "greater than zero");
    }

    // ── Test 10 ──────────────────────────────────────────────────────────────
    [TestMethod]
    public async Task RecordPayment_TwoInstallments_CorrectlyTrackBalance()
    {
        var invoice = await CreateIssuedInvoiceAsync(workOrderId: 10, materialCost: 1000m);
        // TotalAmount = 1180

        await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 590m,
            PaymentMethod = "UPI", TransactionReference = "INST-01"
        }, centerId: 1);

        var second = await _paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id, Amount = 590m,
            PaymentMethod = "UPI", TransactionReference = "INST-02"
        }, centerId: 1);

        Assert.AreEqual("Paid", second.InvoiceStatus);
        Assert.AreEqual(0m, second.InvoiceBalanceDue);
    }
}
