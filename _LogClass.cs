using System;
using System.Reflection;

namespace AATB
{
    public class AATB_Log
    {
        /* Contains data structures and methods to implement logging function
         * Log messages are written simultaneously to the log file and to the console
         */
        private readonly string LogFileName;

        public AATB_Log(string FileName)
        {
            // Constructor for AATB_Log class
            LogFileName = FileName;
            // writes header to console and log
            string LogEntry =
                "Audio Archive Toolbox " +
                Assembly.GetExecutingAssembly().GetName().Version;
            WriteLine(LogEntry);
        } // end constructor AATB_Log

        public void Start()
        {
            // writes processing start time to console and log
            DateTime dateTimeStr = DateTime.Now;
            string LogHeader =
                ">>> Processing started " + dateTimeStr;
            WriteLine(LogHeader);
        } // end Start

        public void Write(string logEntry)
        {
            // writes entries to console and log
            Console.Write(logEntry);
            try
            {
                File.AppendAllText(LogFileName, logEntry);
            }
            catch (Exception e)
            {
                Console.WriteLine("*** " + e.Message);
                Environment.Exit(0);
            }
        } // end Write

        public void WriteLine(string logEntry)
        {
            // writes entries to console and log with linefeed
            Console.WriteLine(logEntry);
            try
            {
                File.AppendAllText(LogFileName, logEntry + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine("*** " + e.Message);
                Environment.Exit(0);
            }
        } // end WriteLine

        public void WriteLine()
        {
            // writes linefeed to console and log (overload - no input parameter)
            Console.WriteLine();
            try
            {
                File.AppendAllText(LogFileName, Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine("*** " + e.Message);
                Environment.Exit(0);
            }
        } // end WriteLine(no parameters)

        public void End()
        {
            // writes footer to console and log
            DateTime dateTimeStr = DateTime.Now;
            string LogFooter =
                ">>> Processing ended " + dateTimeStr + "\n" +
                "= = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =";
            WriteLine(LogFooter);
        } // end End

    } // end class AATB_Log
}
