import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentsHistory } from '../payments-history/payments-history';
import { RentCollectionView } from '../rent-collection-view/rent-collection-view';

@Component({
  selector: 'app-payments-landing',
  standalone: true,
  imports: [CommonModule, PaymentsHistory, RentCollectionView],
  templateUrl: './payments-landing.html',
  styleUrl: './payments-landing.css'
})
export class PaymentsLanding {
  activeTab: 'history' | 'collection' = 'history';

  switchTab(tab: 'history' | 'collection'): void {
    this.activeTab = tab;
  }
}
