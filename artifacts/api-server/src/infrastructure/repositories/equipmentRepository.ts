import { eq, and } from "drizzle-orm";
import { db, equipmentTable, type InsertEquipment } from "@workspace/db";
import type { ICurrentUser } from "../../core/interfaces";
import { buildEquipmentFilters } from "../queryFilters";

export class EquipmentRepository {
  async findAll(currentUser: ICurrentUser) {
    const filters = buildEquipmentFilters(currentUser);
    if (filters.length === 0) {
      return db.select().from(equipmentTable);
    }
    return db.select().from(equipmentTable).where(and(...filters));
  }

  async findById(id: number, currentUser: ICurrentUser) {
    const filters = buildEquipmentFilters(currentUser);
    const allFilters = [eq(equipmentTable.id, id), ...filters];
    const results = await db
      .select()
      .from(equipmentTable)
      .where(and(...allFilters));
    return results[0] || null;
  }

  async create(data: InsertEquipment) {
    const results = await db.insert(equipmentTable).values(data).returning();
    return results[0];
  }

  async update(id: number, data: Partial<InsertEquipment>, currentUser: ICurrentUser) {
    const filters = buildEquipmentFilters(currentUser);
    const allFilters = [eq(equipmentTable.id, id), ...filters];
    const results = await db
      .update(equipmentTable)
      .set(data)
      .where(and(...allFilters))
      .returning();
    return results[0] || null;
  }

  async delete(id: number, currentUser: ICurrentUser) {
    const filters = buildEquipmentFilters(currentUser);
    const allFilters = [eq(equipmentTable.id, id), ...filters];
    const results = await db
      .delete(equipmentTable)
      .where(and(...allFilters))
      .returning();
    return results[0] || null;
  }
}
