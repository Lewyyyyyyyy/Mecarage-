import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateGarageRequest,
  CreateGarageResponse,
  GarageDto,
  GarageInterventionDto,
  GarageClientDto,
} from '../models/garage.models';

@Injectable({
  providedIn: 'root',
})
export class GaragesService {
  private readonly garagesUrl = `${environment.apiBaseUrl}/garages`;
  private readonly tenantsUrl = `${environment.apiBaseUrl}/tenants`;

  constructor(private readonly http: HttpClient) {}

  getMyGarages(): Observable<GarageDto[]> {
    return this.http.get<GarageDto[]>(this.garagesUrl);
  }

  getAllGarages(): Observable<GarageDto[]> {
    return this.http.get<GarageDto[]>(`${this.garagesUrl}/all`);
  }

  getTenantGarages(tenantId: string): Observable<GarageDto[]> {
    return this.http.get<GarageDto[]>(`${this.garagesUrl}/tenant/${tenantId}`);
  }

  create(payload: CreateGarageRequest): Observable<CreateGarageResponse> {
    return this.http.post<CreateGarageResponse>(this.garagesUrl, payload);
  }

  createWithTenant(payload: any, tenantId: string): Observable<CreateGarageResponse> {
    return this.http.post<CreateGarageResponse>(`${this.garagesUrl}?tenantId=${tenantId}`, payload);
  }

  createTenant(payload: any): Observable<any> {
    return this.http.post<any>(this.tenantsUrl, payload);
  }

  getGarageInterventions(garageId: string): Observable<GarageInterventionDto[]> {
    return this.http.get<GarageInterventionDto[]>(`${this.garagesUrl}/${garageId}/interventions`);
  }

  getGarageClients(garageId: string): Observable<GarageClientDto[]> {
    return this.http.get<GarageClientDto[]>(`${this.garagesUrl}/${garageId}/clients`);
  }
}
