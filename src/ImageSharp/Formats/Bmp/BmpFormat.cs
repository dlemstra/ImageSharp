﻿// <copyright file="BmpFormat.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Formats
{
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates the means to encode and decode bitmap images.
    /// </summary>
    public class BmpFormat : IImageFormat
    {
        /// <inheritdoc/>
        public string MimeType => "image/bmp";

        /// <inheritdoc/>
        public string Extension => "bmp";

        /// <inheritdoc/>
        public IEnumerable<string> SupportedExtensions => new string[] { "bmp", "dip" };

        /// <inheritdoc/>
        public IImageDecoder Decoder => new BmpDecoder();

        /// <inheritdoc/>
        public IImageEncoder Encoder => new BmpEncoder();
    }
}
