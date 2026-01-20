import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-pg-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pg-select.html'
})
export class PgSelect {
  pgs: any[] = [];

  constructor(
    private router: Router,
    private auth: Auth
  ) {
    const nav = this.router.getCurrentNavigation();
    this.pgs = nav?.extras?.state?.['pgs'] || [];
  }

  selectPg(pgId: string) {
    this.auth.selectPg(pgId).subscribe(res => {
      this.auth.saveToken(res.token); // replace temp token
      this.router.navigate(['/dashboard']);
    });
  }
}
