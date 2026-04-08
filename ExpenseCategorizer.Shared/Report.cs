using ExpenseCategorizer.Shared;

namespace ExpenseCategorizerFunction
{
    public class Report
    {
        public decimal TotalExpenses { get; set; }
        public List<CategorySummary> Categories { get; set; } = new();
    }
}