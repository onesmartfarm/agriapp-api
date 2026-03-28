import { eq, and, type SQL } from "drizzle-orm";
import type { ICurrentUser } from "../core/interfaces";
import { Role } from "../core/enums";
import { equipmentTable, inquiriesTable, workOrdersTable } from "@workspace/db";

export function applyCenterFilter<T extends { centerId: any }>(
  table: T,
  currentUser: ICurrentUser,
): SQL | undefined {
  if (currentUser.role === Role.SuperUser) {
    return undefined;
  }
  if (currentUser.centerId === null) {
    throw new Error("Non-SuperUser must have a centerId");
  }
  return eq(table.centerId, currentUser.centerId);
}

export function applyInquiryOwnershipFilter(
  currentUser: ICurrentUser,
): SQL | undefined {
  if (currentUser.role === Role.SuperUser) {
    return undefined;
  }
  if (currentUser.role === Role.Sales) {
    return eq(inquiriesTable.salespersonId, currentUser.userId);
  }
  return undefined;
}

export function buildInquiryFilters(currentUser: ICurrentUser): SQL[] {
  const filters: SQL[] = [];

  const centerFilter = applyCenterFilter(inquiriesTable, currentUser);
  if (centerFilter) filters.push(centerFilter);

  const ownerFilter = applyInquiryOwnershipFilter(currentUser);
  if (ownerFilter) filters.push(ownerFilter);

  return filters;
}

export function buildEquipmentFilters(currentUser: ICurrentUser): SQL[] {
  const filters: SQL[] = [];
  const centerFilter = applyCenterFilter(equipmentTable, currentUser);
  if (centerFilter) filters.push(centerFilter);
  return filters;
}

export function buildWorkOrderFilters(currentUser: ICurrentUser): SQL[] {
  const filters: SQL[] = [];
  const centerFilter = applyCenterFilter(workOrdersTable, currentUser);
  if (centerFilter) filters.push(centerFilter);
  return filters;
}
