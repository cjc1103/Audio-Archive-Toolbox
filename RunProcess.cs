using System;
using System.Diagnostics;

namespace AATB
{
    public partial class AATB_Main
    {
        static string RunProcess(string ExternalProgram, string ExternalArguments)
        {
            /* Creates a process to run an external program
             * Inputs
             *   ExternalProgram     External program to run in process
             *   ExternalArguments    String containing arguments
             * Note: Process sends normal output to StandardError stream so "RedirectStandardError"
             *   option is set to false
             * Returns:
             *   StandardOutput string from external program
             */
            Process p;
            string
                ExternalOutput = null;

            if (ExternalProgram != null)
            {
                p = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ExternalProgram,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                        Arguments = ExternalArguments
                    }
                };
                try
                {
                    p.Start();
                    ExternalOutput = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                catch (Exception e)
                {
                    Log.WriteLine(); // flush output buffer
                    Log.WriteLine("*** Fatal program exception: " + ExternalProgram);
                    if (Debug) Log.WriteLine(ExternalArguments);
                    if (Debug) Log.WriteLine(e.Message);
                    Environment.Exit(0);
                }
            }
            else
                Log.WriteLine("*** External program not specified");

            return ExternalOutput;
        } // end RunProcess
    }
}