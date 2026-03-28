export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  name: string;
  email: string;
  password: string;
  role: string;
  centerId: number | null;
}

export interface CreateEquipmentDto {
  name: string;
  category: string;
  hourlyRate: string;
  centerId: number;
}

export interface CreateInquiryDto {
  customerId: number;
  equipmentId: number;
  salespersonId: number;
  centerId: number;
}

export interface CreateWorkOrderDto {
  equipmentId: number;
  staffId: number;
  description: string;
  centerId: number;
}

export interface RentalQuoteDto {
  equipmentId: number;
  hours: number;
}

export interface AuthResponseDto {
  token: string;
  user: {
    id: number;
    name: string;
    email: string;
    role: string;
    centerId: number | null;
  };
}
