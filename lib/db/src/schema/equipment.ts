import { pgTable, serial, text, integer, numeric, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";
import { centersTable } from "./centers";

export const equipmentCategoryEnum = pgEnum("equipment_category", [
  "Tractor",
  "Drone",
  "BioCNG",
]);

export const equipmentTable = pgTable("equipment", {
  id: serial("id").primaryKey(),
  name: text("name").notNull(),
  category: equipmentCategoryEnum("category").notNull(),
  hourlyRate: numeric("hourly_rate", { precision: 10, scale: 2 }).notNull(),
  centerId: integer("center_id").notNull().references(() => centersTable.id),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const insertEquipmentSchema = createInsertSchema(equipmentTable).omit({ id: true, createdAt: true });
export type InsertEquipment = z.infer<typeof insertEquipmentSchema>;
export type Equipment = typeof equipmentTable.$inferSelect;
