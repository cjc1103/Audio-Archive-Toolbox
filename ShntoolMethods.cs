using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void CreateSHNReport(string SHNReportPath, FileInfo[] FLACFileList)
        {
            /* Create shntool report
             * Inputs:
             *   SHNReportPath pathname of SHNRPT report file
             *   FLACFileList  list of all FLAC files to include in report
             * Calls external programs:
             *   shntool
             *     len mode produces shntool "length" report for all files in input list
             * Outputs:
             *   Produces a text file to stdout
             */
            string
                Data1,
                Data2,
                FileName,
                FLACFileNames = null,
                ExternalProgram,
                ExternalArguments,
                ExternalOutput;
            string[]
                DataList;
            bool
                FirstDataLine;

            ExternalProgram = "shntool.exe";

            if (FLACFileList != null)
            {
                Log.WriteLine("    Creating shntool report");
                if (CreateFile(SHNReportPath))
                {
                    // build string containing FLAC file names from list
                    foreach (FileInfo fi in FLACFileList)
                        FLACFileNames = FLACFileNames + SPACE + DBLQ + fi.FullName + DBLQ;

                    // create argument string
                    ExternalArguments = "len"   // no extra space needed here
                                      + FLACFileNames;
                    if (Debug) Console.WriteLine(ExternalArguments);

                    // run shntool program
                    ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);

                    // raw output includes entire path for filenames
                    // split external output into lines
                    DataList = SplitDataByLine(ExternalOutput);
                    FirstDataLine = true;
                    // parse each line in the list to remove embedded path
                    foreach (string li in DataList)
                    {
                        if (FirstDataLine)
                        {
                            // first line in output has column headers
                            // copy first line directly to shntool file, reset flag
                            File.WriteAllText(SHNReportPath, li + Environment.NewLine);
                            FirstDataLine = false;
                        }
                        else if (li.Length > 0)
                        {
                            // split data line - file pathname starts at column 66
                            Data1 = li.Substring(0, 65);
                            Data2 = li.Substring(65);
                            // extract filename from Data2
                            FileName = SplitFileName(Data2);
                            File.AppendAllText(SHNReportPath, Data1 + FileName + Environment.NewLine);
                        }
                    }
                }
            }
            else
                Log.WriteLine("*** No flac files found in this directory to create shn report");
        } // end CreateSHNReport

        static void VerifySHNReport(string SHNReportPath, FileInfo[] SHNReportList, string Bitrate)
        {
            /* Verify shntool report
             * If shntool report filename is incorrect, it is changed to the correct name
             * Inputs:
             *   SHNReportPath: Pathname of shntool report file
             *      column (zero based) / description
             *      0                32-34 38-39 43-47           65
             *      length exp32size cbs   WAV   prob  fmt ratio filename
             *      ("x" character may be reported if shntool can't recognize wav format
             *       e.g., 24bit wav files, but are not treated as errors in this program)
             *   SHNReportlist: List of all SHNRPT report files in input directory
             *      only the first one is valid. If multiple entries, none are verified
             * Outputs:
             *   Errors are written to log and stdout
             */

            int
                i,
                SHNErrors = 0,
                SHNReportCount;
            string
                ExistingSHNReportPath,
                fname,
                li;
            string[]
                DataList;

            SHNReportCount = SHNReportList.Length;
            if (SHNReportCount == 1)
            {
                Log.Write("    Verifying shntool report..");

                // check existing SHNRPT filename is correct, otherwise rename it
                ExistingSHNReportPath = SHNReportList[0].FullName;
                if (ExistingSHNReportPath != SHNReportPath)
                    MoveFile(ExistingSHNReportPath, SHNReportPath);

                // read SHNRPT data, ignore first line, read until one less then the line count
                // Note all character numbers referenced start at 0 to match substring index
                DataList = ReadTextFile(SHNReportPath);
                for (i = 1; i < DataList.Length; i++)
                {
                    // get line i from list
                    li = DataList[i];
                    // file name is from char 65 to end of string
                    fname = li.Substring(65);
                    // CD flags char 32-34
                    // only applicable if verifying files for CD burning and bitrate = 16-44
                    if (Bitrate == BR1644)
                    {
                        if (li.Substring(32, 1) == "c")
                        {
                            SHNErrors += 1;
                            Log.Write("\n    CD quality error: " + fname);
                        }
                        if (li.Substring(33, 1) == "b")
                        {
                            SHNErrors += 1;
                            Log.Write("\n    CD sector boundary error: " + fname);
                        }
                        if (li.Substring(34, 1) == "s")
                        {
                            SHNErrors += 1;
                            Log.Write("\n    File is too short to be burned: " + fname);
                        }
                    }
                    // WAV file properties char 38-39
                    if (li.Substring(38, 1) == "h")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV file header is not canonical: " + fname);
                    }
                    if (li.Substring(39, 1) == "e")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV file contains extra RIFF chunks: " + fname);
                    }
                    // WAV file problems char 43-47
                    // Note: "3" in char 43 signifies IDV32 header, ignore
                    if (li.Substring(44, 1) == "a")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV audio data is not block aligned: " + fname);
                    }
                    if (li.Substring(45, 1) == "i")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV header is inconsistent about data and/or file size: " + fname);
                    }
                    if (li.Substring(46, 1) == "t")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV file seems to be truncated: " + fname);
                    }
                    if (li.Substring(47, 1) == "j")
                    {
                        SHNErrors += 1;
                        Log.Write("\n    WAV file seems to have junk appended to it: " + fname);
                    }
                }
                if (SHNErrors == 0)
                    Log.WriteLine("  OK");
                else
                    Log.WriteLine();  // clear output buffer
            }
            else
                Log.WriteLine("    Multiple shntool report files exist, and are not verified");
        } // end VerifySHNReport
    }
}