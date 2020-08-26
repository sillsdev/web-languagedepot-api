import { Component, OnInit, Input } from '@angular/core';
import { FormControl, FormGroup, ValidatorFn, ValidationErrors } from '@angular/forms';
import { NoticeService } from '../services/notice.service';

@Component({
  selector: 'app-change-password-form',
  templateUrl: './change-password-form.component.html',
  styleUrls: ['./change-password-form.component.scss']
})
export class ChangePasswordFormComponent implements OnInit {
  @Input()
  showOldPasswordField = true;
  formControl: FormGroup;

  constructor(private readonly notice: NoticeService) { }

  passwordsMustMatch: ValidatorFn = (control: FormGroup): ValidationErrors | null => {
    const password1 = control.get('newPasswordControl');
    const password2 = control.get('confirmNewPasswordControl');

    return password1.value === password2.value ? null : { confirmMismatch: true };
  }

  ngOnInit(): void {
    const fields =
      this.showOldPasswordField ? {
        oldPasswordControl: new FormControl(''),
        newPasswordControl: new FormControl(''),
        confirmNewPasswordControl: new FormControl(''),
      } : {
        newPasswordControl: new FormControl(''),
        confirmNewPasswordControl: new FormControl(''),
      };
    this.formControl = new FormGroup(fields, { validators: [this.passwordsMustMatch] });
  }

  onSubmit(): void {
    const msg = 'Password change request would be submitted with new password: ' + this.formControl.get('newPasswordControl').value;
    // To show a brief notification that doesn't require interaction:
    // this.notice.show(msg);
    // To show a notification that requires clicking on the "Dismiss" button:
    this.notice.showMessageDialog(() => msg);
  }
}
