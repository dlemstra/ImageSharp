﻿// <copyright file="ImageProcessor.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Processors
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the application of processors to images.
    /// </summary>
    /// <typeparam name="TColor">The pixel format.</typeparam>
    /// <typeparam name="TPacked">The packed format. <example>uint, long, float.</example></typeparam>
    public abstract class ImageProcessor<TColor, TPacked> : IImageProcessor
        where TColor : struct, IPackedPixel<TPacked>
        where TPacked : struct
    {
        /// <inheritdoc/>
        public virtual ParallelOptions ParallelOptions { get; set; } = Bootstrapper.Instance.ParallelOptions;

        /// <inheritdoc/>
        public virtual bool Compand { get; set; } = false;
    }
}