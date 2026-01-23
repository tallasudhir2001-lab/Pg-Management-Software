import { Component } from '@angular/core';
import { Router, RouterOutlet,RouterLink } from '@angular/router';
import { RouterModule } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { jwtDecode } from 'jwt-decode';
@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet,RouterLink,RouterModule],
  templateUrl: './layout.html',
  styleUrl: './layout.css',
})
export class Layout {
  //isAdmin=false;
  constructor(private auth: Auth,private router: Router) {}

//   ngOnInit() {
//   const token = localStorage.getItem('token');
//   if (token) {
//     const decoded: any = jwtDecode(token);
//     this.isAdmin = decoded.role === 'Admin';
//   }
// }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
