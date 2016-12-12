﻿// <copyright file="PixelArea.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents an area of generic <see cref="Image{TColor,TPacked}"/> pixels.
    /// </summary>
    /// <typeparam name="TColor">The pixel format.</typeparam>
    /// <typeparam name="TPacked">The packed format. <example>uint, long, float.</example></typeparam>
    public sealed unsafe class PixelArea<TColor, TPacked> : IDisposable
        where TColor : struct, IPackedPixel<TPacked>
        where TPacked : struct
    {
        /// <summary>
        /// Provides a way to access the pixels from unmanaged memory.
        /// </summary>
        private readonly GCHandle pixelsHandle;

        /// <summary>
        /// The pointer to the pixel buffer.
        /// </summary>
        private IntPtr dataPointer;

        /// <summary>
        /// A value indicating whether this instance of the given entity has been disposed.
        /// </summary>
        /// <value><see langword="true"/> if this instance has been disposed; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// If the entity is disposed, it must not be disposed a second time. The isDisposed field is set the first time the entity
        /// is disposed. If the isDisposed field is true, then the Dispose() method will not dispose again. This help not to prolong the entity's
        /// life in the Garbage Collector.
        /// </remarks>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="componentOrder">The component order.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="bytes"></paramref> is the incorrect length.
        /// </exception>
        public PixelArea(int width, byte[] bytes, ComponentOrder componentOrder)
            : this(width, 1, bytes, componentOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="componentOrder">The component order.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="bytes"></paramref> is the incorrect length.
        /// </exception>
        public PixelArea(int width, int height, byte[] bytes, ComponentOrder componentOrder)
        {
            this.CheckBytesLength(width, height, bytes, componentOrder);

            this.Width = width;
            this.Height = height;
            this.ComponentOrder = componentOrder;
            this.RowByteCount = width * GetComponentCount(componentOrder);
            this.Bytes = bytes;
            this.pixelsHandle = GCHandle.Alloc(this.Bytes, GCHandleType.Pinned);

            // TODO: Why is Resharper warning us about an impure method call?
            this.dataPointer = this.pixelsHandle.AddrOfPinnedObject();
            this.PixelBase = (byte*)this.dataPointer.ToPointer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="componentOrder">The component order.</param>
        public PixelArea(int width, ComponentOrder componentOrder)
          : this(width, 1, componentOrder, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        /// <param name="width">The width. </param>
        /// <param name="componentOrder">The component order.</param>
        /// <param name="padding">The number of bytes to pad each row.</param>
        public PixelArea(int width, ComponentOrder componentOrder, int padding)
          : this(width, 1, componentOrder, padding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="componentOrder">The component order.</param>
        /// <param name="padding">The number of bytes to pad each row.</param>
        public PixelArea(int width, int height, ComponentOrder componentOrder, int padding)
        {
            this.Width = width;
            this.Height = height;
            this.ComponentOrder = componentOrder;
            this.RowByteCount = (width * GetComponentCount(componentOrder)) + padding;
            this.Bytes = new byte[this.RowByteCount * height];
            this.pixelsHandle = GCHandle.Alloc(this.Bytes, GCHandleType.Pinned);

            // TODO: Why is Resharper warning us about an impure method call?
            this.dataPointer = this.pixelsHandle.AddrOfPinnedObject();
            this.PixelBase = (byte*)this.dataPointer.ToPointer();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PixelArea{TColor,TPacked}"/> class.
        /// </summary>
        ~PixelArea()
        {
            this.Dispose();
        }

        /// <summary>
        /// Gets the data in bytes.
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Gets the pointer to the pixel buffer.
        /// </summary>
        public IntPtr DataPointer => this.dataPointer;

        /// <summary>
        /// Gets the data pointer.
        /// </summary>
        public byte* PixelBase { get; private set; }

        /// <summary>
        /// Gets the component order.
        /// </summary>
        public ComponentOrder ComponentOrder { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets number of bytes in a row.
        /// </summary>
        public int RowByteCount { get; }

        /// <summary>
        /// Reads the stream to the area.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Read(Stream stream)
        {
            stream.Read(this.Bytes, 0, this.Bytes.Length);
        }

        /// <summary>
        /// Writes the area to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Write(Stream stream)
        {
            stream.Write(this.Bytes, 0, this.Bytes.Length);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.PixelBase == null)
            {
                return;
            }

            if (this.pixelsHandle.IsAllocated)
            {
                this.pixelsHandle.Free();
            }

            this.dataPointer = IntPtr.Zero;
            this.PixelBase = null;

            this.isDisposed = true;

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resets the bytes of the array to it's initial value.
        /// </summary>
        internal void Reset()
        {
            Unsafe.InitBlock(this.PixelBase, 0, (uint)this.Bytes.Length);
        }

        /// <summary>
        /// Gets component count for the given order.
        /// </summary>
        /// <param name="componentOrder">The component order.</param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if an invalid order is given.
        /// </exception>
        private static int GetComponentCount(ComponentOrder componentOrder)
        {
            switch (componentOrder)
            {
                case ComponentOrder.ZYX:
                case ComponentOrder.XYZ:
                    return 3;
                case ComponentOrder.ZYXW:
                case ComponentOrder.XYZW:
                    return 4;
            }

            throw new NotSupportedException();
        }

        [Conditional("DEBUG")]
        private void CheckBytesLength(int width, int height, byte[] bytes, ComponentOrder componentOrder)
        {
            int requiredLength = (width * GetComponentCount(componentOrder)) * height;
            if (bytes.Length != requiredLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), $"Invalid byte array length. Length {bytes.Length}; Should be {requiredLength}.");
            }
        }
    }
}
