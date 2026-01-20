import { TestBed } from '@angular/core/testing';

import { Roomservice } from './roomservice';

describe('Roomservice', () => {
  let service: Roomservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Roomservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
