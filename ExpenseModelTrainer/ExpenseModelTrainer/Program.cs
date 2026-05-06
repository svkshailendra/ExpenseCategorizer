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


        //metrics
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy}");
        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy}");
        Console.WriteLine($"LogLoss: {metrics.LogLoss}");
        Console.WriteLine($"LogLossReduction: {metrics.LogLossReduction}");

        //Save model

        string modelPath = Path.Combine(AppContext.BaseDirectory, "expenseModel.zip");
        mlContext.Model.Save(model, data.Schema, modelPath);        
        Console.WriteLine($"Model saved to: {modelPath}");


        // Confusion Matrix
        Console.WriteLine("Confusion Matrix:");
        var cm = metrics.ConfusionMatrix;
        for (int i = 0; i < cm.NumberOfClasses; i++)
        {
            for (int j = 0; j < cm.NumberOfClasses; j++)
            {
                Console.Write($"{cm.Counts[i][j]} ");
            }
            Console.WriteLine();
        }

        /// Calculate per-class precision, recall, F1
        Console.WriteLine("Per-Class Metrics:");
        for (int i = 0; i < cm.NumberOfClasses; i++)
        {
            double truePositives = cm.Counts[i][i];
            double falseNegatives = cm.Counts[i].Sum() - truePositives;
            double falsePositives = 0;

            for (int j = 0; j < cm.NumberOfClasses; j++)
            {
                if (j != i)
                    falsePositives += cm.Counts[j][i];
            }

            double precision = truePositives / (truePositives + falsePositives);
            double recall = truePositives / (truePositives + falseNegatives);
            double f1 = 2 * (precision * recall) / (precision + recall);

            Console.WriteLine($"Class {i}:");
            Console.WriteLine($"  Precision: {precision:F2}");
            Console.WriteLine($"  Recall: {recall:F2}");
            Console.WriteLine($"  F1 Score: {f1:F2}");
        }
    }
}
