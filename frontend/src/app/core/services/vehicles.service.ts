import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateVehicleRequest,
  CreateVehicleResponse,
  VehicleDto,
} from '../models/vehicle.models';

@Injectable({
  providedIn: 'root',
})
export class VehiclesService {
  private readonly vehiclesUrl = `${environment.apiBaseUrl}/vehicles`;

  constructor(private readonly http: HttpClient) {}

  getMyVehicles(): Observable<VehicleDto[]> {
    return this.http.get<VehicleDto[]>(this.vehiclesUrl);
  }

  create(payload: CreateVehicleRequest): Observable<CreateVehicleResponse> {
    return this.http.post<CreateVehicleResponse>(this.vehiclesUrl, payload);
  }
}

