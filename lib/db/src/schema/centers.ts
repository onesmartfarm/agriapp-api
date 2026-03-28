import { pgTable, serial, text, timestamp } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const centersTable = pgTable("centers", {
  id: serial("id").primaryKey(),
  name: text("name").notNull(),
  location: text("location").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const insertCenterSchema = createInsertSchema(centersTable).omit({ id: true, createdAt: true });
export type InsertCenter = z.infer<typeof insertCenterSchema>;
export type Center = typeof centersTable.$inferSelect;
