import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SymptomReportDto,
  CreateSymptomReportDto,
  AddChefFeedbackDto,
  ChefInboxItemDto,
  PendingAppointmentDto,
  AppointmentDto,
  CreateAppointmentDto,
  InvoiceDto,
  CreateInvoiceDto,
  MechanicTaskDto,
  CreateRepairTaskDto,
  UpdateRepairTaskStatusDto,
  SubmitExaminationDto,
  PendingExaminationDto,
  RepairReadyTaskDto,
  ClientNotificationDto,
  SparePartDto,
  CreateSparePartDto,
  UpdateSparePartDto,
} from '../models/workshop.models';

@Injectable({ providedIn: 'root' })
export class SymptomReportService {
  private readonly apiUrl = `${environment.apiBaseUrl}/symptomreports`;

  constructor(private http: HttpClient) {}

  createReport(data: CreateSymptomReportDto): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  getMyReports(): Observable<SymptomReportDto[]> {
    return this.http.get<SymptomReportDto[]>(`${this.apiUrl}/my-reports`);
  }

  getPendingReviews(garageId: string): Observable<ChefInboxItemDto[]> {
    return this.http.get<ChefInboxItemDto[]>(`${this.apiUrl}/pending-reviews/${garageId}`);
  }

  getChefInbox(garageId: string): Observable<ChefInboxItemDto[]> {
    return this.getPendingReviews(garageId);
  }

  addFeedback(reportId: string, data: AddChefFeedbackDto): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${reportId}/feedback`, data);
  }
}

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private apiUrl = `${environment.apiBaseUrl}/appointments`;

  constructor(private http: HttpClient) {}

  createAppointment(data: CreateAppointmentDto): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  getMyAppointments(): Observable<AppointmentDto[]> {
    return this.http.get<AppointmentDto[]>(`${this.apiUrl}/my-appointments`);
  }

  getPendingAppointments(garageId: string): Observable<PendingAppointmentDto[]> {
    return this.http.get<PendingAppointmentDto[]>(`${this.apiUrl}/pending/${garageId}`);
  }

  approveAppointment(appointmentId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${appointmentId}/approve`, {});
  }

  declineAppointment(appointmentId: string, declineReason: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${appointmentId}/decline`, { declineReason });
  }
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private apiUrl = `${environment.apiBaseUrl}/invoices`;

  constructor(private http: HttpClient) {}

  createInvoice(data: CreateInvoiceDto): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  getMyInvoices(): Observable<InvoiceDto[]> {
    return this.http.get<InvoiceDto[]>(`${this.apiUrl}/my-invoices`);
  }

  getGarageInvoices(garageId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/garage/${garageId}`);
  }

  finalizeInvoice(invoiceId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${invoiceId}/finalize`, {});
  }

  approveInvoice(invoiceId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${invoiceId}/approve`, {});
  }

  rejectInvoice(invoiceId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${invoiceId}/reject`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class RepairTaskService {
  private apiUrl = `${environment.apiBaseUrl}/repairtasks`;

  constructor(private http: HttpClient) {}

  createTask(data: CreateRepairTaskDto): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  getMyTasks(): Observable<MechanicTaskDto[]> {
    return this.http.get<MechanicTaskDto[]>(`${this.apiUrl}/my-tasks`);
  }

  getTaskDetails(taskId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${taskId}`);
  }

  assignMechanic(taskId: string, mechanicId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${taskId}/assign-mechanic`, { mechanicId });
  }

  updateTaskStatus(taskId: string, data: UpdateRepairTaskStatusDto): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${taskId}/status`, data);
  }

  submitExaminationReport(taskId: string, data: SubmitExaminationDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/${taskId}/examination-report`, data);
  }

  uploadExaminationFile(taskId: string, file: File): Observable<{ fileUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ fileUrl: string }>(`${this.apiUrl}/${taskId}/upload-exam-file`, formData);
  }

  reviewExamination(taskId: string, isApproved: boolean, serviceFee: number, declineReason?: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${taskId}/review-examination`, { isApproved, serviceFee, declineReason });
  }

  getPendingExaminations(garageId: string): Observable<PendingExaminationDto[]> {
    return this.http.get<PendingExaminationDto[]>(`${this.apiUrl}/pending-examinations/${garageId}`);
  }

  getRepairReadyTasks(garageId: string): Observable<RepairReadyTaskDto[]> {
    return this.http.get<RepairReadyTaskDto[]>(`${this.apiUrl}/repair-ready/${garageId}`);
  }

  markTaskTested(taskId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${taskId}/mark-tested`, {});
  }

  signalReadyForPickup(taskId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${taskId}/ready-for-pickup`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl = `${environment.apiBaseUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getMyNotifications(): Observable<ClientNotificationDto[]> {
    return this.http.get<ClientNotificationDto[]>(`${this.apiUrl}/my-notifications`);
  }

  markAsRead(notificationId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${notificationId}/mark-read`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class SparePartsService {
  private apiUrl(garageId: string) {
    return `${environment.apiBaseUrl}/garages/${garageId}/stock`;
  }

  constructor(private http: HttpClient) {}

  getStock(garageId: string, category?: string, search?: string): Observable<SparePartDto[]> {
    let url = this.apiUrl(garageId);
    const params: string[] = [];
    if (category) params.push(`category=${encodeURIComponent(category)}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (params.length) url += '?' + params.join('&');
    return this.http.get<SparePartDto[]>(url);
  }

  createPart(garageId: string, data: CreateSparePartDto): Observable<any> {
    return this.http.post(this.apiUrl(garageId), data);
  }

  updatePart(garageId: string, partId: string, data: UpdateSparePartDto): Observable<any> {
    return this.http.put(`${this.apiUrl(garageId)}/${partId}`, data);
  }

  restock(garageId: string, partId: string, quantityToAdd: number): Observable<any> {
    return this.http.patch(`${this.apiUrl(garageId)}/${partId}/restock`, { quantityToAdd });
  }

  deletePart(garageId: string, partId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl(garageId)}/${partId}`);
  }
}

