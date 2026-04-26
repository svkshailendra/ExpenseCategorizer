using System.Text;
using ExpenseCategorizer.Shared;

namespace ExpenseCategorizerFunction
{
    public class ReportService
    {
        private readonly DatabaseService _dbService;

        public ReportService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task<Report> GenerateReportAsync(string userid)
        {
            var expenses = await _dbService.GetAllExpensesByUserAsync(userid);

            var total = expenses.Sum(e => e.Amount);
            var categories = expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategorySummary
                {
                    Name = g.Key,
                    Amount = g.Sum(e => e.Amount)
                })
                .ToList();

            return new Report
            {
                TotalExpenses = total,
                Categories = categories
            };
        }
    }
}