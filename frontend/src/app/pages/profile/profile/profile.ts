import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../auth/auth.service';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.html',
  styles: ``,
})
export class ProfileComponent {
  authService = inject(AuthService);
  fb = inject(FormBuilder);

  isChangePasswordModalOpen = signal(false);
  isSubmitting = signal(false);
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  user = computed(() => {
    const currentUser = this.authService.user();
    if (currentUser) {
      return {
        ...currentUser,
        initials: `${currentUser.firstName[0]}${currentUser.lastName[0]}`.toUpperCase()
      };
    }
    return null;
  });

  changePasswordForm = this.fb.group({
    currentPassword: ['', [Validators.required, Validators.minLength(6)]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
  }, { validators: this.passwordMatchValidator });

  private passwordMatchValidator(form: any) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');
    if (newPassword && confirmPassword && newPassword.value !== confirmPassword.value) {
      confirmPassword.setErrors({ mismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  openChangePasswordModal() {
    this.isChangePasswordModalOpen.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }

  closeChangePasswordModal() {
    this.isChangePasswordModalOpen.set(false);
    this.changePasswordForm.reset();
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }

  submitChangePassword() {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.changePasswordForm.invalid) {
      this.changePasswordForm.markAllAsTouched();
      return;
    }

    const formValue = this.changePasswordForm.getRawValue();
    const currentUser = this.user();

    if (!currentUser) {
      this.errorMessage.set('Utilisateur non trouvé');
      return;
    }

    this.isSubmitting.set(true);

    this.authService.changePassword(currentUser.email, formValue.currentPassword!, formValue.newPassword!)
      .subscribe({
        next: (response) => {
          this.successMessage.set(response.message);
          this.changePasswordForm.reset();
          setTimeout(() => {
            this.closeChangePasswordModal();
          }, 2000);
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.message || 'Erreur lors du changement de mot de passe');
          this.isSubmitting.set(false);
        },
        complete: () => {
          this.isSubmitting.set(false);
        }
      });
  }
}
