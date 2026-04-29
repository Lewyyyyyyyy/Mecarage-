import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssignMecanicienRequest,
  CreateInterventionRequest,
  CreateInterventionResponse,
  DiagnoseInterventionResponse,
  InterventionDto,
  MessageResponse,
  UpdateInterventionStatusRequest,
} from '../models/intervention.models';

@Injectable({
  providedIn: 'root',
})
export class InterventionsService {
  private readonly interventionsUrl = `${environment.apiBaseUrl}/interventions`;

  constructor(private readonly http: HttpClient) {}

  getMyInterventions(): Observable<InterventionDto[]> {
    return this.http.get<InterventionDto[]>(this.interventionsUrl);
  }

  create(payload: CreateInterventionRequest): Observable<CreateInterventionResponse> {
    return this.http.post<CreateInterventionResponse>(this.interventionsUrl, payload);
  }

  updateStatus(interventionId: string, payload: UpdateInterventionStatusRequest): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(`${this.interventionsUrl}/${interventionId}/status`, payload);
  }

  assignMecanicien(interventionId: string, payload: AssignMecanicienRequest): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(`${this.interventionsUrl}/${interventionId}/assign`, payload);
  }

  diagnose(interventionId: string): Observable<DiagnoseInterventionResponse> {
    return this.http.post<DiagnoseInterventionResponse>(`${this.interventionsUrl}/${interventionId}/diagnose`, {});
  }
}

