export interface VehicleDto {
  id: string;
  clientId: string;
  brand: string;
  model: string;
  year: number;
  licensePlate: string;
  fuelType: string;
  mileage: number;
  vin?: string | null;
}

export interface CreateVehicleRequest {
  brand: string;
  model: string;
  year: number;
  licensePlate: string;
  fuelType: string;
  mileage: number;
  vin?: string | null;
}

export interface CreateVehicleResponse {
  message: string;
  vehicleId?: string;
}

