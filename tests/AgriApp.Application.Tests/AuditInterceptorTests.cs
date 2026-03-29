using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using AgriApp.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgriApp.Application.Tests
{
    [TestClass]
    public sealed class AuditInterceptorTests
    {
        private static AgriDbContext CreateDbContextWithInterceptor(string dbName, ICurrentUser? currentUser = null)
        {
            var interceptor = new AuditInterceptor(currentUser);
            var options = new DbContextOptionsBuilder<AgriDbContext>()
                .UseInMemoryDatabase(dbName)
                .AddInterceptors(interceptor)
                .Options;

            return new AgriDbContext(options, currentUser);
        }

        [TestMethod]
        public async Task ProcessAudits_LogsCommissionRealizedEvent()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_LogsCommissionRealizedEvent), currentUserMock.Object);

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = 1,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = 1,
                CenterId = 1,
                Amount = 1000m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            // Act - Update commission to Realized
            var toUpdate = db.CommissionLedgers.First();
            toUpdate.Status = CommissionStatus.Realized;
            toUpdate.UpiTransactionId = "UPI-TEST-123";
            toUpdate.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Assert - Check audit log
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "CommissionLedger" && a.Action == "CommissionRealized")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog, "Audit log should be created for commission realization");
            Assert.AreEqual(1, auditLog.UserId, "Audit should record the actor (current user)");
            Assert.IsTrue(auditLog.NewValue.Contains("Realized"), "NewValue should contain Realized status");
            Assert.IsTrue(auditLog.NewValue.Contains("UPI-TEST-123"), "NewValue should contain UPI transaction ID");
        }

        [TestMethod]
        public async Task ProcessAudits_LogsEntityCreation()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_LogsEntityCreation), currentUserMock.Object);

            var user = new User
            {
                Name = "New User",
                Email = "newuser@test.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "User" && a.Action == "Created")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog, "Audit log should be created for user creation");
            Assert.AreEqual(1, auditLog.UserId);
            Assert.IsNull(auditLog.OldValue, "OldValue should be null for Created action");
            Assert.IsNotNull(auditLog.NewValue);
            Assert.IsTrue(auditLog.NewValue.Contains("New User"), "NewValue should contain user details");
        }

        [TestMethod]
        public async Task ProcessAudits_LogsEntityModification()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(2);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_LogsEntityModification), currentUserMock.Object);

            var user = new User
            {
                Name = "Original Name",
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Clear audit logs from creation
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            // Act - Modify user
            var toUpdate = db.Users.First();
            toUpdate.Name = "Updated Name";
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "User" && a.Action == "Updated")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog, "Audit log should be created for update");
            Assert.AreEqual(2, auditLog.UserId);
            Assert.IsNotNull(auditLog.OldValue);
            Assert.IsNotNull(auditLog.NewValue);
            Assert.IsTrue(auditLog.NewValue.Contains("Updated Name"), "NewValue should contain updated name");
        }

        [TestMethod]
        public async Task ProcessAudits_LogsEntityDeletion()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_LogsEntityDeletion), currentUserMock.Object);

            var center = new Center
            {
                Name = "Test Center",
                Location = "Test Location",
                CreatedAt = DateTime.UtcNow
            };
            db.Centers.Add(center);
            await db.SaveChangesAsync();

            var centerId = center.Id;

            // Clear previous audits
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            // Act - Delete center
            var toDelete = db.Centers.First();
            db.Centers.Remove(toDelete);
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "Center" && a.Action == "Deleted")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog, "Audit log should be created for deletion");
            Assert.AreEqual(centerId.ToString(), auditLog.EntityId);
            Assert.IsNotNull(auditLog.OldValue, "OldValue should contain deleted entity details");
            Assert.IsNull(auditLog.NewValue, "NewValue should be null for Deleted action");
        }

        [TestMethod]
        public async Task ProcessAudits_ExcludesPasswordFromAuditLog()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_ExcludesPasswordFromAuditLog), currentUserMock.Object);

            var user = new User
            {
                Name = "Security Test User",
                Email = "secure@test.com",
                PasswordHash = "SUPER_SECRET_HASH_123",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "User" && a.Action == "Created")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog);
            Assert.IsFalse(auditLog.NewValue.Contains("SUPER_SECRET_HASH_123"), "PasswordHash should not be included in audit log");
            Assert.IsFalse(auditLog.NewValue.Contains("PasswordHash"), "PasswordHash key should not appear in audit log");
        }

        [TestMethod]
        public async Task ProcessAudits_SingleTransactionForMultipleChanges()
        {
            // Arrange
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_SingleTransactionForMultipleChanges), currentUserMock.Object);

            var user1 = new User { Name = "User1", Email = "user1@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow };
            var user2 = new User { Name = "User2", Email = "user2@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow };

            // Act - Add multiple entities in single SaveChanges
            db.Users.Add(user1);
            db.Users.Add(user2);
            await db.SaveChangesAsync();

            // Assert - Both should be audited in same transaction
            var auditLogs = await db.AuditLogs
                .Where(a => a.EntityName == "User" && a.Action == "Created")
                .ToListAsync();

            Assert.AreEqual(2, auditLogs.Count, "Both users should have audit logs");
            Assert.IsTrue(auditLogs.All(a => a.Timestamp != null), "All audit logs should have timestamps");
        }

        [TestMethod]
        public async Task ProcessAudits_CommissionRealizedDoesNotCreateGenericModifiedAudit()
        {
            // Arrange - Verify that commission realization creates ONLY CommissionRealized audit, not generic Modified
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_CommissionRealizedDoesNotCreateGenericModifiedAudit), currentUserMock.Object);

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = 1,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = 1,
                CenterId = 1,
                Amount = 500m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            // Clear initial audits
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            // Act - Update commission to Realized
            var toUpdate = db.CommissionLedgers.First();
            toUpdate.Status = CommissionStatus.Realized;
            toUpdate.UpiTransactionId = "UPI-SPECIFIC-001";
            toUpdate.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Assert - Should have ONLY CommissionRealized audit, NOT generic "Updated"
            var allAudits = await db.AuditLogs.ToListAsync();
            var commissionAudits = allAudits.Where(a => a.EntityName == "CommissionLedger").ToList();

            Assert.AreEqual(1, commissionAudits.Count, "Should have exactly 1 audit for commission (CommissionRealized, not Updated)");
            Assert.AreEqual("CommissionRealized", commissionAudits[0].Action, "Should be CommissionRealized, not Updated");
        }

        [TestMethod]
        public async Task ProcessAudits_WithNullCurrentUser_DefaultsToUserIdZero()
        {
            // Arrange - Verify that null ICurrentUser defaults to UserId 0
            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_WithNullCurrentUser_DefaultsToUserIdZero), currentUser: null);

            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "User" && a.Action == "Created")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog);
            Assert.AreEqual(0, auditLog.UserId, "When ICurrentUser is null, UserId should default to 0");
        }

        [TestMethod]
        public async Task ProcessAudits_AuditLogEntitiesExcludedFromAuditing()
        {
            // Arrange - Verify that AuditLog entities themselves are NOT audited
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_AuditLogEntitiesExcludedFromAuditing), currentUserMock.Object);

            var user = new User
            {
                Name = "Test",
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Assert - Should have 1 audit log for user creation, but the audit log itself should NOT be audited
            var auditLogs = await db.AuditLogs.ToListAsync();
            Assert.AreEqual(1, auditLogs.Count, "Should have 1 audit log entry");

            // Count how many times "AuditLog" appears as EntityName - should be 0 (interceptor excludes AuditLog from auditing)
            var auditLogCount = auditLogs.Count(a => a.EntityName == "AuditLog");
            Assert.AreEqual(0, auditLogCount, "AuditLog entities should not be audited themselves");
        }

        [TestMethod]
        public async Task ProcessAudits_ComplexTransactionWithMixedOperations()
        {
            // Arrange - Create, Read, Update, and Delete in one complex scenario
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_ComplexTransactionWithMixedOperations), currentUserMock.Object);

            // Create users
            var user1 = new User { Name = "User1", Email = "user1@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow };
            var user2 = new User { Name = "User2", Email = "user2@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow };
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();

            var createdAudits = await db.AuditLogs.CountAsync(a => a.Action == "Created");
            Assert.AreEqual(2, createdAudits, "Should have 2 Created audits");

            // Update user1
            var toUpdate = db.Users.First(u => u.Name == "User1");
            toUpdate.Name = "UpdatedUser1";
            await db.SaveChangesAsync();

            var updatedAudits = await db.AuditLogs.CountAsync(a => a.Action == "Updated");
            Assert.AreEqual(1, updatedAudits, "Should have 1 Updated audit");

            // Delete user2
            var toDelete = db.Users.First(u => u.Name == "User2");
            db.Users.Remove(toDelete);
            await db.SaveChangesAsync();

            // Assert
            var deletedAudits = await db.AuditLogs.CountAsync(a => a.Action == "Deleted");
            Assert.AreEqual(1, deletedAudits, "Should have 1 Deleted audit");

            var totalAudits = await db.AuditLogs.CountAsync();
            Assert.AreEqual(4, totalAudits, "Should have 4 total audits (2 Created + 1 Updated + 1 Deleted)");
        }

        [TestMethod]
        public async Task ProcessAudits_CapturesEntityIdForAllOperations()
        {
            // Arrange - Verify EntityId is captured for Create, Update, Delete
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_CapturesEntityIdForAllOperations), currentUserMock.Object);

            var user = new User
            {
                Name = "EntityIdTest",
                Email = "entityid@test.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userId = user.Id;

            // Clear and update
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            var toUpdate = db.Users.First();
            toUpdate.Name = "Updated";
            await db.SaveChangesAsync();

            // Assert - Updated audit should have EntityId
            var updatedAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "Updated");
            Assert.IsNotNull(updatedAudit);
            Assert.AreEqual(userId.ToString(), updatedAudit.EntityId, "EntityId should be captured for Updated operation");

            // Clear and delete
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            var toDelete = db.Users.First();
            db.Users.Remove(toDelete);
            await db.SaveChangesAsync();

            // Assert - Deleted audit should have EntityId
            var deletedAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "Deleted");
            Assert.IsNotNull(deletedAudit);
            Assert.AreEqual(userId.ToString(), deletedAudit.EntityId, "EntityId should be captured for Deleted operation");
        }

        [TestMethod]
        public async Task ProcessAudits_SerializationIncludesAllNonPasswordFields()
        {
            // Arrange - Verify that serialized data includes all fields except Password
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_SerializationIncludesAllNonPasswordFields), currentUserMock.Object);

            var equipment = new Equipment
            {
                Name = "Test Equipment",
                Category = EquipmentCategory.Tractor,
                HourlyRate = 1500.50m,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            db.Equipment.Add(equipment);
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "Equipment" && a.Action == "Created")
                .FirstOrDefaultAsync();

            Assert.IsNotNull(auditLog);
            Assert.IsNotNull(auditLog.NewValue);
            Assert.IsTrue(auditLog.NewValue.Contains("Test Equipment"), "Should contain Name");
            Assert.IsTrue(auditLog.NewValue.Contains("Tractor"), "Should contain Category");
            Assert.IsTrue(auditLog.NewValue.Contains("1500.50"), "Should contain HourlyRate");
        }

        [TestMethod]
        public async Task ProcessAudits_CommissionRealizationCapturesUpiTransactionId()
        {
            // Arrange - Verify that commission realization specifically captures UPI transaction ID
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_CommissionRealizationCapturesUpiTransactionId), currentUserMock.Object);

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = 1,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = 1,
                CenterId = 1,
                Amount = 750m,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            // Clear audits
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            // Act - Realize with specific UPI
            var toRealize = db.CommissionLedgers.First();
            toRealize.Status = CommissionStatus.Realized;
            toRealize.UpiTransactionId = "UPI-SPECIAL-XYZ-9876";
            toRealize.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .FirstOrDefaultAsync(a => a.EntityName == "CommissionLedger" && a.Action == "CommissionRealized");

            Assert.IsNotNull(auditLog);
            Assert.IsTrue(auditLog.NewValue.Contains("UPI-SPECIAL-XYZ-9876"), "Audit should capture the specific UPI transaction ID");
            Assert.IsTrue(auditLog.NewValue.Contains("Status=Realized"), "Audit should show status change to Realized");
        }

        [TestMethod]
        public async Task ProcessAudits_OldValueCapturedForModifications()
        {
            // Arrange - Verify that OldValue is captured for modifications
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.UserId).Returns(1);
            currentUserMock.Setup(x => x.CenterId).Returns(1);

            var db = CreateDbContextWithInterceptor(nameof(ProcessAudits_OldValueCapturedForModifications), currentUserMock.Object);

            var center = new Center
            {
                Name = "Original Name",
                Location = "Original Location",
                CreatedAt = DateTime.UtcNow
            };
            db.Centers.Add(center);
            await db.SaveChangesAsync();

            // Clear audits
            db.AuditLogs.RemoveRange(db.AuditLogs);
            await db.SaveChangesAsync();

            // Act
            var toUpdate = db.Centers.First();
            toUpdate.Name = "Updated Name";
            toUpdate.Location = "Updated Location";
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .FirstOrDefaultAsync(a => a.EntityName == "Center" && a.Action == "Updated");

            Assert.IsNotNull(auditLog);
            Assert.IsNotNull(auditLog.OldValue, "OldValue should be captured");
            Assert.IsNotNull(auditLog.NewValue, "NewValue should be captured");
            Assert.IsTrue(auditLog.OldValue.Contains("Original Name"), "OldValue should contain original data");
            Assert.IsTrue(auditLog.NewValue.Contains("Updated Name"), "NewValue should contain updated data");
        }
    }
}
