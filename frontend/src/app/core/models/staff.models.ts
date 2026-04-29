// Staff Management Models
export interface CreateStaffDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  role: 'ChefAtelier' | 'Mecanicien'; // Chef or Mechanic
  garageId: string;
}

export interface StaffDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
  garageId: string;
  isActive: boolean;
}

export interface CreateStaffResponse {
  message: string;
  userId: string;
}

