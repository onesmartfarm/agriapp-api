import { eq, and } from "drizzle-orm";
import { db, workOrdersTable, type InsertWorkOrder } from "@workspace/db";
import type { ICurrentUser } from "../../core/interfaces";
import { buildWorkOrderFilters } from "../queryFilters";

export class WorkOrderRepository {
  async findAll(currentUser: ICurrentUser) {
    const filters = buildWorkOrderFilters(currentUser);
    if (filters.length === 0) {
      return db.select().from(workOrdersTable);
    }
    return db.select().from(workOrdersTable).where(and(...filters));
  }

  async findById(id: number, currentUser: ICurrentUser) {
    const filters = buildWorkOrderFilters(currentUser);
    const allFilters = [eq(workOrdersTable.id, id), ...filters];
    const results = await db
      .select()
      .from(workOrdersTable)
      .where(and(...allFilters));
    return results[0] || null;
  }

  async create(data: InsertWorkOrder) {
    const results = await db.insert(workOrdersTable).values(data).returning();
    return results[0];
  }

  async updateStatus(id: number, status: string, currentUser: ICurrentUser) {
    const filters = buildWorkOrderFilters(currentUser);
    const allFilters = [eq(workOrdersTable.id, id), ...filters];
    const results = await db
      .update(workOrdersTable)
      .set({ status: status as any })
      .where(and(...allFilters))
      .returning();
    return results[0] || null;
  }
}
