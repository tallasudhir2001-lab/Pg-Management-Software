import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SettingsService, NotificationSettingsResponse } from '../services/settings.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  loading = true;
  saving = false;
  error = '';
  successMsg = '';

  autoSendPaymentReceipt = false;
  sendViaEmail = true;
  sendViaWhatsapp = false;

  isEmailSubscriptionEnabled = false;
  isWhatsappSubscriptionEnabled = false;

  constructor(private settingsService: SettingsService, private cdr: ChangeDetectorRef, private router: Router) {}

  goBack(): void { this.router.navigate(['/configurations']); }

  ngOnInit(): void {
    this.settingsService.getNotificationSettings().subscribe({
      next: (res) => {
        this.applySettings(res);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load settings.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onAutoSendToggle(): void {
    if (this.autoSendPaymentReceipt) {
      // Default to email when enabling auto-send
      this.sendViaEmail = true;
    } else {
      this.sendViaEmail = false;
      this.sendViaWhatsapp = false;
    }
  }

  onEmailToggle(): void {
    // Ensure at least one channel is selected when auto-send is on
    if (this.autoSendPaymentReceipt && !this.sendViaEmail && !this.sendViaWhatsapp) {
      this.sendViaWhatsapp = true;
    }
  }

  onWhatsappToggle(): void {
    if (this.autoSendPaymentReceipt && !this.sendViaEmail && !this.sendViaWhatsapp) {
      this.sendViaEmail = true;
    }
  }

  save(): void {
    if (this.autoSendPaymentReceipt && !this.sendViaEmail && !this.sendViaWhatsapp) {
      this.error = 'Please select at least one channel (Email or WhatsApp).';
      return;
    }

    this.saving = true;
    this.error = '';
    this.successMsg = '';

    this.settingsService.updateNotificationSettings({
      autoSendPaymentReceipt: this.autoSendPaymentReceipt,
      sendViaEmail: this.sendViaEmail,
      sendViaWhatsapp: this.sendViaWhatsapp
    }).subscribe({
      next: (res) => {
        this.applySettings(res);
        this.saving = false;
        this.successMsg = 'Settings saved successfully!';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: () => {
        this.error = 'Failed to save settings.';
        this.saving = false;
        this.cdr.detectChanges();
      }
    });
  }

  private applySettings(res: NotificationSettingsResponse): void {
    this.autoSendPaymentReceipt = res.autoSendPaymentReceipt;
    this.sendViaEmail = res.sendViaEmail;
    this.sendViaWhatsapp = res.sendViaWhatsapp;
    this.isEmailSubscriptionEnabled = res.isEmailSubscriptionEnabled;
    this.isWhatsappSubscriptionEnabled = res.isWhatsappSubscriptionEnabled;
  }
}
