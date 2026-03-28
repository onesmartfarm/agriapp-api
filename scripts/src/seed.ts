import { db } from "@workspace/db";
import { centersTable, usersTable, equipmentTable } from "@workspace/db/schema";
import bcrypt from "bcryptjs";

async function seed() {
  console.log("Seeding database...");

  const existingCenters = await db.select().from(centersTable);
  if (existingCenters.length > 0) {
    console.log("Database already seeded. Skipping.");
    process.exit(0);
  }

  const [center] = await db
    .insert(centersTable)
    .values({
      name: "AgriCenter Pune",
      location: "Pune, Maharashtra, India",
    })
    .returning();

  console.log(`Created center: ${center.name} (ID: ${center.id})`);

  const superUserHash = await bcrypt.hash("SuperUser123!", 10);
  const [superUser] = await db
    .insert(usersTable)
    .values({
      name: "Admin SuperUser",
      email: "admin@agriapp.com",
      passwordHash: superUserHash,
      role: "SuperUser",
      centerId: null,
    })
    .returning();

  console.log(`Created SuperUser: ${superUser.email} (ID: ${superUser.id})`);

  const managerHash = await bcrypt.hash("Manager123!", 10);
  const [manager] = await db
    .insert(usersTable)
    .values({
      name: "Rajesh Kumar",
      email: "rajesh@agriapp.com",
      passwordHash: managerHash,
      role: "Manager",
      centerId: center.id,
    })
    .returning();

  console.log(`Created Manager: ${manager.email} (ID: ${manager.id})`);

  const salesHash = await bcrypt.hash("Sales123!", 10);
  const [salesUser] = await db
    .insert(usersTable)
    .values({
      name: "Priya Sharma",
      email: "priya@agriapp.com",
      passwordHash: salesHash,
      role: "Sales",
      centerId: center.id,
    })
    .returning();

  console.log(`Created Sales user: ${salesUser.email} (ID: ${salesUser.id})`);

  const staffHash = await bcrypt.hash("Staff123!", 10);
  const [staffUser] = await db
    .insert(usersTable)
    .values({
      name: "Amit Patel",
      email: "amit@agriapp.com",
      passwordHash: staffHash,
      role: "Staff",
      centerId: center.id,
    })
    .returning();

  console.log(`Created Staff user: ${staffUser.email} (ID: ${staffUser.id})`);

  const equipmentData = [
    { name: "John Deere 5405", category: "Tractor" as const, hourlyRate: "1500.00", centerId: center.id },
    { name: "DJI Agras T40", category: "Drone" as const, hourlyRate: "2500.00", centerId: center.id },
    { name: "Bio-CNG Generator 500", category: "BioCNG" as const, hourlyRate: "800.00", centerId: center.id },
    { name: "Mahindra 575 DI", category: "Tractor" as const, hourlyRate: "1200.00", centerId: center.id },
  ];

  const equipment = await db.insert(equipmentTable).values(equipmentData).returning();
  console.log(`Created ${equipment.length} equipment records`);

  console.log("\nSeed complete!");
  console.log("\nLogin credentials:");
  console.log("  SuperUser: admin@agriapp.com / SuperUser123!");
  console.log("  Manager:   rajesh@agriapp.com / Manager123!");
  console.log("  Sales:     priya@agriapp.com / Sales123!");
  console.log("  Staff:     amit@agriapp.com / Staff123!");

  process.exit(0);
}

seed().catch((err) => {
  console.error("Seed failed:", err);
  process.exit(1);
});
