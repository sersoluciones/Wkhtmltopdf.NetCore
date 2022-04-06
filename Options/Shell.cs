using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wkhtmltopdf.NetCore;

namespace Wkhtml.Options
{
    public class Response
    {
        public int code { get; set; }
        public string stdout { get; set; }
        public string stderr { get; set; }
        public byte[] bytes { get; set; }
    }

    public static class Shell
    {
        public static Response Term(string wkhtmlPath, string cmd, string html = "")
        {
            var result = new Response();
            var stderr = new StringBuilder();
            var stdout = new StringBuilder();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = wkhtmlPath,
                    Arguments = cmd,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (OS.IsWin())
                {
                    startInfo.RedirectStandardInput = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                }
                //if (!string.IsNullOrEmpty(dir) && output != Output.External)
                //{
                //    startInfo.WorkingDirectory = dir;
                //}
                var ms = new MemoryStream();
                using var process = new Process
                {
                    StartInfo = startInfo
                };
                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
                    throw new WkhtmlDriverException($"Failed to start wkhtmltodpf at path {wkhtmlPath}.", e);
                }

                //while (!process.StandardOutput.EndOfStream)
                //{
                //    string line = process.StandardOutput.ReadLine();
                //    stdout.AppendLine(line);
                //    Console.WriteLine(line);
                //}
                if (OS.IsWin())
                {
                    // generate PDF from given HTML string, not from URL
                    if (!string.IsNullOrEmpty(html))
                    {
                        using var sIn = process.StandardInput;
                        sIn.WriteLine(html);
                    }

                    using var sOut = process.StandardOutput.BaseStream;
                    byte[] buffer = new byte[4096];
                    int read;

                    while ((read = sOut.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }

                    string error = process.StandardError.ReadToEnd();
                    //process.StandardOutput.ReadToEnd();

                    if (ms.Length == 0)
                    {
                        stdout.AppendLine(error);
                        //throw new Exception(error);
                    }
                }
                process.WaitForExit();

                result.stdout = stdout.ToString();
                result.stderr = stderr.ToString();
                result.code = process.ExitCode;
                if (OS.IsWin())
                    result.bytes = ms.ToArray();
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
            }
            return result;
        }
        
        public static Response TermLinux(string wkhtmlPath, string switches, string html = "")
        {
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
            var result = new Response
            {
                bytes = ms.ToArray()
            };
            return result;
        }
    }
}
