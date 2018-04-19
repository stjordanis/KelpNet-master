﻿#region License

/*

Copyright (c) 2009 - 2013 Fatjon Sakiqi

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

#endregion

namespace Cloo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Threading;
    using Cloo.Bindings;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Represents an OpenCL image. </summary>
    ///
    /// <remarks>
    /// A memory object that stores a two- or three- dimensional structured array. Image data can
    /// only be accessed with read and write functions. The read functions use a sampler.
    /// </remarks>
    ///
    /// <seealso cref="T:Cloo.ComputeMemory"/>
    /// <seealso cref="ComputeMemory"/>
    /// <seealso cref="ComputeSampler"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public abstract class ComputeImage : ComputeMemory
    {
        #region Properties

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets (protected) the depth in pixels of the <see cref="ComputeImage"/>. </summary>
        ///
        /// <value> The depth in pixels of the <see cref="ComputeImage"/>. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Depth { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets (protected) the size of the elements (pixels) of the <see cref="ComputeImage"/>.
        /// </summary>
        ///
        /// <value> The size of the elements (pixels) of the <see cref="ComputeImage"/>. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int ElementSize { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets (protected) the height in pixels of the <see cref="ComputeImage"/>. </summary>
        ///
        /// <value> The height in pixels of the <see cref="ComputeImage"/>. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Height { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets (protected) the size in bytes of a row of elements of the <see cref="ComputeImage"/>.
        /// </summary>
        ///
        /// <value> The size in bytes of a row of elements of the <see cref="ComputeImage"/>. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public long RowPitch { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets (protected) the size in bytes of a 2D slice of a <see cref="ComputeImage3D"/>.
        /// </summary>
        ///
        /// <value>
        /// The size in bytes of a 2D slice of a <see cref="ComputeImage3D"/>. For a
        /// <see cref="ComputeImage2D"/> this value is 0.
        /// </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public long SlicePitch { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets (protected) the width in pixels of the <see cref="ComputeImage"/>. </summary>
        ///
        /// <value> The width in pixels of the <see cref="ComputeImage"/>. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Width { get; protected set; }

        #endregion

        #region Constructors

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the Cloo.ComputeImage class. </summary>
        ///
        /// <param name="context">  . </param>
        /// <param name="flags">    . </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected ComputeImage(ComputeContext context, ComputeMemoryFlags flags)
            : base(context, flags)
        { }

        #endregion

        #region Protected methods

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets supported formats. </summary>
        ///
        /// <param name="context">  . </param>
        /// <param name="flags">    . </param>
        /// <param name="type">     . </param>
        ///
        /// <returns>   The supported formats. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected static ICollection<ComputeImageFormat> GetSupportedFormats(ComputeContext context, ComputeMemoryFlags flags, ComputeMemoryType type)
        {
            int formatCountRet = 0;
            ComputeErrorCode error = CL12.GetSupportedImageFormats(context.Handle, flags, type, 0, null, out formatCountRet);
            ComputeException.ThrowOnError(error);

            ComputeImageFormat[] formats = new ComputeImageFormat[formatCountRet];
            error = CL12.GetSupportedImageFormats(context.Handle, flags, type, formatCountRet, formats, out formatCountRet);
            ComputeException.ThrowOnError(error);

            return new Collection<ComputeImageFormat>(formats);
        }

        /// <summary>   Initializes this object. </summary>
        protected void Init()
        {
            SetID(Handle.Value);

            Depth = (int)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.Depth, CL12.GetImageInfo);
            ElementSize = (int)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.ElementSize, CL12.GetImageInfo);
            Height = (int)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.Height, CL12.GetImageInfo);
            RowPitch = (long)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.RowPitch, CL12.GetImageInfo);
            Size = (long)GetInfo<CLMemoryHandle, ComputeMemoryInfo, IntPtr>(Handle, ComputeMemoryInfo.Size, CL12.GetMemObjectInfo);
            SlicePitch = (long)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.SlicePitch, CL12.GetImageInfo);
            Width = (int)GetInfo<CLMemoryHandle, ComputeImageInfo, IntPtr>(Handle, ComputeImageInfo.Width, CL12.GetImageInfo);

            Trace.WriteLine("Create " + this + " in Thread(" + Thread.CurrentThread.ManagedThreadId + ").", "Information");
        }

        #endregion
    }
}