import { eq, and } from "drizzle-orm";
import { db, inquiriesTable, type InsertInquiry } from "@workspace/db";
import type { ICurrentUser } from "../../core/interfaces";
import { buildInquiryFilters } from "../queryFilters";

export class InquiryRepository {
  async findAll(currentUser: ICurrentUser) {
    const filters = buildInquiryFilters(currentUser);
    if (filters.length === 0) {
      return db.select().from(inquiriesTable);
    }
    return db.select().from(inquiriesTable).where(and(...filters));
  }

  async findById(id: number, currentUser: ICurrentUser) {
    const filters = buildInquiryFilters(currentUser);
    const allFilters = [eq(inquiriesTable.id, id), ...filters];
    const results = await db
      .select()
      .from(inquiriesTable)
      .where(and(...allFilters));
    return results[0] || null;
  }

  async create(data: InsertInquiry) {
    const results = await db.insert(inquiriesTable).values(data).returning();
    return results[0];
  }

  async updateStatus(id: number, status: string, currentUser: ICurrentUser) {
    const filters = buildInquiryFilters(currentUser);
    const allFilters = [eq(inquiriesTable.id, id), ...filters];
    const results = await db
      .update(inquiriesTable)
      .set({ status: status as any })
      .where(and(...allFilters))
      .returning();
    return results[0] || null;
  }
}
