import { TestBed } from '@angular/core/testing';
import { GoldApi } from './gold-api';

describe('GoldApi', () => {
  let service: GoldApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GoldApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
