import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'Đã xảy ra lỗi không xác định';

      switch (error.status) {
        case 400:
          message = 'Dữ liệu không hợp lệ: ' + JSON.stringify(error.error?.errors ?? error.error);
          break;
        case 404:
          message = 'Không tìm thấy dữ liệu';
          break;
        case 500:
          message = 'Lỗi server. Vui lòng thử lại sau.';
          break;
        case 0:
          message = 'Không thể kết nối đến server. Backend có đang chạy không?';
          break;
      }

      console.error(`[GoldWatch HTTP ${error.status}]`, message, error);
      return throwError(() => ({ status: error.status, message }));
    })
  );
};

