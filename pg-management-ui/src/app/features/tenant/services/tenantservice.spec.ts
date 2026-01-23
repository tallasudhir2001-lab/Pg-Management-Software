import { TestBed } from '@angular/core/testing';

import { Tenantservice } from './tenantservice';

describe('Tenantservice', () => {
  let service: Tenantservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Tenantservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
