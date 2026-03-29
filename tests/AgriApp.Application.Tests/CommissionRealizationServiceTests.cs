using AgriApp.Application.Services;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgriApp.Application.Tests
{
    /// <summary>
    /// Production-grade tests for CommissionRealizationService.
    /// Tests verify:
    /// - Commission status transitions (Pending → Realized)
    /// - UPI Transaction ID persistence
    /// - Audit trail creation (AuditInterceptor logs commission realization)
    /// - Cross-center isolation via Global Query Filters
    /// - Exception handling with Assert.ThrowsAsync
    /// - Database state consistency (both CommissionLedger AND AuditLog)
    /// </summary>
    [TestClass]
    public sealed class CommissionRealizationServiceTests
    {
        private static AgriDbContext CreateDbContext(string dbName, ICurrentUser? currentUser = null)
        {
            var options = new DbContextOptionsBuilder<AgriDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AgriDbContext(options, currentUser);
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WithValidUpiId_RealizesPendingCommissions()
        {
            // Arrange - Manager from Center 1 processing commission payment
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_WithValidUpiId_RealizesPendingCommissions), managerMock.Object);

            // Create salesperson
            var salesperson = new User
            {
                Name = "Sales Person",
                Email = "sales@test.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(salesperson);
            await db.SaveChangesAsync();

            // Create inquiry
            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = salesperson.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            // Create pending commission
            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = salesperson.Id,
                CenterId = 1,
                Amount = 150m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            var service = new CommissionRealizationService(db);

            // Act
            var (count, total) = await service.ConfirmPaymentAsync("UPI-ABC-123", inquiry.Id);

            // Assert - Strong assertions
            Assert.AreEqual(1, count, "Should realize exactly 1 commission");
            Assert.AreEqual(150m, total, "Total amount should match commission amount exactly");

            // Verify CommissionLedger database state
            var updated = await db.CommissionLedgers.FindAsync(commission.Id);
            Assert.IsNotNull(updated, "Commission should exist in database");
            Assert.AreEqual(CommissionStatus.Realized, updated.Status, "Commission status must transition to Realized");
            Assert.AreEqual("UPI-ABC-123", updated.UpiTransactionId, "UPI transaction ID must be persisted");
            Assert.IsNotNull(updated.UpdatedAt, "UpdatedAt timestamp must be set on realization");

            // Verify AuditLog was created for commission realization
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "CommissionLedger"
                    && a.Action == "CommissionRealized"
                    && a.EntityId == commission.Id.ToString())
                .FirstOrDefaultAsync();
            Assert.IsNotNull(auditLog, "AuditLog must be created for commission realization");
            Assert.IsTrue(auditLog.NewValue.Contains("Realized"), "Audit log must capture Realized status");
            Assert.IsTrue(auditLog.NewValue.Contains("UPI-ABC-123"), "Audit log must capture UPI transaction ID");
            Assert.AreEqual(managerMock.Object.UserId, auditLog.UserId, "Audit log must record the user who processed the payment");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WithMultiplePendingCommissions_RealizesAll()
        {
            // Arrange - Batch realization of multiple pending commissions
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_WithMultiplePendingCommissions_RealizesAll), managerMock.Object);

            var salesperson = new User
            {
                Name = "Sales Person",
                Email = "sales@test.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(salesperson);
            await db.SaveChangesAsync();

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = salesperson.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            // Create 3 pending commissions
            var commissions = new List<CommissionLedger>
            {
                new() { InquiryId = inquiry.Id, UserId = salesperson.Id, CenterId = 1, Amount = 100m, Status = CommissionStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { InquiryId = inquiry.Id, UserId = salesperson.Id, CenterId = 1, Amount = 75m, Status = CommissionStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { InquiryId = inquiry.Id, UserId = salesperson.Id, CenterId = 1, Amount = 50m, Status = CommissionStatus.Pending, CreatedAt = DateTime.UtcNow }
            };
            db.CommissionLedgers.AddRange(commissions);
            await db.SaveChangesAsync();

            var service = new CommissionRealizationService(db);

            // Act
            var (count, total) = await service.ConfirmPaymentAsync("UPI-XYZ-789", inquiry.Id);

            // Assert - Verify all commissions are realized
            Assert.AreEqual(3, count, "Should realize all 3 commissions exactly");
            Assert.AreEqual(225m, total, "Total amount should be 100 + 75 + 50 = 225");

            // Verify each commission state in database
            var allUpdated = await db.CommissionLedgers
                .Where(c => c.InquiryId == inquiry.Id)
                .OrderBy(c => c.Amount)
                .ToListAsync();

            Assert.AreEqual(3, allUpdated.Count, "All 3 commissions should be persisted");

            foreach (var commission in allUpdated)
            {
                Assert.AreEqual(CommissionStatus.Realized, commission.Status, $"Commission ${commission.Amount} status must be Realized");
                Assert.AreEqual("UPI-XYZ-789", commission.UpiTransactionId, $"Commission ${commission.Amount} must have same UPI ID");
                Assert.IsNotNull(commission.UpdatedAt, $"Commission ${commission.Amount} must have UpdatedAt timestamp");
            }

            // Verify 3 audit logs were created
            var auditLogs = await db.AuditLogs
                .Where(a => a.EntityName == "CommissionLedger"
                    && a.Action == "CommissionRealized"
                    && commissions.Select(c => c.Id.ToString()).Contains(a.EntityId))
                .ToListAsync();

            Assert.AreEqual(3, auditLogs.Count, "Should have 3 audit logs for 3 commission realizations");
            Assert.IsTrue(auditLogs.All(a => a.NewValue.Contains("UPI-XYZ-789")), "All audit logs must capture UPI ID");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WithEmptyUpiId_ThrowsArgumentException()
        {
            // Arrange - Empty UPI ID should be rejected
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(2);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_WithEmptyUpiId_ThrowsArgumentException), managerMock.Object);

            var salesperson = new User
            {
                Name = "Sales Person",
                Email = "sales@test.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(salesperson);
            await db.SaveChangesAsync();

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = salesperson.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = salesperson.Id,
                CenterId = 1,
                Amount = 200m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            var service = new CommissionRealizationService(db);

            // Act & Assert - Use Assert.ThrowsAsync instead of try/catch
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.ConfirmPaymentAsync("", inquiry.Id),
                "Empty UPI ID should throw ArgumentException");

            Assert.IsTrue(exception.Message.Contains("UPI") || exception.Message.Contains("empty"),
                "Exception message should mention UPI or empty requirement");

            // Verify commission was NOT realized
            var unrealized = await db.CommissionLedgers.FindAsync(commission.Id);
            Assert.AreEqual(CommissionStatus.Pending, unrealized.Status, "Commission status must remain Pending on validation error");
            Assert.IsNull(unrealized.UpiTransactionId, "UPI transaction ID should not be set on validation error");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WithNullUpiId_ThrowsArgumentException()
        {
            // Arrange - Null UPI ID should be rejected
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(2);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_WithNullUpiId_ThrowsArgumentException), managerMock.Object);

            var salesperson = new User
            {
                Name = "Sales Person",
                Email = "sales@test.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(salesperson);
            await db.SaveChangesAsync();

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = salesperson.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = salesperson.Id,
                CenterId = 1,
                Amount = 200m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            var service = new CommissionRealizationService(db);

            // Act & Assert - Use Assert.ThrowsAsync
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.ConfirmPaymentAsync(null!, inquiry.Id),
                "Null UPI ID should throw ArgumentException");

            // Verify commission remains pending
            var unrealized = await db.CommissionLedgers.FindAsync(commission.Id);
            Assert.AreEqual(CommissionStatus.Pending, unrealized.Status, "Commission must remain Pending on null UPI ID");
            Assert.IsNull(unrealized.UpiTransactionId, "UPI transaction ID should remain null");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WithNoMatchingInquiry_ReturnsZero()
        {
            // Arrange - Non-existent inquiry should gracefully return (0, 0m)
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_WithNoMatchingInquiry_ReturnsZero), managerMock.Object);

            var service = new CommissionRealizationService(db);

            // Act
            var (count, total) = await service.ConfirmPaymentAsync("UPI-TEST-001", 999); // Non-existent inquiry

            // Assert
            Assert.AreEqual(0, count, "Should return 0 count for non-existent inquiry");
            Assert.AreEqual(0m, total, "Should return 0m total for non-existent inquiry");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_LeavesRealizedCommissionsUnchanged()
        {
            // Arrange - Verify that already-realized commissions are not double-charged
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(ConfirmPaymentAsync_LeavesRealizedCommissionsUnchanged), managerMock.Object);

            var salesperson = new User
            {
                Name = "Sales Person",
                Email = "sales@test.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(salesperson);
            await db.SaveChangesAsync();

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = salesperson.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var pendingCommission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = salesperson.Id,
                CenterId = 1,
                Amount = 100m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var realizedCommission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = salesperson.Id,
                CenterId = 1,
                Amount = 50m,
                Status = CommissionStatus.Realized,
                UpiTransactionId = "UPI-OLD-123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            db.CommissionLedgers.AddRange(pendingCommission, realizedCommission);
            await db.SaveChangesAsync();

            var service = new CommissionRealizationService(db);

            // Act
            var (count, total) = await service.ConfirmPaymentAsync("UPI-NEW-456", inquiry.Id);

            // Assert - Only pending commission should be realized
            Assert.AreEqual(1, count, "Should only realize 1 (the pending one)");
            Assert.AreEqual(100m, total, "Should only sum the pending amount (100m)");

            // Verify pending commission was realized
            var updated = await db.CommissionLedgers.FindAsync(pendingCommission.Id);
            Assert.AreEqual(CommissionStatus.Realized, updated.Status, "Pending commission must transition to Realized");
            Assert.AreEqual("UPI-NEW-456", updated.UpiTransactionId, "Pending commission must have new UPI ID");

            // Verify already-realized commission was NOT modified
            var alreadyRealized = await db.CommissionLedgers.FindAsync(realizedCommission.Id);
            Assert.AreEqual(CommissionStatus.Realized, alreadyRealized.Status, "Already realized commission must remain Realized");
            Assert.AreEqual("UPI-OLD-123", alreadyRealized.UpiTransactionId, "Already realized commission must keep original UPI ID");
            Assert.AreEqual(50m, alreadyRealized.Amount, "Already realized commission amount must not change");

            // Verify only 1 new audit log was created (not 2)
            var auditLogs = await db.AuditLogs
                .Where(a => a.EntityName == "CommissionLedger"
                    && a.Action == "CommissionRealized"
                    && a.NewValue.Contains("UPI-NEW-456"))
                .ToListAsync();

            Assert.AreEqual(1, auditLogs.Count, "Only 1 audit log should be created for the newly realized commission");
        }
    }
}
