﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSMacros.ExecutionEngine;

namespace VSMacros.ExecutionEngine.Helpers
{
    public static class InputParser
    {
        internal static string[] SeparateArgs(string[] args)
        {
            string[] stringSeparator = new string[] { "[delimiter]" };
            string[] separatedArgs = args[0].Split(stringSeparator, StringSplitOptions.RemoveEmptyEntries);
            return separatedArgs;
        }

        internal static int GetPid(string unparsedPid)
        {
            int pid;

            Validate.IsNotNullAndNotEmpty(unparsedPid, "unparsedPid");

            if (!int.TryParse(unparsedPid, out pid))
            {
                MessageBox.Show("The pid is invalid.");
                throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, Resources.InvalidPIDArgument, unparsedPid, "unparsedPid"));
            }

            return pid;
        }

        internal static string DecodePath(string encodedPath)
        {
            return encodedPath.Replace("%20", " ");
        }

        internal static short GetNumberOfIterations(string iter)
        {
            short iterations;

            Validate.IsNotNullAndNotEmpty(iter, "iter");

            if (!short.TryParse(iter, out iterations))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, Resources.InvalidIterationsArgument, iter, "iter"));
            }

            return iterations;
        }

        internal static string ExtractScript(string path)
        {
            Validate.IsNotNullAndNotEmpty(path, "path");
            return File.ReadAllText(path);
        }

        internal static string WrapScript(string unwrapped)
        {
            string wrapped = "function currentScript() {";
            wrapped += unwrapped;
            wrapped += "}";

            return wrapped;
        }
    }
}
