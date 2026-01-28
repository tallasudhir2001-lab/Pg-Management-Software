import { Component, OnInit } from '@angular/core';
import { ToastService } from './toast-service';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { ToastModel } from '../models/toast.model';

@Component({
  selector: 'app-toast',
  imports: [CommonModule],
  templateUrl: './toast.html',
  styleUrl: './toast.css',
})
export class Toast {

   toast$!: Observable<ToastModel | null>;

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.toast$ = this.toastService.toast$;
  }

}
