﻿using System;
using Microsoft.ML.Data;
using Microsoft.ML.StaticPipe;

namespace Microsoft.ML.Samples.Static
{
    public class AveragedPerceptronBinaryClassificationExample
    {
        public static void AveragedPerceptronBinaryClassification()
        {
            // Downloading a classification dataset from github.com/dotnet/machinelearning.
            // It will be stored in the same path as the executable
            string dataFilePath = SamplesUtils.DatasetUtils.DownloadAdultDataset();

            // Data Preview
            // 1. Column [Label]: IsOver50K (boolean)
            // 2. Column: workclass (text/categorical)
            // 3. Column: education (text/categorical)
            // 4. Column: marital-status (text/categorical)
            // 5. Column: occupation (text/categorical)
            // 6. Column: relationship (text/categorical)
            // 7. Column: ethnicity (text/categorical)
            // 8. Column: sex (text/categorical)
            // 9. Column: native-country-region (text/categorical)
            // 10. Column: age (numeric)
            // 11. Column: fnlwgt (numeric)
            // 12. Column: education-num (numeric)
            // 13. Column: capital-gain (numeric)
            // 14. Column: capital-loss (numeric)
            // 15. Column: hours-per-week (numeric)

            // Creating the ML.Net IHostEnvironment object, needed for the pipeline
            var mlContext = new MLContext();

            // Creating Data Reader with the initial schema based on the format of the data
            var reader = TextLoader.CreateReader(
                mlContext,
                c => (
                    Age: c.LoadFloat(0),
                    Workclass: c.LoadText(1),
                    Fnlwgt: c.LoadFloat(2),
                    Education: c.LoadText(3),
                    EducationNum: c.LoadFloat(4),
                    MaritalStatus: c.LoadText(5),
                    Occupation: c.LoadText(6),
                    Relationship: c.LoadText(7),
                    Ethnicity: c.LoadText(8),
                    Sex: c.LoadText(9),
                    CapitalGain: c.LoadFloat(10),
                    CapitalLoss: c.LoadFloat(11),
                    HoursPerWeek: c.LoadFloat(12),
                    NativeCountry: c.LoadText(13),
                    IsOver50K: c.LoadBool(14)),
                separator: ',',
                hasHeader: true);

            // Read the data, and leave 10% out, so we can use them for testing
            var data = reader.Read(dataFilePath);
            var (trainData, testData) = mlContext.BinaryClassification.TrainTestSplit(data, testFraction: 0.1);

            // Create the Estimator
            var learningPipeline = reader.MakeNewEstimator()
                .Append(row => (
                        Features: row.Age.ConcatWith(
                            row.EducationNum,
                            row.MaritalStatus.OneHotEncoding(),
                            row.Occupation.OneHotEncoding(),
                            row.Relationship.OneHotEncoding(),
                            row.Ethnicity.OneHotEncoding(),
                            row.Sex.OneHotEncoding(),
                            row.HoursPerWeek,
                            row.NativeCountry.OneHotEncoding().SelectFeaturesBasedOnCount(count: 10)),
                        Label: row.IsOver50K))
                .Append(row => (
                        Features: row.Features.Normalize(),
                        Label: row.Label,
                        Score: mlContext.BinaryClassification.Trainers.AveragedPerceptron(
                            row.Label,
                            row.Features,
                            learningRate: 0.1f,
                            numIterations: 100)))
                .Append(row => (
                    Label: row.Label,
                    Score: row.Score,
                    PredictedLabel: row.Score.predictedLabel));

            // Fit this Pipeline to the Training Data
            var model = learningPipeline.Fit(trainData);

            // Evaluate how the model is doing on the test data
            var dataWithPredictions = model.Transform(testData);

            var metrics = mlContext.BinaryClassification.Evaluate(dataWithPredictions, row => row.Label, row => row.Score);

            Console.WriteLine($"Accuracy: {metrics.Accuracy}"); // 0.83
            Console.WriteLine($"AUC: {metrics.Auc}"); // 0.88
            Console.WriteLine($"F1 Score: {metrics.F1Score}"); // 0.63

            Console.WriteLine($"Negative Precision: {metrics.NegativePrecision}"); // 0.89
            Console.WriteLine($"Negative Recall: {metrics.NegativeRecall}"); // 0.89
            Console.WriteLine($"Positive Precision: {metrics.PositivePrecision}"); // 0.64
            Console.WriteLine($"Positive Recall: {metrics.PositiveRecall}"); // 0.62    
        }
    }
}