import { eq } from "drizzle-orm";
import { db, usersTable, type InsertUser } from "@workspace/db";

export class UserRepository {
  async findByEmail(email: string) {
    const results = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.email, email));
    return results[0] || null;
  }

  async findById(id: number) {
    const results = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.id, id));
    return results[0] || null;
  }

  async create(data: InsertUser) {
    const results = await db.insert(usersTable).values(data).returning();
    return results[0];
  }

  async findAll() {
    return db.select().from(usersTable);
  }
}
