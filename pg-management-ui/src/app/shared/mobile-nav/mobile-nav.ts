import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-mobile-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './mobile-nav.html',
  styleUrl: './mobile-nav.css'
})
export class MobileNav {
  showMore = false;

  constructor(private router: Router) {}

  toggleMore() {
    this.showMore = !this.showMore;
  }

  closeMore() {
    this.showMore = false;
  }

  navigateTo(path: string) {
    this.showMore = false;
    this.router.navigate([path]);
  }

  get isMoreActive(): boolean {
    const url = this.router.url;
    return url.startsWith('/expenses') || url.startsWith('/bookings');
  }
}
