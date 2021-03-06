﻿using System;
using System.Linq;
using KelpNet.Common;
using KelpNet.Common.Functions.Container;
using KelpNet.Common.Tools;
using KelpNet.Functions.Activations;
using KelpNet.Functions.Connections;
using KelpNet.Functions.Normalization;
using KelpNet.Loss;
using KelpNet.Optimizers;
using KelpNetTester.TestData;

namespace KelpNetTester.Tests
{
    using ReflectSoftware.Insight;

    //Decoupled Neural Interfaces using Synthetic Gradients Learning MNIST (Handwritten Characters) by Gradients
    // http://ralo23.hatenablog.com/entry/2016/10/22/233405
    class Test11
    {
        const int BATCH_DATA_COUNT = 200;

        // Number of exercises per generation
        const int TRAIN_DATA_COUNT = 300; // = 60000 / 20

        // number of data at performance evaluation
        const int TEST_DATA_COUNT = 1000;

        public static void Run()
        {
            // Prepare MNIST data
            RILogManager.Default?.SendDebug("MNIST Data Loading...");
            MnistData mnistData = new MnistData(28);

            RILogManager.Default?.SendDebug("Training Start...");

            // Write the network configuration in FunctionStack
            FunctionStack Layer1 = new FunctionStack("Test11 Layer 1",
                new Linear(true, 28 * 28, 256, name: "l1 Linear"),
                new BatchNormalization(true, 256, name: "l1 Norm"),
                new ReLU(name: "l1 ReLU")
            );

            FunctionStack Layer2 = new FunctionStack("Test11 Layer 2",
                new Linear(true, 256, 256, name: "l2 Linear"),
                new BatchNormalization(true, 256, name: "l2 Norm"),
                new ReLU(name: "l2 ReLU")
            );

            FunctionStack Layer3 = new FunctionStack("Test11 Layer 3",
                new Linear(true, 256, 256, name: "l3 Linear"),
                new BatchNormalization(true, 256, name: "l3 Norm"),
                new ReLU(name: "l3 ReLU")
            );

            FunctionStack Layer4 = new FunctionStack("Test11 Layer 4",
                new Linear(true, 256, 10, name: "l4 Linear")
            );

            // Function stack itself is also stacked as Function
            FunctionStack nn = new FunctionStack
            ("Test11",
                Layer1,
                Layer2,
                Layer3,
                Layer4
            );

            FunctionStack DNI1 = new FunctionStack("Test11 DNI1",
                new Linear(true, 256, 1024, name: "DNI1 Linear1"),
                new BatchNormalization(true, 1024, name: "DNI1 Norm1"),
                new ReLU(name: "DNI1 ReLU1"),
                new Linear(true, 1024, 1024, name: "DNI1 Linear2"),
                new BatchNormalization(true, 1024, name: "DNI1 Norm2"),
                new ReLU(name: "DNI1 ReLU2"),
                new Linear(true, 1024, 256, initialW: new Real[1024, 256], name: "DNI1 Linear3")
            );

            FunctionStack DNI2 = new FunctionStack("Test11 DNI2",
                new Linear(true, 256, 1024, name: "DNI2 Linear1"),
                new BatchNormalization(true, 1024, name: "DNI2 Norm1"),
                new ReLU(name: "DNI2 ReLU1"),
                new Linear(true, 1024, 1024, name: "DNI2 Linear2"),
                new BatchNormalization(true, 1024, name: "DNI2 Norm2"),
                new ReLU(name: "DNI2 ReLU2"),
                new Linear(true, 1024, 256, initialW: new Real[1024, 256], name: "DNI2 Linear3")
            );

            FunctionStack DNI3 = new FunctionStack("Test11 DNI3",
                new Linear(true, 256, 1024, name: "DNI3 Linear1"),
                new BatchNormalization(true, 1024, name: "DNI3 Norm1"),
                new ReLU(name: "DNI3 ReLU1"),
                new Linear(true, 1024, 1024, name: "DNI3 Linear2"),
                new BatchNormalization(true, 1024, name: "DNI3 Norm2"),
                new ReLU(name: "DNI3 ReLU2"),
                new Linear(true, 1024, 256, initialW: new Real[1024, 256], name: "DNI3 Linear3")
            );

            //optimizer
            Layer1.SetOptimizer(new Adam());
            Layer2.SetOptimizer(new Adam());
            Layer3.SetOptimizer(new Adam());
            Layer4.SetOptimizer(new Adam());
            DNI1.SetOptimizer(new Adam());
            DNI2.SetOptimizer(new Adam());
            DNI3.SetOptimizer(new Adam());

            // Three generations learning
            for (int epoch = 0; epoch < 20; epoch++)
            {
                RILogManager.Default?.SendDebug("epoch " + (epoch + 1));

                Real totalLoss = 0;
                Real DNI1totalLoss = 0;
                Real DNI2totalLoss = 0;
                Real DNI3totalLoss = 0;

                long totalLossCount = 0;
                long DNI1totalLossCount = 0;
                long DNI2totalLossCount = 0;
                long DNI3totalLossCount = 0;

                // how many times to run the batch
                for (int i = 1; i < TRAIN_DATA_COUNT + 1; i++)
                {
                    // Get data randomly from the training data
                    TestDataSet datasetX = mnistData.GetRandomXSet(BATCH_DATA_COUNT, 28, 28);

                    // Run first tier
                    NdArray[] layer1ForwardResult = Layer1.Forward(true, datasetX.Data);

                    // Obtain the slope of the first layer
                    NdArray[] DNI1Result = DNI1.Forward(true, layer1ForwardResult);

                    // Apply the slope of the first layer
                    layer1ForwardResult[0].Grad = DNI1Result[0].Data.ToArray();

                    // Update first layer
                    Layer1.Backward(true, layer1ForwardResult);
                    layer1ForwardResult[0].ParentFunc = null; // Backward was executed and cut off calculation graph
                    Layer1.Update();

                    // Run Layer 2
                    NdArray[] layer2ForwardResult = Layer2.Forward(true, layer1ForwardResult);

                    // Get the inclination of the second layer
                    NdArray[] DNI2Result = DNI2.Forward(true, layer2ForwardResult);

                    // Apply the slope of the second layer
                    layer2ForwardResult[0].Grad = DNI2Result[0].Data.ToArray();

                    // Update layer 2
                    Layer2.Backward(true, layer2ForwardResult);
                    layer2ForwardResult[0].ParentFunc = null;

                    // Learn DNI for first tier
                    Real DNI1loss = new MeanSquaredError().Evaluate(DNI1Result, new NdArray(layer1ForwardResult[0].Grad, DNI1Result[0].Shape, DNI1Result[0].BatchCount));

                    Layer2.Update();

                    DNI1.Backward(true, DNI1Result);
                    DNI1.Update();

                    DNI1totalLoss += DNI1loss;
                    DNI1totalLossCount++;

                    // run layer 3
                    NdArray[] layer3ForwardResult = Layer3.Forward(true, layer2ForwardResult);

                    // Get the inclination of the third layer
                    NdArray[] DNI3Result = DNI3.Forward(true, layer3ForwardResult);

                    // Apply the slope of the third layer
                    layer3ForwardResult[0].Grad = DNI3Result[0].Data.ToArray();

                    // Update layer 3
                    Layer3.Backward(true, layer3ForwardResult);
                    layer3ForwardResult[0].ParentFunc = null;

                    // Run DNI learning for layer 2
                    Real DNI2loss = new MeanSquaredError().Evaluate(DNI2Result, new NdArray(layer2ForwardResult[0].Grad, DNI2Result[0].Shape, DNI2Result[0].BatchCount));

                    Layer3.Update();

                    DNI2.Backward(true, DNI2Result);
                    DNI2.Update();

                    DNI2totalLoss += DNI2loss;
                    DNI2totalLossCount++;

                    // run layer 4
                    NdArray[] layer4ForwardResult = Layer4.Forward(true, layer3ForwardResult);

                    // Obtain the slope of the fourth layer
                    Real sumLoss = new SoftmaxCrossEntropy().Evaluate(layer4ForwardResult, datasetX.Label);

                    // Update fourth layer
                    Layer4.Backward(true, layer4ForwardResult);
                    layer4ForwardResult[0].ParentFunc = null;

                    totalLoss += sumLoss;
                    totalLossCount++;

                    // Run DNI learning for layer 3
                    Real DNI3loss = new MeanSquaredError().Evaluate(DNI3Result, new NdArray(layer3ForwardResult[0].Grad, DNI3Result[0].Shape, DNI3Result[0].BatchCount));

                    Layer4.Update();

                    DNI3.Backward(true, DNI3Result);
                    DNI3.Update();

                    DNI3totalLoss += DNI3loss;
                    DNI3totalLossCount++;

                    RILogManager.Default?.SendDebug("batch count " + i + "/" + TRAIN_DATA_COUNT);
                    RILogManager.Default?.SendDebug("total loss " + totalLoss / totalLossCount);
                    RILogManager.Default?.SendDebug("local loss " + sumLoss);

                    RILogManager.Default?.SendDebug("DNI1 total loss " + DNI1totalLoss / DNI1totalLossCount);
                    RILogManager.Default?.SendDebug("DNI2 total loss " + DNI2totalLoss / DNI2totalLossCount);
                    RILogManager.Default?.SendDebug("DNI3 total loss " + DNI3totalLoss / DNI3totalLossCount);

                    RILogManager.Default?.SendDebug("DNI1 local loss " + DNI1loss);
                    RILogManager.Default?.SendDebug("DNI2 local loss " + DNI2loss);
                    RILogManager.Default?.SendDebug("DNI3 local loss " + DNI3loss);

                    // Test the accuracy if you move the batch 20 times
                    if (i % 20 == 0)
                    {
                        RILogManager.Default?.SendDebug("Testing...");

                        // Get data randomly from test data
                        TestDataSet datasetY = mnistData.GetRandomYSet(TEST_DATA_COUNT, 28);

                        // Run test
                        Real accuracy = Trainer.Accuracy(nn, datasetY.Data, datasetY.Label);
                        RILogManager.Default?.SendDebug("accuracy " + accuracy);
                    }
                }
            }
        }
    }
}
