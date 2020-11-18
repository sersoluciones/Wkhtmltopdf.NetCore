using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Wkhtmltopdf.NetCore.Interfaces;

namespace Wkhtmltopdf.NetCore
{
    public class WkhtmlDriver : IWkhtmlDriver
    {
        private readonly IWkhtmltopdfPathProvider _pathProvider;

        public WkhtmlDriver(IWkhtmltopdfPathProvider pathProvider = null)
        {
            _pathProvider = pathProvider ?? RotativaPathAsPrefixPathProvider.Default;
        }

        /* <inheritDoc /> */
        public byte[] Convert(IConvertOptions options, string html) => Convert(_pathProvider, options.GetConvertOptions(), html);

        /// <summary>
        /// Converts given URL or HTML string to PDF.
        /// </summary>
        /// <param name="wkhtmlPath">Path to wkthmltopdf\wkthmltoimage.</param>
        /// <param name="switches">Switches that will be passed to wkhtmltopdf binary.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <returns>PDF as byte array.</returns>
        [Obsolete]
        public static byte[] Convert(string wkhtmlPath, string switches, string html)
        {
            string rotativaLocation;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                rotativaLocation = Path.Combine(wkhtmlPath, "Windows", "wkhtmltopdf.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                rotativaLocation = Path.Combine(wkhtmlPath, "Mac", "wkhtmltopdf");
            }
            else
            {
                rotativaLocation = Path.Combine(wkhtmlPath, "Linux", "wkhtmltopdf");
            }

            if (!File.Exists(rotativaLocation))
            {
                throw new Exception("wkhtmltopdf not found, searched for " + rotativaLocation);
            }

            return Convert(new ExactPathProvider(rotativaLocation,  ""), switches, html);
        }

        /// <summary>
        /// Converts given URL or HTML string to PDF.
        /// </summary>
        /// <param name="pathProvider">Path to wkthmltopdf\wkthmltoimage.</param>
        /// <param name="switches">Switches that will be passed to wkhtmltopdf binary.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <returns>PDF as byte array.</returns>
        private static byte[] Convert(IWkhtmltopdfPathProvider pathProvider, string switches, string html)
        {
            // switches:
            //     "-q"  - silent output, only errors - no progress messages
            //     " -"  - switch output to stdout
            //     "- -" - switch input to stdin and output to stdout
            switches = "-q " + switches + " -";

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                switches += " -";
                html = SpecialCharsEncode(html);
            }

            var wkhtmlPath = pathProvider.GetPath();
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = wkhtmlPath,
                    Arguments = switches,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                proc.Start();
            }
            catch (Exception e)
            {
                throw new WkhtmlDriverException($"Failed to start wkhtmltodpf at path {wkhtmlPath}.", e);
            }

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                using (var sIn = proc.StandardInput)
                {
                    sIn.WriteLine(html);
                }
            }

            using var ms = new MemoryStream();
            using (var sOut = proc.StandardOutput.BaseStream)
            {
                byte[] buffer = new byte[4096];
                int read;

                while ((read = sOut.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
            }

            string error = proc.StandardError.ReadToEnd();

            if (ms.Length == 0)
            {
                throw new Exception(error);
            }

            proc.WaitForExit();

            return ms.ToArray();
        }

        /// <summary>
        /// Encode all special chars
        /// </summary>
        /// <param name="text">Html text</param>
        /// <returns>Html with special chars encoded</returns>
        private static string SpecialCharsEncode(string text)
        {
            var chars = text.ToCharArray();
            var result = new StringBuilder(text.Length + (int)(text.Length * 0.1));

            foreach (var c in chars)
            {
                var value = System.Convert.ToInt32(c);
                if (value > 127)
                    result.AppendFormat("&#{0};", value);
                else
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}
