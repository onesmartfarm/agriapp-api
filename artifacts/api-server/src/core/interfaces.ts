import { Role } from "./enums";

export interface ICurrentUser {
  userId: number;
  email: string;
  role: Role;
  centerId: number | null;
}
