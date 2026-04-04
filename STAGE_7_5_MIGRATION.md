# Stage 7.5 Migration Command

## Execute this command in PowerShell from the repository root:

```powershell
cd C:\Github\agriapp-api
dotnet ef migrations add Stage7_5_WorkOrderTimeLogs --project src/AgriApp.Infrastructure --startup-project src/AgriApp.Api
```

### What This Migration Will Do:

1. **Create `work_order_time_logs` table**
   - `id` (INT, PK)
   - `work_order_id` (INT, FK→work_orders)
   - `start_time` (TIMESTAMP, NOT NULL)
   - `end_time` (TIMESTAMP, NOT NULL)
   - `log_type` (VARCHAR(50), NOT NULL) → Working|Break|Breakdown
   - `notes` (VARCHAR(500), nullable)
   - `created_at` (TIMESTAMP, DEFAULT NOW())
   - `updated_at` (TIMESTAMP, nullable)

2. **Add Index**
   - `IX_WorkOrderTimeLogs_WorkOrderId` on `work_order_id` for query performance

3. **Add Constraint**
   - `CK_WorkOrderTimeLogs_EndTimeAfterStartTime` → ensures `end_time > start_time`

4. **Foreign Key Relationship**
   - `work_order_id` → `work_orders(id)` with CASCADE delete

### After Migration Runs:

Apply the migration to your PostgreSQL database:

```powershell
dotnet ef database update --project src/AgriApp.Infrastructure --startup-project src/AgriApp.Api
```

### Verification Query (PostgreSQL):

```sql
SELECT * FROM work_order_time_logs;
SELECT * FROM information_schema.tables WHERE table_name = 'work_order_time_logs';
```

---

## Files Changed:
- ✅ `src/AgriApp.Core/Entities/WorkOrderTimeLog.cs` (created)
- ✅ `src/AgriApp.Core/Enums/WorkTimeLogType.cs` (created)
- ✅ `src/AgriApp.Core/Entities/WorkOrder.cs` (navigation added)
- ✅ `src/AgriApp.Infrastructure/Data/AgriDbContext.cs` (DbSet + ConfigureWorkOrderTimeLog)
- ✅ `src/AgriApp.Web/Services/ICalendarService.cs` (created)
- ✅ `src/AgriApp.Web/Services/CalendarService.cs` (created)
- ✅ `src/AgriApp.Web/Pages/Calendar.razor` (wired)
- ✅ `src/AgriApp.Web/Services/WorkOrderService.cs` (route fixed: api/work-orders → api/workorders)
- ⏳ `src/AgriApp.Infrastructure/Migrations/[timestamp]_Stage7_5_WorkOrderTimeLogs.cs` (will be created)
