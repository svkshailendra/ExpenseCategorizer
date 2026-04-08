using Microsoft.ML;
using Microsoft.ML.Data;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

namespace ExpenseCategorizerFunction.Services
{
    //public class MlNetService
    //{
    // TODO: Load ML.NET model here
    //public string PredictCategory(string text)
    //{
    //    // Simple placeholder logic
    //    if (text.Contains("flight") || text.Contains("hotel"))
    //        return "Travel";
    //    if (text.Contains("medicine") || text.Contains("clinic"))
    //        return "Medical";
    //    if (text.Contains("laptop") || text.Contains("software"))
    //        return "Office Supplies";
    //    return "Other";
    //}
    //}
    public class MlNetService : IMlNetService
    {
        private readonly PredictionEngine<ExpenseInputTrain, ExpensePrediction> _engine;

        public MlNetService()
        {
            var mlContext = new MLContext();

            // Use relative path to Models folder
            var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "expenseModel.zip");

            var model = mlContext.Model.Load(modelPath, out _);
            _engine = mlContext.Model.CreatePredictionEngine<ExpenseInputTrain, ExpensePrediction>(model, ignoreMissingColumns:true);

             
        }

        public string PredictCategory(string text)
        {
            var prediction = _engine.Predict(new ExpenseInputTrain { Description = text });
            return prediction.Category;
        }
    }

    //Training Class
    public class ExpenseInputTrain
    {
        [LoadColumn(0)]
        public string Description { get; set; }

        [LoadColumn(1)]
        [ColumnName("Label")]
        public string Category { get; set; }
    }


    //Prediction Class
    public class ExpenseInputPrediction
    {
        public string Description { get; set; }
    }

    public class ExpensePrediction
    {
        [ColumnName("PredictedLabel")]
        public string Category { get; set; }
    }

}