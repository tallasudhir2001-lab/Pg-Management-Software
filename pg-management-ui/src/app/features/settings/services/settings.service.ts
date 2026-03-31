import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface NotificationSettingsResponse {
  autoSendPaymentReceipt: boolean;
  sendViaEmail: boolean;
  sendViaWhatsapp: boolean;
  isEmailSubscriptionEnabled: boolean;
  isWhatsappSubscriptionEnabled: boolean;
}

export interface NotificationSettingsUpdate {
  autoSendPaymentReceipt: boolean;
  sendViaEmail: boolean;
  sendViaWhatsapp: boolean;
}

export interface SubscriptionStatus {
  isEmailSubscriptionEnabled: boolean;
  isWhatsappSubscriptionEnabled: boolean;
}

export interface ReportOption {
  reportType: string;
  displayName: string;
  description: string;
}

export interface UserReportSubscription {
  userId: string;
  fullName: string;
  email: string;
  subscribedReports: string[];
}

export interface UpdateUserReportSubscriptions {
  userId: string;
  reportTypes: string[];
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly baseUrl = `${environment.apiBaseUrl}/settings`;

  constructor(private http: HttpClient) {}

  getNotificationSettings(): Observable<NotificationSettingsResponse> {
    return this.http.get<NotificationSettingsResponse>(`${this.baseUrl}/notifications`);
  }

  updateNotificationSettings(data: NotificationSettingsUpdate): Observable<NotificationSettingsResponse> {
    return this.http.put<NotificationSettingsResponse>(`${this.baseUrl}/notifications`, data);
  }

  getSubscriptionStatus(): Observable<SubscriptionStatus> {
    return this.http.get<SubscriptionStatus>(`${this.baseUrl}/subscription-status`);
  }

  getReportOptions(): Observable<ReportOption[]> {
    return this.http.get<ReportOption[]>(`${this.baseUrl}/report-subscriptions/report-options`);
  }

  getReportSubscriptions(): Observable<UserReportSubscription[]> {
    return this.http.get<UserReportSubscription[]>(`${this.baseUrl}/report-subscriptions`);
  }

  updateReportSubscriptions(userSubscriptions: UpdateUserReportSubscriptions[]): Observable<any> {
    return this.http.put(`${this.baseUrl}/report-subscriptions`, { userSubscriptions });
  }
}
