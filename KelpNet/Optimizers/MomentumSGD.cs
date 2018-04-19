﻿using System;
using KelpNet.Common;
using KelpNet.Common.Optimizers;

namespace KelpNet.Optimizers
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable) a momentum sgd. </summary>
    ///
    /// <seealso cref="T:KelpNet.Common.Optimizers.Optimizer"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class MomentumSGD : Optimizer
    {
        /// <summary>   The learning rate. </summary>
        public Real LearningRate;
        /// <summary>   The momentum. </summary>
        public Real Momentum;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the KelpNet.Optimizers.MomentumSGD class.
        /// </summary>
        ///
        /// <param name="learningRate"> (Optional) The learning rate. </param>
        /// <param name="momentum">     (Optional) The momentum. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MomentumSGD(double learningRate = 0.01, double momentum = 0.9)
        {
            LearningRate = learningRate;
            Momentum = momentum;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds a function parameters. </summary>
        ///
        /// <param name="functionParameters">   Options for controlling the function. </param>
        ///
        /// <seealso cref="M:KelpNet.Common.Optimizers.Optimizer.AddFunctionParameters(NdArray[])"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal override void AddFunctionParameters(NdArray[] functionParameters)
        {
            foreach (NdArray functionParameter in functionParameters)
            {
                OptimizerParameters.Add(new MomentumSGDParameter(functionParameter, this));
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable) a momentum sgd parameter. </summary>
    ///
    /// <seealso cref="T:KelpNet.Common.Optimizers.OptimizerParameter"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    class MomentumSGDParameter : OptimizerParameter
    {
        /// <summary>   The optimizer. </summary>
        private readonly MomentumSGD optimizer;
        /// <summary>   A Real[] to process. </summary>
        private readonly Real[] v;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the KelpNet.Optimizers.MomentumSGDParameter class.
        /// </summary>
        ///
        /// <param name="functionParameter">    The function parameter. </param>
        /// <param name="optimizer">            The optimizer. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MomentumSGDParameter(NdArray functionParameter, MomentumSGD optimizer) : base(functionParameter)
        {
            v = new Real[functionParameter.Data.Length];
            this.optimizer = optimizer;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Updates the function parameters. </summary>
        ///
        /// <seealso cref="M:KelpNet.Common.Optimizers.OptimizerParameter.UpdateFunctionParameters()"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override void UpdateFunctionParameters()
        {
            for (int i = 0; i < FunctionParameter.Data.Length; i++)
            {
                v[i] *= optimizer.Momentum;
                v[i] -= optimizer.LearningRate * FunctionParameter.Grad[i];

                FunctionParameter.Data[i] += v[i];
            }
        }
    }

}
