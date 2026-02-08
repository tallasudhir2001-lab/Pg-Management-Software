namespace PgManagement_WebApi.Models
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Expense> Expenses { get; set; }
    }

}
