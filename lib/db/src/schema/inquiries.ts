import { pgTable, serial, integer, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";
import { usersTable } from "./users";
import { equipmentTable } from "./equipment";

export const inquiryStatusEnum = pgEnum("inquiry_status", [
  "New",
  "InProgress",
  "Converted",
  "Closed",
]);

export const inquiriesTable = pgTable("inquiries", {
  id: serial("id").primaryKey(),
  customerId: integer("customer_id").notNull().references(() => usersTable.id),
  equipmentId: integer("equipment_id").notNull().references(() => equipmentTable.id),
  salespersonId: integer("salesperson_id").notNull().references(() => usersTable.id),
  status: inquiryStatusEnum("status").notNull().default("New"),
  centerId: integer("center_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const insertInquirySchema = createInsertSchema(inquiriesTable).omit({ id: true, createdAt: true });
export type InsertInquiry = z.infer<typeof insertInquirySchema>;
export type Inquiry = typeof inquiriesTable.$inferSelect;
