export type ToastType = 'success' | 'error' | 'info';

export interface ToastModel {
  message: string;
  type: ToastType;
}
