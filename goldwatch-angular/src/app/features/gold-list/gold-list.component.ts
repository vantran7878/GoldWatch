import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GoldService } from '../../core/services/gold.service';
import { GoldPrice, CreateGoldPriceRequest } from '../../core/models/gold-price.model';

type ModalMode = 'create' | 'edit' | null;

@Component({
  selector: 'app-gold-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gold-list.component.html',
  styleUrl: './gold-list.component.scss'
})
export class GoldListComponent {
  @Input() prices: GoldPrice[] = [];
  @Output() dataChanged = new EventEmitter<void>();

  private readonly goldService = inject(GoldService);

  // Modal state
  modalMode = signal<ModalMode>(null);
  editingId = signal<number | null>(null);
  isSubmitting = signal(false);
  modalError = signal<string | null>(null);

  // Form model
  form: CreateGoldPriceRequest = this.emptyForm();

  // Confirm delete
  deletingId = signal<number | null>(null);

  // ─── Modal helpers ───────────────────────────────────────

  openCreate(): void {
    this.form = this.emptyForm();
    this.modalError.set(null);
    this.modalMode.set('create');
  }

  openEdit(price: GoldPrice): void {
    this.form = {
      goldType: price.goldType,
      buyPrice: price.buyPrice,
      sellPrice: price.sellPrice,
      currency: price.currency,
      source: price.source
    };
    this.editingId.set(price.id);
    this.modalError.set(null);
    this.modalMode.set('edit');
  }

  closeModal(): void {
    this.modalMode.set(null);
    this.editingId.set(null);
    this.isSubmitting.set(false);
    this.modalError.set(null);
  }

  // ─── CRUD operations ─────────────────────────────────────

  submitForm(): void {
    if (!this.isFormValid()) return;

    this.isSubmitting.set(true);
    this.modalError.set(null);

    const mode = this.modalMode();

    if (mode === 'create') {
      this.goldService.create(this.form).subscribe({
        next: () => {
          this.closeModal();
          this.dataChanged.emit();
        },
        error: (err) => {
          this.modalError.set(err.message ?? 'Tạo thất bại');
          this.isSubmitting.set(false);
        }
      });
    } else if (mode === 'edit') {
      const id = this.editingId()!;
      this.goldService.update(id, this.form).subscribe({
        next: () => {
          this.closeModal();
          this.dataChanged.emit();
        },
        error: (err) => {
          this.modalError.set(err.message ?? 'Cập nhật thất bại');
          this.isSubmitting.set(false);
        }
      });
    }
  }

  confirmDelete(id: number): void {
    this.deletingId.set(id);
  }

  cancelDelete(): void {
    this.deletingId.set(null);
  }

  executeDelete(): void {
    const id = this.deletingId();
    if (id === null) return;

    this.goldService.delete(id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.dataChanged.emit();
      },
      error: () => {
        this.deletingId.set(null);
      }
    });
  }

  // ─── Helpers ─────────────────────────────────────────────

  isFormValid(): boolean {
    return (
      !!this.form.goldType.trim() &&
      this.form.buyPrice > 0 &&
      this.form.sellPrice > 0 &&
      !!this.form.currency.trim() &&
      !!this.form.source.trim()
    );
  }

  formatPrice(value: number): string {
    return new Intl.NumberFormat('vi-VN').format(value);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString('vi-VN', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  private emptyForm(): CreateGoldPriceRequest {
    return { goldType: '', buyPrice: 0, sellPrice: 0, currency: 'VND', source: '' };
  }
}

