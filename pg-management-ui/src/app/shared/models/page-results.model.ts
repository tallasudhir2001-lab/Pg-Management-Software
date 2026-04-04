export interface PagedResults<T>{
    items: T[];
    totalCount: number;
    totalAmount?: number;
}