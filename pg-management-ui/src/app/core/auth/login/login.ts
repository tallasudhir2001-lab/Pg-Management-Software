import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../services/auth';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-login',
  standalone:true,  
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  userNameOrEmail = '';
  password = '';
  error = '';

  constructor(private auth:Auth,private router : Router, private toastService: ToastService){

  }
  login(){
    this.auth.login({
      userNameOrEmail: this.userNameOrEmail,
      password: this.password
    }).subscribe({
      next: res => {
        if (res.isAdmin) {
          this.auth.saveToken(res.token);
          this.router.navigate(['/admin']);
        return;
        }
        // CASE 1: Multiple PGs → temp token
        if (res.requirespgSelection) {
          this.auth.saveToken(res.tempToken);
          this.router.navigate(['/select-pg'], {
            state: { pgs: res.pgs }
          });
        }
        // CASE 2: Single PG → tenant token
        else {
          this.auth.saveToken(res.token);
          this.router.navigate(['/dashboard']);
        }
      },
      error: err => {
        this.toastService.showError(err.error || "Invalid Credentials");
      }
    });
  }
}
