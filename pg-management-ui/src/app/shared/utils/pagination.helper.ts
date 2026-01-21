import { Router, ActivatedRoute } from '@angular/router';

export class PaginationHelper {

  static updateQueryParams(
    router: Router,
    route: ActivatedRoute,
    params: {
      page?: number;
      search?: string;
      status?: string;
      ac?: string;
    }
  ): void {
    router.navigate([], {
      relativeTo: route,
      queryParams: {
        page: params.page,
        search: params.search || null,
        status: params.status || null,
        ac: params.ac || null
      },
      queryParamsHandling: 'merge'
    });
  }
  
}
