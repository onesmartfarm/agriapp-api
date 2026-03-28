import { pgTable, serial, integer, text, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";
import { equipmentTable } from "./equipment";
import { usersTable } from "./users";

export const workStatusEnum = pgEnum("work_status", [
  "Pending",
  "InProgress",
  "Completed",
  "Cancelled",
]);

export const workOrdersTable = pgTable("work_orders", {
  id: serial("id").primaryKey(),
  equipmentId: integer("equipment_id").notNull().references(() => equipmentTable.id),
  staffId: integer("staff_id").notNull().references(() => usersTable.id),
  description: text("description").notNull(),
  status: workStatusEnum("status").notNull().default("Pending"),
  centerId: integer("center_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const insertWorkOrderSchema = createInsertSchema(workOrdersTable).omit({ id: true, createdAt: true });
export type InsertWorkOrder = z.infer<typeof insertWorkOrderSchema>;
export type WorkOrder = typeof workOrdersTable.$inferSelect;
