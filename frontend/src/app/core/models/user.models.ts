import { UserRole } from './auth.models';

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: UserRole | string;
  isActive: boolean;
  tenantId?: string;
  garageId?: string | null;
  garageName?: string | null;
}

export interface AssignUserToGarageRequest {
  userId: string;
  garageId: string;
}

export interface AssignUserToGarageResponse {
  message: string;
}
