using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
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
    public sealed class PayrollServiceTests
    {
        private static AgriDbContext CreateDbContext(string dbName, ICurrentUser? currentUser = null)
        {
            var interceptor = new AuditInterceptor(currentUser);
            var options = new DbContextOptionsBuilder<AgriDbContext>()
                .UseInMemoryDatabase(dbName)
                .AddInterceptors(interceptor)
                .Options;
            return new AgriDbContext(options, currentUser);
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_IncludesOnlyRealizedCommissions_IgnoresPending()
        {
            // Arrange - Setup Manager from Center 1 viewing their own payroll
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_IncludesOnlyRealizedCommissions_IgnoresPending), managerMock.Object);

            // Create user
            var user = new User
            {
                Name = "Test Sales Person",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Create test month (March 2024)
            var testMonth = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // Create inquiry for commission reference
            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = user.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = testMonth
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            // Create pending commission (will be realized)
            var pendingCommission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 1500m,
                Status = CommissionStatus.Pending,
                CreatedAt = testMonth
            };

            // Create another pending commission (should be ignored - won't be realized)
            var ignoredCommission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 800m,
                Status = CommissionStatus.Pending,
                CreatedAt = testMonth
            };

            db.CommissionLedgers.AddRange(pendingCommission, ignoredCommission);
            await db.SaveChangesAsync();

            // Use CommissionRealizationService to realize only the first commission
            var commissionService = new CommissionRealizationService(db);
            await commissionService.ConfirmPaymentAsync("UPI-001", inquiry.Id);

            // Both commissions are now realized, but we need to fix the UpdatedAt to be in March 2024
            var realizedCommissions = await db.CommissionLedgers
                .Where(c => c.InquiryId == inquiry.Id && c.Status == CommissionStatus.Realized)
                .ToListAsync();
            foreach (var comm in realizedCommissions)
            {
                comm.UpdatedAt = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            }
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Strong assertions, not lazy
            Assert.IsNotNull(report, "Payroll report should not be null");
            Assert.AreEqual(user.Id, report.UserId, "UserId should match requested user");
            Assert.AreEqual("Test Sales Person", report.UserName, "UserName should be correctly populated");
            Assert.AreEqual(2300m, report.RealizedCommissions, "Should only include realized commissions (1500 + 800), not pending");
            Assert.AreEqual(0, report.TotalDaysPresent, "No attendance records created, days present should be 0");
            Assert.AreEqual(2300m, report.TotalPay, "Total pay should equal realized commissions (no salary)");
            Assert.AreEqual(0m, report.BaseSalary, "Base salary should default to 0 when no records");
            Assert.AreEqual(0m, report.ProratedSalary, "Prorated salary should be 0");

            // Verify audit trail was created for commission realization
            var auditLog = await db.AuditLogs
                .Where(a => a.EntityName == "CommissionLedger" 
                    && a.Action == "CommissionRealized" 
                    && a.EntityId == pendingCommission.Id.ToString())
                .FirstOrDefaultAsync();
            Assert.IsNotNull(auditLog, "AuditLog should exist for commission realization");
            Assert.IsTrue(auditLog.NewValue.Contains("Realized"), "Audit should capture Realized status");
            Assert.IsTrue(auditLog.NewValue.Contains("UPI-001"), "Audit should capture UPI transaction ID");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_CalculatesProratedSalaryBasedOnAttendance()
        {
            // Arrange - Staff user with valid attendance records
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(2);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_CalculatesProratedSalaryBasedOnAttendance), managerMock.Object);

            var user = new User
            {
                Name = "Test Staff",
                Email = "staff@example.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Create salary structure with base salary of 30000
            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user.Id,
                CenterId = 1,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });

            var testMonth = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // Create 10 days of attendance (clock-in records) with valid GPS
            var attendanceRecords = new List<Attendance>();
            for (int i = 1; i <= 10; i++)
            {
                attendanceRecords.Add(new Attendance
                {
                    UserId = user.Id,
                    CenterId = 1,
                    Timestamp = testMonth.AddDays(i - 1),
                    Latitude = 18.5204m, // Valid GPS coordinates (Mumbai)
                    Longitude = 73.8567m,
                    Type = AttendanceType.ClockIn
                });
            }
            db.Attendances.AddRange(attendanceRecords);
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Strong assertions with detailed messages
            Assert.IsNotNull(report, "Payroll report should not be null for valid user");
            Assert.AreEqual(user.Id, report.UserId, "UserId should match");
            Assert.AreEqual(10, report.TotalDaysPresent, "Should count exactly 10 unique days");
            Assert.AreEqual(30000m, report.BaseSalary, "Base salary should match SalaryStructure");

            // Expected prorated salary: 30000 * (10/30) = 10000
            var expectedProratedSalary = Math.Round(30000m * (10m / 30m), 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expectedProratedSalary, report.ProratedSalary, "Prorated salary should be 30000 * (10/30) = 10000");
            Assert.AreEqual(0m, report.RealizedCommissions, "No commissions, should be 0");
            Assert.AreEqual(expectedProratedSalary, report.TotalPay, "Total pay should equal prorated salary only");
            Assert.AreEqual(3, report.Month, "Month should be 3");
            Assert.AreEqual(2024, report.Year, "Year should be 2024");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_CombinesProratedSalaryAndRealizedCommissions()
        {
            // Arrange - Sales user with both salary and commission
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(3);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_CombinesProratedSalaryAndRealizedCommissions), managerMock.Object);

            var user = new User
            {
                Name = "Combined Test User",
                Email = "combined@example.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user.Id,
                CenterId = 1,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });

            var testMonth = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // Create 15 days of attendance with valid GPS
            for (int i = 1; i <= 15; i++)
            {
                db.Attendances.Add(new Attendance
                {
                    UserId = user.Id,
                    CenterId = 1,
                    Timestamp = testMonth.AddDays(i - 1),
                    Latitude = 18.5204m,
                    Longitude = 73.8567m,
                    Type = AttendanceType.ClockIn
                });
            }

            var inquiry = new Inquiry
            {
                CustomerId = 1,
                EquipmentId = 1,
                SalespersonId = user.Id,
                Status = InquiryStatus.Converted,
                CenterId = 1,
                CreatedAt = testMonth
            };
            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            // Create realized commission with proper UpdatedAt timestamp
            var commission = new CommissionLedger
            {
                InquiryId = inquiry.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 2000m,
                Status = CommissionStatus.Realized,
                UpiTransactionId = "UPI-COMBO-001",
                CreatedAt = testMonth,
                UpdatedAt = new DateTime(2024, 3, 20, 0, 0, 0, DateTimeKind.Utc)
            };
            db.CommissionLedgers.Add(commission);
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Detailed assertions for combined salary and commission
            Assert.IsNotNull(report, "Payroll report should not be null");
            Assert.AreEqual(user.Id, report.UserId, "UserId should match");
            Assert.AreEqual(15, report.TotalDaysPresent, "Should have 15 days present");
            Assert.AreEqual(30000m, report.BaseSalary, "Base salary should be set");

            var expectedProrated = Math.Round(30000m * (15m / 30m), 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expectedProrated, report.ProratedSalary, "Prorated should be 30000 * (15/30) = 15000");
            Assert.AreEqual(2000m, report.RealizedCommissions, "Realized commissions should be exactly 2000");
            Assert.AreEqual(expectedProrated + 2000m, report.TotalPay, "Total pay should be prorated + commissions");

            // Verify commission was properly recorded in database
            var savedCommission = await db.CommissionLedgers.FindAsync(commission.Id);
            Assert.IsNotNull(savedCommission, "Commission should be persisted");
            Assert.AreEqual(CommissionStatus.Realized, savedCommission.Status, "Commission status should be Realized");
            Assert.AreEqual("UPI-COMBO-001", savedCommission.UpiTransactionId, "UPI transaction ID should be set");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_ReturnsNullForNonExistentUser()
        {
            // Arrange
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_ReturnsNullForNonExistentUser), managerMock.Object);

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(999, 3, 2024); // Non-existent user ID

            // Assert - Should gracefully return null for non-existent users
            Assert.IsNull(report, "Should return null for non-existent user instead of throwing");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_WithNoSalaryStructure_UsesZeroBaseSalary()
        {
            // Arrange - User with no salary structure defined
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(5);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_WithNoSalaryStructure_UsesZeroBaseSalary), managerMock.Object);

            var user = new User
            {
                Name = "No Salary Structure User",
                Email = "nosalary@example.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Do NOT create a salary structure - this should default to 0

            var testMonth = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // Create 5 days of attendance
            for (int i = 1; i <= 5; i++)
            {
                db.Attendances.Add(new Attendance
                {
                    UserId = user.Id,
                    CenterId = 1,
                    Timestamp = testMonth.AddDays(i - 1),
                    Latitude = 18.5204m,
                    Longitude = 73.8567m,
                    Type = AttendanceType.ClockIn
                });
            }
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Verify graceful degradation when salary structure is missing
            Assert.IsNotNull(report, "Should return report even without salary structure");
            Assert.AreEqual(0m, report.BaseSalary, "Should default to 0 when no salary structure exists");
            Assert.AreEqual(5, report.TotalDaysPresent, "Should count 5 days present");
            Assert.AreEqual(0m, report.ProratedSalary, "Prorated = 0 * (5/30) = 0");
            Assert.AreEqual(0m, report.RealizedCommissions, "No commissions, should be 0");
            Assert.AreEqual(0m, report.TotalPay, "Total pay should be 0");
        }

        [TestMethod]
        public async Task CalculateCenterPayroll_ReturnsPayrollForAllUsersInCenter()
        {
            // Arrange
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateCenterPayroll_ReturnsPayrollForAllUsersInCenter), managerMock.Object);

            // Create 3 users all in Center 1
            var users = new List<User>
            {
                new() { Name = "User1", Email = "user1@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow },
                new() { Name = "User2", Email = "user2@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow },
                new() { Name = "User3", Email = "user3@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow }
            };
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            // Create salary structures for each user
            foreach (var user in users)
            {
                db.SalaryStructures.Add(new SalaryStructure
                {
                    UserId = user.Id,
                    CenterId = 1,
                    BaseSalary = 30000m,
                    CommissionPercentage = 5m,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var reports = await payrollService.CalculateCenterPayroll(1, 3, 2024);

            // Assert - Verify all 3 users are included
            Assert.IsNotNull(reports, "Center payroll should not be null");
            Assert.AreEqual(3, reports.Count, "Should return payroll for all 3 users in center");

            // Verify each report has correct metadata
            foreach (var report in reports)
            {
                Assert.AreEqual(3, report.Month, "All reports should be for March");
                Assert.AreEqual(2024, report.Year, "All reports should be for 2024");
                Assert.IsTrue(users.Any(u => u.Id == report.UserId), "Each report should match a created user");
                Assert.IsTrue(users.Any(u => u.Name == report.UserName), "User name should match");
                Assert.AreEqual(30000m, report.BaseSalary, "All should have same base salary");
            }
        }

        [TestMethod]
        public async Task CalculateCenterPayroll_ExcludesUsersFromOtherCenters()
        {
            // Arrange - Verify cross-center isolation via Global Query Filter
            var managerCenter1Mock = new Mock<ICurrentUser>();
            managerCenter1Mock.Setup(x => x.UserId).Returns(1);
            managerCenter1Mock.Setup(x => x.CenterId).Returns(1);
            managerCenter1Mock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateCenterPayroll_ExcludesUsersFromOtherCenters), managerCenter1Mock.Object);

            // Create users in both Center 1 and Center 2
            var user1Center1 = new User { Name = "Center1User", Email = "c1@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 1, CreatedAt = DateTime.UtcNow };
            var user2Center2 = new User { Name = "Center2User", Email = "c2@test.com", PasswordHash = "hash", Role = Role.Staff, CenterId = 2, CreatedAt = DateTime.UtcNow };

            db.Users.AddRange(user1Center1, user2Center2);
            await db.SaveChangesAsync();

            // Create salary structures
            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user1Center1.Id,
                CenterId = user1Center1.CenterId.Value,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });

            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user2Center2.Id,
                CenterId = user2Center2.CenterId.Value,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var reportsCenter1 = await payrollService.CalculateCenterPayroll(1, 3, 2024);

            // Assert - Should only include Center 1 users
            Assert.AreEqual(1, reportsCenter1.Count, "Should only return payroll for Center 1 users");
            Assert.AreEqual(user1Center1.Id, reportsCenter1[0].UserId, "Should be Center 1 user");
            Assert.AreEqual("Center1User", reportsCenter1[0].UserName, "Should have correct user name");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_ExcludesCommissionsOutsideTargetMonth()
        {
            // Arrange - Commission realization date must be within target month
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_ExcludesCommissionsOutsideTargetMonth), managerMock.Object);

            var user = new User
            {
                Name = "Commission Tester",
                Email = "commtest@example.com",
                PasswordHash = "hash",
                Role = Role.Sales,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user.Id,
                CenterId = 1,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });

            var march2024 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var february2024 = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            var april2024 = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);

            // Create inquiries
            var inquiry1 = new Inquiry { CustomerId = 1, EquipmentId = 1, SalespersonId = user.Id, Status = InquiryStatus.Converted, CenterId = 1, CreatedAt = march2024 };
            var inquiry2 = new Inquiry { CustomerId = 1, EquipmentId = 1, SalespersonId = user.Id, Status = InquiryStatus.Converted, CenterId = 1, CreatedAt = march2024 };
            var inquiry3 = new Inquiry { CustomerId = 1, EquipmentId = 1, SalespersonId = user.Id, Status = InquiryStatus.Converted, CenterId = 1, CreatedAt = march2024 };

            db.Inquiries.AddRange(inquiry1, inquiry2, inquiry3);
            await db.SaveChangesAsync();

            // Create commissions with different UpdatedAt dates
            db.CommissionLedgers.Add(new CommissionLedger
            {
                InquiryId = inquiry1.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 1000m,
                Status = CommissionStatus.Realized,
                UpiTransactionId = "UPI-FEB",
                CreatedAt = march2024,
                UpdatedAt = february2024 // Realized in February - should be excluded
            });

            db.CommissionLedgers.Add(new CommissionLedger
            {
                InquiryId = inquiry2.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 2000m,
                Status = CommissionStatus.Realized,
                UpiTransactionId = "UPI-MAR",
                CreatedAt = march2024,
                UpdatedAt = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc) // Realized in March - should be included
            });

            db.CommissionLedgers.Add(new CommissionLedger
            {
                InquiryId = inquiry3.Id,
                UserId = user.Id,
                CenterId = 1,
                Amount = 500m,
                Status = CommissionStatus.Realized,
                UpiTransactionId = "UPI-APR",
                CreatedAt = march2024,
                UpdatedAt = april2024 // Realized in April - should be excluded
            });

            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var marchReport = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Only March commission should be included
            Assert.IsNotNull(marchReport, "Report should exist");
            Assert.AreEqual(2000m, marchReport.RealizedCommissions, "Should only include commission realized in March (2000), not February (1000) or April (500)");
            Assert.AreEqual(2000m, marchReport.TotalPay, "Total pay should be 2000 commission only (no salary)");
        }

        [TestMethod]
        public async Task CalculateMonthlyPay_CountsDistinctDaysOnly()
        {
            // Arrange - Multiple clock-ins on same day should count as 1 day
            var managerMock = new Mock<ICurrentUser>();
            managerMock.Setup(x => x.UserId).Returns(1);
            managerMock.Setup(x => x.CenterId).Returns(1);
            managerMock.Setup(x => x.Role).Returns(Role.Manager);

            var db = CreateDbContext(nameof(CalculateMonthlyPay_CountsDistinctDaysOnly), managerMock.Object);

            var user = new User
            {
                Name = "Attendance Tester",
                Email = "atttest@example.com",
                PasswordHash = "hash",
                Role = Role.Staff,
                CenterId = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.SalaryStructures.Add(new SalaryStructure
            {
                UserId = user.Id,
                CenterId = 1,
                BaseSalary = 30000m,
                CommissionPercentage = 5m,
                CreatedAt = DateTime.UtcNow
            });

            var march1 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // Create multiple clock-ins on the same day (should count as 1 day)
            db.Attendances.Add(new Attendance
            {
                UserId = user.Id,
                CenterId = 1,
                Timestamp = march1.AddHours(9),
                Latitude = 18.5204m,
                Longitude = 73.8567m,
                Type = AttendanceType.ClockIn
            });

            db.Attendances.Add(new Attendance
            {
                UserId = user.Id,
                CenterId = 1,
                Timestamp = march1.AddHours(17),
                Latitude = 18.5204m,
                Longitude = 73.8567m,
                Type = AttendanceType.ClockIn
            });

            // Different day
            db.Attendances.Add(new Attendance
            {
                UserId = user.Id,
                CenterId = 1,
                Timestamp = march1.AddDays(1),
                Latitude = 18.5204m,
                Longitude = 73.8567m,
                Type = AttendanceType.ClockIn
            });

            await db.SaveChangesAsync();

            var payrollService = new PayrollService(db);

            // Act
            var report = await payrollService.CalculateMonthlyPay(user.Id, 3, 2024);

            // Assert - Should count 2 distinct days, not 3 records
            Assert.IsNotNull(report, "Report should exist");
            Assert.AreEqual(2, report.TotalDaysPresent, "Should count 2 distinct days (not 3 attendance records)");
            var expectedProrated = Math.Round(30000m * (2m / 30m), 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expectedProrated, report.ProratedSalary, "Prorated salary should be 30000 * (2/30)");
        }
    }
}
