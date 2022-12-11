using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void ConcatentateWAVFiles(AATB_DirInfo Dir, FileInfo[] WAVFileList, string OutputFileName)
        {
            /* Concatenates separate wav files in WAVFileList into one wav file
             * Input:
             *   RootDir      Current directory
             *   WAVFileList  List of all WAV files to be concatenated
             *   DestinationFileName  Output wav filename
             * Calls external program:
             *   sox input1.wav input2.wav .. inputn.wav output.wav
             *     concatenates input wav files into one contiguous wav file (last parameter = output)
             * Output:
             *   The combined wav file is output to the root directory
             */
            string
               InputWAVFileNames = String.Empty,
               OutputWAVFilePath,
               ExternalProgram,
               ExternalArguments;

            Log.WriteLine("  Creating combined wav file: " + OutputFileName);

            ExternalProgram = "sox.exe";
            OutputWAVFilePath = Dir.ParentPath + BACKSLASH + OutputFileName;
            // create file or overwrite existing file
            if (!File.Exists(OutputWAVFilePath)
                || File.Exists(OutputWAVFilePath) && Overwrite)
            {
                // Delete output file if it exists
                DeleteFile(OutputWAVFilePath);

                // create input file list
                foreach (FileInfo fi in WAVFileList)
                    InputWAVFileNames = InputWAVFileNames + DBLQ + fi.FullName + DBLQ + SPACE;

                // create argument string
                ExternalArguments = InputWAVFileNames
                                  + DBLQ + OutputWAVFilePath + DBLQ;
                if (Debug) Console.WriteLine(ExternalArguments);

                // run external process, discard external output
                RunProcess(ExternalProgram, ExternalArguments);
            }
            else
                Log.WriteLine("*** Combined wav file exists, use overwrite option to replace");

        } // end ConcatentateWAVFiles
    }
}