using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static void ConvertBitrate(AATB_DirInfo Dir, FileInfo[] WAVFileList,
                                   string ConversionFromBitrate, string ConversionToBitrate)
        {
            /* Converts FIles in input list from one bitrate to another
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             *     Dir.Path
             *     Dir.ParentPath
             *   WAVFileList  Array of WAV files (any bitrate)
             *   ConversionFromBitrate <bitdepth-samplerate> i.e., 16-44
             *   ConversionToBitrate  <bitdepth-samplerate> i.e., 16-44      
             * Calls external program:
             *   sox (Sound Output eXchange utility)
             *     -b <bit depth for output files = 16|24>
             *     -r <samplerate for output files = 44100|48000|88200|96000 >
             * Outputs:
             *   All wav files in input array will be converted to the new bitrate
             *   and written to the parent's subdirectory with <bitrate>.wav suffi
             */
            int
                TrackNumber = 0,
                ConversionBitrateComparison;
            string
                TrackNumberStr,
                BitDepth,
                SampleRate,
                InputFileName,
                InputFilePath,
                OutputDirPath,
                OutputFileName,
                OutputFilePath,
                SongBitrate,
                ExternalProgram = "sox.exe",
                ExternalArguments,
                ExternalOutput;

            // build output directory path - subdir of parent directory
            OutputDirPath = Dir.ParentPath + BACKSLASH + ConversionToBitrate;
			// create output directory or overwrite existing directory
            if ((!Directory.Exists(OutputDirPath) && CreateDir(OutputDirPath))
                || (Directory.Exists(OutputDirPath) && Overwrite))
            {
                Log.WriteLine("  Converting from " + ConversionFromBitrate + " to " + ConversionToBitrate);
                Log.WriteLine("  Input Dir:  " + Dir.Path);
                Log.WriteLine("  Output Dir: " + OutputDirPath);
                Log.Write("    Track: ");
                // extract bitdepth and samplerate from ConversionToBitrate string
				// using hyphen as delimiter, e.g., "16-44" --> (16, 44)
                (BitDepth, SampleRate) = SplitString(ConversionToBitrate, HYPHEN);
                switch (SampleRate)
                {
                    case "44":
                        SampleRate = "44100";
                        break;
                    case "48":
                        SampleRate = "48000";
                        break;
                    case "88":
                        SampleRate = "88200";
                        break;
                    case "96":
                        SampleRate = "96000";
                        break;
                }
                // process all files in input list
                foreach (FileInfo fi in WAVFileList)
                {
                    // increment tracknumber and convert to two place string
                    TrackNumber++;
                    TrackNumberStr = TrackNumber.ToString("00");
                    Log.Write(TrackNumberStr + "..");
                    // build filenames
                    InputFileName = fi.Name;
                    InputFilePath = fi.FullName;
                    // get bitrate
                    SongBitrate = GetTrackBitrate(InputFilePath);
                    if (Debug) Console.WriteLine("dbg: Input File Bitrate: {0}", SongBitrate);
                    // Compare actual bitrate of each file with the "conversion from" bitrate
                    ConversionBitrateComparison = String.Compare(SongBitrate, ConversionFromBitrate, comparisonType: StringComparison.OrdinalIgnoreCase);
                    if (ConversionBitrateComparison != 0)
                    {
                        Log.WriteLine("\n*** Input file bitrate " + SongBitrate + " is not equal to 'conversion from' bitrate");
                        break;
                    }
                    // build output file pathname
                    OutputFileName = Regex.Replace(InputFileName, @ConversionFromBitrate, ConversionToBitrate);
                    OutputFilePath = OutputDirPath + BACKSLASH + OutputFileName;
                    if (Debug) Console.WriteLine("dbg: OutputFilePath: {0}", OutputFilePath);
                    // check output file does not exist, or overwrite existing file
                    if ((!File.Exists(OutputFilePath))
                       || (File.Exists(OutputFilePath) && Overwrite))
                    {
                        ExternalArguments = DBLQ + InputFilePath + DBLQ
                                          + " -b " + BitDepth
                                          + " -r " + SampleRate
                                          + SPACE + DBLQ + OutputFilePath + DBLQ;
                        // run external program
                        ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
                    }
                    else
                    {
                        Log.WriteLine("\n*** Output file exists, use overwrite option to replace:\n"
                                     +"    " + OutputFilePath);
                        break;
                    }
                }
                Log.WriteLine();
            }
            else
                Log.WriteLine("*** Output directory exists, use overwrite option to replace\n"
                            + "    " + OutputDirPath);
        } // end ConvertBitrate
    }
}