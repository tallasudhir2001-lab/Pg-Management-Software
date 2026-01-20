import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Adminservice } from '../../services/adminservice';
@Component({
  selector: 'app-register-pg',
  standalone:true,
  imports: [CommonModule,FormsModule],
  templateUrl: './register-pg.html',
  styleUrl: './register-pg.css',
})
export class RegisterPg {
  pgName = '';
  address = '';
  contactNumber = '';
  ownerName = '';
  ownerEmail = '';
  password = '';

  success = '';
  error = '';

  constructor(private adminservice: Adminservice) {}
  registerPg() {
    this.success = '';
    this.error = '';

    this.adminservice.registerPg({
      pgName: this.pgName,
      address: this.address,
      contactNumber: this.contactNumber,
      ownerName:this.ownerName,
      ownerEmail: this.ownerEmail,
      password: this.password
    }).subscribe({
      next: () => {
        this.success = 'PG registered successfully';
        this.pgName = this.address = this.contactNumber = '';
        this.ownerName = this.ownerEmail = this.password = '';
      },
      error: () => {
        this.error = 'Failed to register PG';
      }
    });
  }

}
