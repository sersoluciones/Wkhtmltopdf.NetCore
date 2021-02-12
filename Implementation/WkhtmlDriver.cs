using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Wkhtml.Options;
using Wkhtmltopdf.NetCore.Interfaces;

namespace Wkhtmltopdf.NetCore
{
    public class WkhtmlDriver : IWkhtmlDriver
    {
        private readonly IWkhtmltopdfPathProvider _pathProvider;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger _logger;


        public WkhtmlDriver(IWebHostEnvironment hostingEnvironment, IWkhtmltopdfPathProvider pathProvider = null, ILogger logger = null)
        {
            _hostingEnvironment = hostingEnvironment;
            _pathProvider = pathProvider ?? RotativaPathAsPrefixPathProvider.Default;
            _logger = logger;
        }

        /* <inheritDoc /> */
        public byte[] Convert(IConvertOptions options, string html) => Convert(_hostingEnvironment, _pathProvider, options.GetConvertOptions(), html, logger: _logger);

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

            return Convert(null, new ExactPathProvider(rotativaLocation, ""), switches, html);
        }

        /// <summary>
        /// Converts given URL or HTML string to PDF.
        /// </summary>
        /// <param name="pathProvider">Path to wkthmltopdf\wkthmltoimage.</param>
        /// <param name="switches">Switches that will be passed to wkhtmltopdf binary.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <returns>PDF as byte array.</returns>
        private static byte[] Convert(IWebHostEnvironment hostingEnvironment, IWkhtmltopdfPathProvider pathProvider, string switches, string html, ILogger logger = null)
        {
            string globalPath = "files/upload/pdf/";
            string nameFile = Guid.NewGuid().ToString();
            string sWebRootFolder = hostingEnvironment.WebRootPath;
            var webRootPath = Path.Combine(sWebRootFolder, globalPath);

            string inputPath = string.Format("{0}{1}", globalPath, $"{nameFile}.html");
            inputPath = Path.Combine(sWebRootFolder, inputPath);

            string outputPath = string.Format("{0}{1}", globalPath, $"{nameFile}.pdf");
            outputPath = Path.Combine(sWebRootFolder, outputPath);

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                html = SpecialCharsEncode(html);
            }

            // switches:
            //     "-q"  - silent output, only errors - no progress messages
            //     " -"  - switch output to stdout
            //     "- -" - switch input to stdin and output to stdout          

            switch (OS.GetCurrent())
            {
                case "win":
                    switches = "-q " + switches + " - - ";

                    break;
                case "mac":
                case "gnu":
                    if (!Directory.Exists(webRootPath))
                    {
                        logger?.LogInformation("That path not exists already.");
                        // Try to create the directory.
                        DirectoryInfo di = Directory.CreateDirectory(webRootPath);
                        logger?.LogInformation("The directory was created successfully at {0}.", Directory.GetCreationTime(webRootPath));

                    }

                    // Create the file, or overwrite if the file exists.
                    using (FileStream fs = File.Create(inputPath))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(html);
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                    }

                    switches = "-q " + switches + $" {inputPath} {outputPath}";
                    break;
            }

            var wkhtmlPath = pathProvider.GetPath();

            Console.WriteLine($"--------------------- wkhtmlPath: {wkhtmlPath} ");
            Console.WriteLine($"--------------------- switches: {switches} ");

            var result = Shell.Term(wkhtmlPath, switches, html: html);

            logger?.LogInformation(result.code.ToString());
            if (result.code == 0)
            {
                logger?.LogInformation($"Command Works :D");
            }
            else
            {
                logger?.LogError(result.stderr);
                throw new WkhtmlDriverException($"Failed to create PDF {result.stderr}.", new Exception(result.stderr));
            }

            /// delete file
            if (!OS.IsWin())
            {
                if (File.Exists(inputPath))
                    File.Delete(inputPath);

                result.bytes = File.ReadAllBytesAsync(outputPath).Result;

                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }

            return result.bytes;
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
