using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Wkhtmltopdf.NetCore
{
    /// <summary>
    ///     Provides exact specified path to wkthmltopdf/wkthmltoimage.
    /// </summary>
    public class ExactPathProvider : IWkhtmltopdfPathProvider
    {
        private readonly string _path;

        private const string DefaultPathWindows = @"C:\Program Files\wkhtmltopdf\bin\wkhtmltopdf";
        private const string DefaultPathLinux = @"/usr/bin/wkhtmltopdf";
        /// <summary>
        ///     Constructs new instance of <see cref="ExactPathProvider" />. Uses provided path as is.
        ///     save the environmet variable  = WKPDF
        /// </summary>
        /// <param name="pathWindows">Path to wkthmltopdf/wkthmltoimage.</param>
        public ExactPathProvider(string pathWindows, string pathLinux)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var newPath = Environment.GetEnvironmentVariable("WKPDF");
                _path = newPath ?? (string.IsNullOrEmpty(pathWindows) ? DefaultPathWindows : pathWindows);
            }
            else
            {
                _path = string.IsNullOrEmpty(pathLinux) ? DefaultPathLinux : pathLinux;
            }

        }

        /* <inheritDoc /> */
        public string GetPath() => _path;
    }
}
