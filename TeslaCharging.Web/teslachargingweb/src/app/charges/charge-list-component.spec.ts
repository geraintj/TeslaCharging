import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ChargeListComponentComponent } from './charge-list.component';

describe('ChargeListComponentComponent', () => {
  let component: ChargeListComponentComponent;
  let fixture: ComponentFixture<ChargeListComponentComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargeListComponentComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargeListComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
