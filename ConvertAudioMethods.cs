using System;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static FileInfo[] ConvertFromWAV(String CompType, AATB_DirInfo Dir, string CompDirPath, FileInfo[] WAVFileList)
        {
            /* Converts all WAV files in input file list to Freeware Lossless Audio Codec format
             * Inputs:
             *   CompType     String representing what audio compression codec is to be used
             *   Dir          Directory as AATB_DirInfo class instance
             *   CompDirPath  Output directory
             *   WAVFileList  List of all WAV files to be converted
             * Calls external programs, defined in code
             * Outputs:
             *   compressed audio files are written to the output directory
             * Returns:
             *   list of compressed audio files
             */
            FileInfo[]
                CompFileList = new FileInfo[WAVFileList.Length];
            string
                TrackNumberStr,
                BaseFileName,
                Extension,
                WAVFileName,
                WAVFilePath,
                CompFileName,
                CompFilePath,
                ExternalProgram,
                ExternalArguments;
            int
                TrackNumber = 0,
                CompTypeIndex,
                QualityValue;

            // initialize external program parameters
            ExternalProgram = ExternalArguments = null;

            Log.Write("    Track ");
            foreach (FileInfo fi in WAVFileList)
            {
                // increment track number and convert to two place string
                TrackNumber++;
                TrackNumberStr = TrackNumber.ToString("00");
                Log.Write(TrackNumberStr + "..");
                // build filenames
                WAVFileName = fi.Name;
                WAVFilePath = fi.FullName;
                (BaseFileName, Extension) = SplitString(WAVFileName, PERIOD);
                CompFileName = BaseFileName + PERIOD + CompType;
                CompFilePath = CompDirPath + BACKSLASH + CompFileName;
                // convert FileName to FileInfo type and build FLACFileList
                CompFileList[TrackNumber - 1] = new FileInfo(CompFilePath);
                // get compression quality value
                CompTypeIndex = Array.IndexOf(AudioCompressionFormats, CompType);
                QualityValue = AudioCompressionQuality[CompTypeIndex][ACTIVE];

                switch (CompType)
                {
                    case MP3:
                        ExternalProgram = "lame.exe";
                        ExternalArguments = "-V" + QualityValue
                                          + " --add-id3v2"
                                          + " --tt " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --ta " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --tl " + DBLQ + Dir.Album + DBLQ
                                          + " --ty " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --tn " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;

                    case M4A:
                        ExternalProgram = "qaac64.exe";
                        ExternalArguments = "--threading"
                                          + " --tvbr " + QualityValue
                                          + " --title " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album " + DBLQ + Dir.Album + DBLQ
                                          + " --date " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --track " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                        break;

                    case OGG:
                        ExternalProgram = "oggenc2.exe";
                        ExternalArguments = "-q " + QualityValue
                                          + " --title=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album=" + DBLQ + Dir.Album + DBLQ
                                          + " --date=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --tracknum=" + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                        break;

                    case OPUS:
                        ExternalProgram = "opusenc.exe";
                        ExternalArguments = "--vbr"
                                          + " --bitrate " + QualityValue
                                          + " --comp 10"
                                          + " --title=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album=" + DBLQ + Dir.Album + DBLQ
                                          + " --date=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;

                    case ALAC:
                        ExternalProgram = "qaac64.exe";
                        ExternalArguments = "--alac"
                                          + " --threading"
                                          + " --title " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album " + DBLQ + Dir.Album + DBLQ
                                          + " --date " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --track " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                        break;

                    case FLAC:
                        ExternalProgram = "flac.exe";
                        if (Dir.Type == TRACKEDAUDIO)
                            ExternalArguments = "-" + QualityValue
                                              + " --force"
                                              + " --verify"
                                              + " --tag=TITLE=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                              + " --tag=ARTIST=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                              + " --tag=ALBUM=" + DBLQ + Dir.Album + DBLQ
                                              + " --tag=DATE=" + DBLQ + Dir.ConcertDate + DBLQ
                                              + " --tag=TRACKNUMBER=" + TrackNumberStr
                                              + SPACE + DBLQ + WAVFilePath + DBLQ
                                              + " --output-name " + DBLQ + CompFilePath + DBLQ;
                        else if (Dir.Type == RAWAUDIO)
                            ExternalArguments = "-" + QualityValue
                                              + " --force"
                                              + " --verify"
                                              + SPACE + DBLQ + WAVFilePath + DBLQ
                                              + " --output-name " + DBLQ + CompFilePath + DBLQ;
                        break;

                    default:
                        Log.WriteLine("*** Undefined external program in CompressFromWAV: " + ExternalProgram);
                        Environment.Exit(0);
                        break;
                }

                // run external process, discard external output
                RunProcess(ExternalProgram, ExternalArguments);
            }
            Log.WriteLine();
            return CompFileList;
        } // end ConvertFromWAV

        static FileInfo[] ConvertToWAV(string CompType, string WAVDirPath, FileInfo[] CompFileList)
        {
            /* Decompresses all lossless audio files in input file list to the original WAV format
             * Inputs:
             *   CompType     String representing what audio compression codec is to be used
             *   Dir          Directory as AATB_DirInfo class instance
             *   WAVDirPath   Output directory
             *   CompFileList List of all FLAC files to be converted
             * Calls external programs, defined in code
             *   shorten.exe
             *   sox.exe
             *   wma2wav.exe
             *   flac.exe
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
                ExternalArguments;
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
                    case SHN:
                        ExternalProgram = "shorten.exe";
                        ExternalArguments = "-x " + DBLQ + CompFilePath + DBLQ
                                          + SPACE + DBLQ + WAVFilePath + DBLQ;
                        break;

                    case AIF:
                        ExternalProgram = "sox.exe";
                        ExternalArguments = DBLQ + CompFilePath + DBLQ
                                          + SPACE + DBLQ + WAVFilePath + DBLQ;
                        break;

                    case WMA:
                        ExternalProgram = "wma2wav.exe";
                        ExternalArguments = "-i " + DBLQ + CompFilePath + DBLQ
                                          + " -o " + DBLQ + WAVFilePath + DBLQ
                                          + " -f";
                        break;

                    case ALAC:
                        ExternalProgram = "qaac64.exe";
                        ExternalArguments = "--alac"
                                          + " --decode"
                                          + " --threading"
                                          + SPACE + DBLQ + CompFilePath + DBLQ
                                          + " -o " + DBLQ + WAVFilePath + DBLQ;
                        break;

                    case FLAC:
                        ExternalProgram = "flac.exe";
                        ExternalArguments = "-d"
                                          + " --force"
                                          + SPACE + DBLQ + CompFilePath + DBLQ
                                          + " --output-name " + DBLQ + WAVFilePath + DBLQ;
                        break;

                    default:
                        Log.WriteLine("*** Undefined external program in ConvertToWAV: " + ExternalProgram);
                        Environment.Exit(0);
                        break;
                }

                // run external process, discard external output
                RunProcess(ExternalProgram, ExternalArguments);
            }
            Log.WriteLine();
            return WAVFileList;
        } // end ConvertToWAV

        static void ConvertWAVBitrate(AATB_DirInfo Dir, FileInfo[] WAVFileList,
                                   string ConvertFromBitrate, string ConvertToBitrate)
        {
            /* Converts FIles in input list from one bitrate to another
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             *     Dir.Path
             *     Dir.ParentPath
             *   WAVFileList  Array of WAV files (any bitrate)
             *   ConvertFromBitrate <bitdepth-samplerate> i.e., 16-44
             *   ConvertToBitrate  <bitdepth-samplerate> i.e., 16-44      
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
            bool
                OutputFileExists = false;
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
                ExternalProgram,
                ExternalArguments;

            ExternalProgram = "sox.exe";
            OutputDirPath = Dir.ParentPath + BACKSLASH + ConvertToBitrate;
            // create output directory or overwrite existing directory
            if ((!Directory.Exists(OutputDirPath) && CreateDir(OutputDirPath))
                || (Directory.Exists(OutputDirPath) && Overwrite))
            {
                Log.WriteLine("  Converting from " + ConvertFromBitrate + " to " + ConvertToBitrate);
                Log.WriteLine("  Input Dir:  " + Dir.Path);
                Log.WriteLine("  Output Dir: " + OutputDirPath);
                Log.Write("    Track: ");
                // extract bitdepth and samplerate from ConvertToBitrate string
                // using hyphen as delimiter, e.g., "16-44" --> (16, 44)
                (BitDepth, SampleRate) = SplitString(ConvertToBitrate, HYPHEN);
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
                    ConversionBitrateComparison = String.Compare(SongBitrate, ConvertFromBitrate, comparisonType: StringComparison.OrdinalIgnoreCase);
                    if (ConversionBitrateComparison != 0)
                    {
                        Log.WriteLine("\n*** Input file bitrate " + SongBitrate + " is not equal to 'conversion from' bitrate");
                        break;
                    }
                    // build output file pathname
                    OutputFileName = Regex.Replace(InputFileName, @ConvertFromBitrate, ConvertToBitrate);
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
                        // run external process, discard external output
                        RunProcess(ExternalProgram, ExternalArguments);
                    }
                    else
                    {
                        OutputFileExists = true;
                        Log.WriteLine("\n*** Output file exists: " + OutputFileName);
                        break;
                    }
                }
                if (OutputFileExists) Log.WriteLine("*** Use overwrite option to replace");
                Log.WriteLine();
            }
            else
                Log.WriteLine("*** Output directory exists, use overwrite option to replace\n"
                            + "    " + OutputDirPath);
        } // end ConvertWAVBitrate
    }
}