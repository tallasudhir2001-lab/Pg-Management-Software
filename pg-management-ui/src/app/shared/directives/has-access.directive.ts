import { Directive, Input, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { PermissionService } from '../../core/services/permission.service';

@Directive({
  selector: '[hasAccess]',
  standalone: true,
})
export class HasAccessDirective implements OnInit {
  @Input('hasAccess') accessKey = '';

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private permissionService: PermissionService
  ) {}

  ngOnInit(): void {
    if (this.permissionService.hasAccess(this.accessKey)) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainer.clear();
    }
  }
}
