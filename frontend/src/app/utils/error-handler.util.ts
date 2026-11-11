import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: unknown, defaultMessage = '發生未預期的錯誤，請稍後再試'): string {
  if (!(err instanceof HttpErrorResponse)) {
    return defaultMessage;
  }

  // 1. 後端自訂的回傳格式: { message: "...", isSuccess: false }
  if (err.error?.message && typeof err.error.message === 'string') {
    return err.error.message;
  }

  // 2. ASP.NET Core ModelState 400 驗證錯誤格式: { errors: { field: ["error1", "error2"] }, title: "..." }
  if (err.error?.errors && typeof err.error.errors === 'object') {
    const errorList: string[] = [];
    for (const key of Object.keys(err.error.errors)) {
      const fieldErrors = err.error.errors[key];
      if (Array.isArray(fieldErrors)) {
        errorList.push(...fieldErrors);
      } else if (typeof fieldErrors === 'string') {
        errorList.push(fieldErrors);
      }
    }
    if (errorList.length > 0) {
      return errorList.join('；');
    }
  }

  // 3. ProblemDetails title
  if (err.error?.title && typeof err.error.title === 'string') {
    return err.error.title;
  }

  // 4. 文字純字串回應
  if (typeof err.error === 'string' && err.error.trim().length > 0) {
    return err.error;
  }

  // 5. HTTP 狀態碼對應常見訊息
  if (err.status === 0) {
    return '無法連線至伺服器，請檢查網路連線或確認後端服務是否已啟動';
  }
  if (err.status === 400) {
    return '傳送的資料格式不正確，請檢查輸入內容';
  }
  if (err.status === 401) {
    return '帳號或密碼不正確';
  }
  if (err.status === 403) {
    return '存取被拒絕，權限不足';
  }
  if (err.status === 409) {
    return '資料衝突，該帳號或信箱可能已存在';
  }
  if (err.status >= 500) {
    return '伺服器內部發生錯誤，請聯絡系統管理員或稍後再試';
  }

  return defaultMessage;
}
