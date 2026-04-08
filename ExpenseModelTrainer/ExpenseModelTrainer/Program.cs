// See https://aka.ms/new-console-template for more information
using Microsoft.ML;
using Microsoft.ML.Data;

public class ExpenseInputTrain
{
    [LoadColumn(0)]
    public string Description { get; set; }

    [LoadColumn(1)]
    [ColumnName("Label")]
    public string Category { get; set; }
}

public class ExpenseInputPrediction
{
    public string Description { get; set; }
}

public class ExpensePrediction
{
    [ColumnName("PredictedLabel")]
    public string Category { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        var mlContext = new MLContext();

        // Load data
        string fullPath = Path.Combine(AppContext.BaseDirectory, "expenses.csv");
        var data = mlContext.Data.LoadFromTextFile<ExpenseInputTrain>(path: fullPath, hasHeader: true,
            separatorChar: ',');

        // Split train/test
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        //Build pipeline

        var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(ExpenseInputTrain.Description))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Label"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // Train
        var model = pipeline.Fit(split.TrainSet);

        // Evaluate
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy}");


        //Save model

       mlContext.Model.Save(model, data.Schema, "expenseModel.zip");
       Console.WriteLine("Model saved to expenseModel.zip");


    }
}
