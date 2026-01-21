namespace PgManagement_WebApi.DTOs.Pagination
{
    public class PageResultsDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
