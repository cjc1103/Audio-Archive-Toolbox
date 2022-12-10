using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static FileInfo[] DecompressToWAV(string CompType, string WAVDirPath, FileInfo[] CompFileList)
        {
            /* Decompresses all lossless audio files in input file list to the original WAV format
             * Inputs:
             *   CompType     String representing what audio compression codec is to be used
             *   Dir          Directory as AATB_DirInfo class instance
             *   WAVDirPath   Output directory
             *   CompFileList List of all FLAC files to be converted
             * Calls external programs, defined in code
             *   Note: FLAC is currently the only compression method supported to reduce complexity
             *   Other lossless compression methods like ALAC and APE could be supported with some
             *   code changes
             * Outputs:
             *   WAV audio files are written to the output directory
             * Returns:
             *   list of wav uncompressed audio files
             */
            FileInfo[]
                WAVFileList = new FileInfo[CompFileList.Length];
            string
                TrackNumberStr,
                BaseFileName,
                Extension,
                WAVFileName,
                WAVFilePath,
                CompFileName,
                CompFilePath,
                ExternalProgram,
                ExternalArguments,
                ExternalOutput;
            int
                TrackNumber = 0;

            // initialize external program parameters
            ExternalProgram = ExternalArguments = null;

            Log.Write("    Track ");
            foreach (FileInfo fi in CompFileList)
            {
                // increment track number and convert to two place string
                TrackNumber++;
                TrackNumberStr = TrackNumber.ToString("00");
                Log.Write(TrackNumberStr + "..");
                // build filenames
                CompFileName = fi.Name;
                CompFilePath = fi.FullName;
                (BaseFileName, Extension) = SplitString(CompFileName, PERIOD);
                WAVFileName = BaseFileName + PERIOD + WAV;
                WAVFilePath = WAVDirPath + BACKSLASH + WAVFileName;
                // convert WAVFileName to FileInfo type and build WAVFileList
                WAVFileList[TrackNumber - 1] = new FileInfo(WAVFilePath);

                switch (CompType)
                {
                    case FLAC:
                        ExternalProgram = "flac.exe";
                        ExternalArguments = "-d"
                                          + " --force"
                                          + SPACE + DBLQ + CompFilePath + DBLQ
                                          + " --output-name " + DBLQ + WAVFilePath + DBLQ;
                        break;

                    default:
                        Log.WriteLine("*** Invalid argument for DecompressToWAV method: " + CompType);
                        Environment.Exit(0);
                        break;
                }

                // run external program
                ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
            }
            Log.WriteLine();
            return WAVFileList;
        } // end DecompressToWAV
    }
}