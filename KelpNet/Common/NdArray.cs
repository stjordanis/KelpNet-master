﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using KelpNet.Common.Functions;
using KelpNet.Functions.Mathmetrics.BasicMath;

namespace KelpNet.Common
{
    /// <summary>   (Serializable) a nd array. </summary>
    [Serializable]
    [DebuggerDisplay("{Name + ToString(\"Size\")}", Type = "{\"NdArray\" + ToString(\"Size\")}")]
    public class NdArray
    {
        /// <summary>   The name. </summary>
        public string Name = "NdArray";

        /// <summary>   The data. </summary>
        public Real[] Data;

        /// <summary>   The graduated. </summary>
        [NonSerialized]
        public Real[] Grad;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   size of each dimension of this NdArray. </summary>
        ///
        /// <value> The shape. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int[] Shape { private set; get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Length calculated from Shape, different from Length of Data. </summary>
        ///
        /// <value> The length. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Length { private set; get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Count the number of times used by the function to try the timing of the Backward operation.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        [NonSerialized]
        public int UseCount;

        /// <summary>   If created by itself, save the function here. </summary>
        [NonSerialized]
        public Function ParentFunc;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Indicates the number of batches executed collectively in each function, used in the discount
        /// in the Loss function.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int BatchCount;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Count the number of Backwards executed without updating and use it when running Optimizer.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        [NonSerialized]
        public int TrainCount;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the KelpNet.Common.NdArray class. </summary>
        ///
        /// <param name="data">         The data. </param>
        /// <param name="parentFunc">   (Optional) The parent function. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray(Array data, Function parentFunc = null)
        {
            Real[] resultData = Real.GetArray(data);

            int[] resultShape = new int[data.Rank];

            for (int i = 0; i < data.Rank; i++)
            {
                resultShape[i] = data.GetLength(i);
            }

            Data = resultData;
            Shape = resultShape;
            Length = Data.Length;
            Grad = new Real[Length];
            BatchCount = 1;
            TrainCount = 0;
            ParentFunc = parentFunc;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the KelpNet.Common.NdArray class. </summary>
        ///
        /// <param name="shape">    A variable-length parameters list containing shape. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray(params int[] shape)
        {
            Data = new Real[ShapeToArrayLength(shape)];
            Shape = shape.ToArray();
            Length = Data.Length;
            BatchCount = 1;
            Grad = new Real[Length];
            TrainCount = 0;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the KelpNet.Common.NdArray class. </summary>
        ///
        /// <param name="data">         The data. </param>
        /// <param name="shape">        The shape. </param>
        /// <param name="batchCount">   (Optional) Number of batches. </param>
        /// <param name="parentFunc">   (Optional) The parent function. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray(Real[] data, int[] shape, int batchCount = 1, Function parentFunc = null)
        {
            Shape = shape.ToArray();
            Length = ShapeToArrayLength(Shape);
            BatchCount = batchCount;
            Data = data.ToArray();
            Grad = new Real[Length * batchCount];
            TrainCount = 0;
            ParentFunc = parentFunc;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the KelpNet.Common.NdArray class. </summary>
        ///
        /// <param name="shape">        The shape. </param>
        /// <param name="batchCount">   Number of batches. </param>
        /// <param name="parentFunc">   (Optional) The parent function. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray(int[] shape, int batchCount, Function parentFunc = null)
        {
            Shape = shape.ToArray();
            Length = ShapeToArrayLength(Shape);
            BatchCount = batchCount;
            Data = new Real[Length * batchCount];
            Grad = new Real[Length * batchCount];
            TrainCount = 0;
            ParentFunc = parentFunc;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Register array array as batch. </summary>
        ///
        /// <param name="arrays">       The arrays. </param>
        /// <param name="parentFunc">   (Optional) The parent function. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray FromArrays(Array[] arrays, Function parentFunc = null)
        {
            int[] resultShape = new int[arrays[0].Rank];

            for (int i = 0; i < arrays[0].Rank; i++)
            {
                resultShape[i] = arrays[0].GetLength(i);
            }

            int length = arrays[0].Length;
            Real[] result = new Real[length * arrays.Length];

            for (int i = 0; i < arrays.Length; i++)
            {
                Array.Copy(Real.GetArray(arrays[i]), 0, result, length * i, length);
            }

            return new NdArray(result, resultShape, arrays.Length, parentFunc);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts. </summary>
        ///
        /// <param name="data">         The data. </param>
        /// <param name="shape">        The shape. </param>
        /// <param name="batchCount">   Number of batches. </param>
        /// <param name="parentFunc">   (Optional) The parent function. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray Convert(Real[] data, int[] shape, int batchCount, Function parentFunc = null)
        {
            return new NdArray(shape, batchCount, parentFunc) { Data = data };
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Zeros like. </summary>
        ///
        /// <param name="baseArray">    Array of bases. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray ZerosLike(NdArray baseArray)
        {
            return new NdArray(baseArray.Shape.ToArray(), baseArray.BatchCount);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Because the indexer is not too early, I do not recommend using it when accessing frequently.
        /// Please divide it for debugging purpose。.
        /// </summary>
        ///
        /// <param name="batchcount">   The batchcount. </param>
        /// <param name="indices">      A variable-length parameters list containing indices. </param>
        ///
        /// <returns>   The indexed item. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Real this[int batchcount, params int[] indices]
        {
            get => Data[GetLocalIndex(batchcount, indices)];
            set => Data[GetLocalIndex(batchcount, indices)] = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Reshapes the given shape. </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="shape">    A variable-length parameters list containing shape. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Reshape(params int[] shape)
        {
            int val = 0;
            int dimension = Length;

            //Calculate / / -1 specification
            if (shape.Contains(-1))
            {
                int minusIndex = -1;

                for (int i = 0; i < shape.Length; i++)
                {
                    if (shape[i] != -1)
                    {
                        val += Length % shape[i];

                        if (val == 0)
                        {
                            dimension /= shape[i];
                        }
                        else
                        {
                            throw new Exception("Element specification is incorrect");
                        }
                    }
                    else
                    {
                        if (minusIndex != -1)
                        {
                            throw new Exception("More than one -1 is specified");
                        }

                        minusIndex = i;
                    }
                }

                shape[minusIndex] = dimension;
            }
#if DEBUG
            else if (Length != ShapeToArrayLength(shape)) throw new Exception("The size of the specified Shape is not equal to the current Data.Length");
#endif

            Shape = shape.ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Dispatch the array that is gathered in a batch and eject it. </summary>
        ///
        /// <returns>   A NdArray[]. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray[] DivideArrays()
        {
            NdArray[] result = new NdArray[BatchCount];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetSingleArray(i);
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   eject the array corresponding to the batch number. </summary>
        ///
        /// <param name="i">    Zero-based index of the. </param>
        ///
        /// <returns>   The single array. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray GetSingleArray(int i)
        {
            Real[] data = new Real[Length];
            Array.Copy(Data, i * Length, data, 0, Length);

            return new NdArray(data, Shape);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Shape to array length. </summary>
        ///
        /// <param name="shapes">   A variable-length parameters list containing shapes. </param>
        ///
        /// <returns>   An int. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static int ShapeToArrayLength(params int[] shapes)
        {
            return shapes.Aggregate(1, (current, shape) => current * shape);
        }

        /// <summary>   Backwards the given y coordinate. </summary>
        public void Backward()
        {
            if (ParentFunc != null)
            {
                for (int i = 0; i < Grad.Length; i++)
                {
                    Grad[i] = 1;
                }

                NdArray.Backward(this);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Backwards the given y coordinate. </summary>
        ///
        /// <param name="y">    A NdArray to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void Backward(NdArray y)
        {
            if (y.ParentFunc != null)
            {
                List<NdArray[]> prevInputs = y.ParentFunc.PrevInputs;
                NdArray[] xs = prevInputs[prevInputs.Count - 1];

                y.ParentFunc.Backward(y);

                foreach (var t in xs.Where(t => t.UseCount == 0))
                {
                    NdArray.Backward(t);
                }
            }
        }

        /// <summary>   Count up. </summary>
        public void CountUp()
        {
            TrainCount++;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Correct slope. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool Reduce()
        {
            if (TrainCount > 0)
            {
                for (int i = 0; i < Grad.Length; i++)
                {
                    Grad[i] /= TrainCount;
                }

                return true;
            }

            return false;
        }

        /// <summary>   Initialize slope. </summary>
        public void ClearGrad()
        {
            Grad = new Real[Data.Length];

            // Reset counter
            TrainCount = 0;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ///
        /// <seealso cref="M:System.Object.ToString()"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return ToString(Data);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <param name="format">   Describes the format to use. </param>
        ///
        /// <returns>   A string that represents this object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(string format)
        {
            switch (format)
            {
                case "Data":
                    return ToString(Data);

                case "Grad":
                    return ToString(Grad);

                case "Shape":
                    return "[" + string.Join(",", Shape) + "]";

                case "Size":
                    return "[" + string.Join(",", Shape) + "]" +
                           (BatchCount > 1 ? "x" + BatchCount + "batch" : string.Empty);

                case "Name":
                    return Name;

                default:
                    return Name;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <param name="datas">    The datas. </param>
        ///
        /// <returns>   A string that represents this object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(Real[] datas)
        {
            StringBuilder sb = new StringBuilder();

            int intMaxLength = 0; // maximum value of integer part
            int realMaxLength = 0; // maximum value below the decimal point
            bool isExponential = false; // Will it be exponential representation?

            foreach (var divStr in datas.Select(data => ((double)data).ToString().Split('.')))
            {
                intMaxLength = Math.Max(intMaxLength, divStr[0].Length);

                if (divStr.Length > 1)
                {
                    isExponential |= divStr[1].Contains("E");
                }

                if (realMaxLength != 8 && divStr.Length == 2)
                {
                    realMaxLength = Math.Max(realMaxLength, divStr[1].Length);

                    if (realMaxLength > 8)
                    {
                        realMaxLength = 8;
                    }
                }
            }

            // Obtain subdivision of array
            int[] commonDivisorList = new int[Shape.Length];

            // First manual acquisition
            commonDivisorList[0] = Shape[Shape.Length - 1];

            for (int i = 1; i < Shape.Length; i++)
            {
                commonDivisorList[i] = commonDivisorList[i - 1] * Shape[Shape.Length - i - 1];
            }

            if (BatchCount > 1)
            {
                sb.Append("{");
            }

            for (int batch = 0; batch < BatchCount; batch++)
            {
                int indexOffset = batch * Length;
                // First parenthesis
                for (int i = 0; i < Shape.Length; i++)
                {
                    sb.Append("[");
                }

                int closer = 0;
                for (int i = 0; i < Length; i++)
                {
                    string[] divStr;
                    divStr = isExponential ? $"{(Real) datas[indexOffset + i]:0.00000000e+00}".Split('.') : ((Real)datas[indexOffset + i]).ToString().Split('.');

                    // Align indentation with maximum number of characters
                    for (int j = 0; j < intMaxLength - divStr[0].Length; j++)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(divStr[0]);
                    if (realMaxLength != 0)
                    {
                        sb.Append(".");
                    }
                    if (divStr.Length == 2)
                    {
                        sb.Append(divStr[1].Length > 8 && !isExponential ? divStr[1].Substring(0, 8) : divStr[1]);
                        for (int j = 0; j < realMaxLength - divStr[1].Length; j++)
                        {
                            sb.Append(" ");
                        }
                    }
                    else
                    {
                        for (int j = 0; j < realMaxLength; j++)
                        {
                            sb.Append(" ");
                        }
                    }

                    // If it is perfect by investigating divisors, it outputs parentheses
                    if (i != Length - 1)
                    {
                        foreach (var commonDivisor in commonDivisorList.Where(commonDivisor => (i + 1) % commonDivisor == 0))
                        {
                            sb.Append("]");
                            closer++;
                        }

                        sb.Append(" ");

                        if ((i + 1) % commonDivisorList[0] == 0)
                        {
                            // Add a newline by closing parenthesis
                            for (int j = 0; j < closer; j++)
                            {
                                sb.Append("\n");
                            }
                            closer = 0;

                            if (BatchCount > 1) sb.Append(" ");

                            // indent before parenthesis
                            foreach (var commonDivisor in commonDivisorList.Where(commonDivisor => (i + 1) % commonDivisor != 0))
                            {
                                sb.Append(" ");
                            }
                        }

                        foreach (var commonDivisor in commonDivisorList.Where(commonDivisor => (i + 1) % commonDivisor == 0))
                        {
                            sb.Append("[");
                        }
                    }
                }

                for (int i = 0; i < Shape.Length; i++)
                {
                    sb.Append("]");
                }

                if (batch < BatchCount - 1)
                {
                    sb.Append("},\n{");
                }
            }

            if (BatchCount > 1)
            {
                sb.Append("}");
            }

            return sb.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Addition operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator +(NdArray a, NdArray b)
        {
            return new Add().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Addition operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A Real to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator +(NdArray a, Real b)
        {
            return new AddConst().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Addition operator. </summary>
        ///
        /// <param name="a">    A Real to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator +(Real a, NdArray b)
        {
            return new AddConst().Forward(b, a)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Multiplication operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator *(NdArray a, NdArray b)
        {
            return new Mul().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Multiplication operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A Real to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator *(NdArray a, Real b)
        {
            return new MulConst().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Multiplication operator. </summary>
        ///
        /// <param name="a">    A Real to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator *(Real a, NdArray b)
        {
            return new MulConst().Forward(b, a)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Subtraction operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator -(NdArray a, NdArray b)
        {
            return new Sub().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Subtraction operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A Real to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator -(NdArray a, Real b)
        {
            return new SubConst().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Subtraction operator. </summary>
        ///
        /// <param name="a">    A Real to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator -(Real a, NdArray b)
        {
            return new ConstSub().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Division operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator /(NdArray a, NdArray b)
        {
            return new Div().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Division operator. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A Real to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator /(NdArray a, Real b)
        {
            return new DivConst().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Division operator. </summary>
        ///
        /// <param name="a">    A Real to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray operator /(Real a, NdArray b)
        {
            return new ConstDiv().Forward(a, b)[0];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Implicit cast that converts the given Real[] to a NdArray. </summary>
        ///
        /// <param name="a">    A Real[] to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static implicit operator NdArray(Real[] a)
        {
            return new NdArray(a);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Implicit cast that converts the given Real to a NdArray. </summary>
        ///
        /// <param name="a">    A Real to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static implicit operator NdArray(Real a)
        {
            return new NdArray(new[] { a });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Implicit cast that converts the given long to a NdArray. </summary>
        ///
        /// <param name="a">    A long to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static implicit operator NdArray(long a)
        {
            return new NdArray(new[] { (Real)a });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Makes a deep copy of this object. </summary>
        ///
        /// <returns>   A copy of this object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NdArray Clone()
        {
            return new NdArray
            {
                ParentFunc = ParentFunc,
                Data = Data.ToArray(),
                Grad = Grad.ToArray(),
                Shape = Shape.ToArray(),
                Name = Name,
                Length = Length,
                BatchCount = BatchCount,
                UseCount = UseCount,
                TrainCount = TrainCount
            };
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sums. </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="a">        A NdArray to process. </param>
        /// <param name="keepDims"> (Optional) True to keep dims. </param>
        /// <param name="axis">     A variable-length parameters list containing axis. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray Sum(NdArray a, bool keepDims = false, params int[] axis)
        {
#if DEBUG
            if (axis.Length != axis.Distinct().ToArray().Length)
            {
                throw new Exception("The specified element is a duplicate");
            }

            if (axis.Length != 0 && a.Shape.Length < axis.Max())
            {
                throw new Exception("The specified element is out of range");
            }
#endif
            if (axis.Length == 0)
            {
                axis = Enumerable.Range(0, a.Shape.Length).ToArray();
            }

            Array.Sort(axis);

            NdArray result = Sum(a, axis[0]);

            for (int i = 1; i < axis.Length; i++)
            {
                result = Sum(result, axis[i] - i);
            }

            if (keepDims)
            {
                List<int> resultKeepDimShape = new List<int>();
                int count = a.Shape.Length - result.Shape.Length;

                for (int i = 0; i < count; i++)
                {
                    resultKeepDimShape.Add(1);
                }

                resultKeepDimShape.AddRange(result.Shape);
                result.Shape = resultKeepDimShape.ToArray();
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sums. </summary>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="axis"> The axis. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static NdArray Sum(NdArray a, int axis)
        {
            int[] resultShape = new int[a.Shape.Length - 1];

            for (int i = 0, j = 0; i < a.Shape.Length; i++)
            {
                if (i != axis)
                {
                    resultShape[j++] = a.Shape[i];
                }
            }

            NdArray result = new NdArray(resultShape, a.BatchCount);

            for (int i = 0; i < a.Length; i++)
            {
                List<int> index = new List<int>(a.GetDimensionsIndex(i));
                index.RemoveAt(axis);
                int localIndex = result.GetLocalIndex(0, index.ToArray());

                for (int batchCount = 0; batchCount < a.BatchCount; batchCount++)
                {
                    result.Data[batchCount * result.Length + localIndex] += a.Data[batchCount * a.Length + i];
                    result.Grad[batchCount * result.Length + localIndex] += a.Grad[batchCount * a.Length + i];
                }
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Splits. </summary>
        ///
        /// <param name="array">    The array. </param>
        /// <param name="indices">  The indices. </param>
        /// <param name="axis">     (Optional) The axis. </param>
        ///
        /// <returns>   A NdArray[]. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray[] Split(NdArray array, int indices, int axis = 1)
        {
            return Split(array, new[] { indices }, axis);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Splits. </summary>
        ///
        /// <param name="array">    The array. </param>
        /// <param name="indices">  The indices. </param>
        /// <param name="axis">     (Optional) The axis. </param>
        ///
        /// <returns>   A NdArray[]. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray[] Split(NdArray array, int[] indices, int axis = 1)
        {
            int[] shapeOffets = new int[indices.Length + 1];        // an array with the leading 0 of the entered indices added
            int[] resultAxisShapes = new int[indices.Length + 1];   // Shape of specified axis after division

            for (int i = 0; i < indices.Length; i++)
            {
                shapeOffets[i + 1] = indices[i];
                resultAxisShapes[i] = indices[i] - shapeOffets[i];
            }
            resultAxisShapes[indices.Length] = array.Shape[axis] - indices[indices.Length - 1];

            NdArray[] resultArrays = new NdArray[indices.Length + 1];

            for (int i = 0; i < resultArrays.Length; i++)
            {
                int[] resultShape = array.Shape.ToArray();
                resultShape[axis] = resultAxisShapes[i];
                resultArrays[i] = new NdArray(resultShape, array.BatchCount);
            }

            for (int batchCount = 0; batchCount < array.BatchCount; batchCount++)
            {
                for (int i = 0; i < resultArrays.Length; i++)
                {
                    for (int j = 0; j < resultArrays[i].Length; j++)
                    {
                        int[] resultIndex = resultArrays[i].GetDimensionsIndex(j);
                        resultIndex[axis] += shapeOffets[i];
                        int localIndex = array.GetLocalIndex(batchCount, resultIndex);

                        resultArrays[i].Data[batchCount * resultArrays[i].Length + j] = array.Data[localIndex];
                        resultArrays[i].Grad[batchCount * resultArrays[i].Length + j] = array.Grad[localIndex];
                    }
                }
            }

            return resultArrays;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Concatenates. </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="a">    A NdArray to process. </param>
        /// <param name="b">    A NdArray to process. </param>
        /// <param name="axis"> The axis. </param>
        ///
        /// <returns>   A NdArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NdArray Concatenate(NdArray a, NdArray b, int axis)
        {
            int[] shapeList = a.Shape.ToArray();
            shapeList[axis] += b.Shape[axis];

#if DEBUG
            for (int i = 0; i < a.Shape.Length; i++)
            {
                if (i != axis && a.Shape[i] != b.Shape[i])
                {
                    throw new Exception("Array sizes are not matched");
                }
            }

            if (a.BatchCount != b.BatchCount)
            {
                throw new Exception("Batch size is not matched");
            }
#endif

            NdArray result = new NdArray(shapeList.ToArray(), a.BatchCount);

            for (int batchCount = 0; batchCount < a.BatchCount; batchCount++)
            {
                int aInputBatchoffset = batchCount * a.Length;
                int bInputBatchoffset = batchCount * b.Length;

                for (int i = 0; i < a.Length; i++)
                {
                    int resultindex = result.GetLocalIndex(batchCount, a.GetDimensionsIndex(i));

                    result.Data[resultindex] = a.Data[i + aInputBatchoffset];
                    result.Grad[resultindex] = a.Grad[i + aInputBatchoffset];
                }

                for (int i = 0; i < b.Length; i++)
                {
                    int[] tmpIndex = b.GetDimensionsIndex(i);
                    tmpIndex[axis] += a.Shape[axis];

                    int resultIndex = result.GetLocalIndex(batchCount, tmpIndex);

                    result.Data[resultIndex] = b.Data[i + bInputBatchoffset];
                    result.Grad[resultIndex] = b.Grad[i + bInputBatchoffset];
                }
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets dimensions index. </summary>
        ///
        /// <param name="index">    Zero-based index of the. </param>
        ///
        /// <returns>   An array of int. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal int[] GetDimensionsIndex(int index)
        {
            //バッチ分を補正
            int batchCount = index / Length;
            index -= Length * batchCount;

            int[] dimensionsIndex = new int[Shape.Length];

            for (int i = Shape.Length - 1; i >= 0; i--)
            {
                dimensionsIndex[i] = index % Shape[i];
                index /= Shape[i];
            }

            return dimensionsIndex;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets local index. </summary>
        ///
        /// <param name="batchIndex">   Zero-based index of the batch. </param>
        /// <param name="indices">      A variable-length parameters list containing indices. </param>
        ///
        /// <returns>   The local index. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal int GetLocalIndex(int batchIndex, params int[] indices)
        {
            int indicesLastIndex = indices.Length - 1;
            int index = batchIndex * Length + indices[indicesLastIndex];
            int rankoffset = 1;

            for (int i = 1; i < indices.Length; i++)
            {
                rankoffset *= Shape[indicesLastIndex--];
                index += indices[indicesLastIndex] * rankoffset;
            }

            return index;
        }
    }
}