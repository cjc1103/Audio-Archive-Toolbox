/*
 * Audio Archive Toolbox
 * Author: Christopher J. Cantwell
 * 
 * Released under the GPL 3.0 software license
 * https://www.gnu.org/licenses/gpl-3.0.en.html
 * 
 * Overview
 * Audio Archive Toolbox (AATB) is a command line utility to perform audio file compression,
 * decompression, tagging, bitrate conversion, and WAV file concatenation. It must be started from
 * the command line in the parent directory to the directory(s) containing the input wav files.
 * The program walks the directory tree under the starting directory to find and operate on
 * relevant audio files in various formats.
 */

using System;
using System.IO;
using System.Text.RegularExpressions;
using IniParser;
using IniParser.Model;

namespace AATB
{
    public partial class AATB_Main
    {
        static AATB_Log Log;
        const int
            // index values for quality parameter lists
            LOWER = 0,
            ACTIVE = 1,
            UPPER = 2;
        const string
            SPACE = " ",
            HYPHEN = "-",
            PERIOD = ".",
            COLON = ":",
            UNDERSCORE = "_",
            EQUALS = "=",
            BACKSLASH = "\\",  // escaped backslash
            DBLQ = "\"",       // escaped double quote
            SPACEHYPHENSPACE = " - ",
            BR1644 = "16-44",
            BR1648 = "16-48",
            BR2444 = "24-44",
            BR2448 = "24-48",
            BR2488 = "24-88",
            BR2496 = "24-96",
            RAW = "Raw",  // subdirectory containing raw wav audio files
            ALLFORMATS = "All Formats",
            ALLBITRATES = "All Bitrates",
            ANYFORMAT = "Any Format",
            ANYBITRATE = "Any Bitrate",
            WAV = "wav",
            AIF = "aif", AIFF = "aiff",
            MP3 = "mp3", MP3F = "mp3f",
            M4A = "m4a", M4AF = "m4af", // M4A file format is MPEG-4 Audio wrapper for AAC
            OGG = "ogg", OGGF = "oggf",
            OPUS = "opus", OPUSF = "opusf",
            SHN = "shn", SHNF = "shnf",
            WMA = "wma", WMAF = "wmaf",
            ALAC = "alac", ALACF = "alacf",
            FLAC = "flac", FLACF = "flacf",
            ALLWAV = "*.wav", ALLFLAC = "*.flac",
            M3U = "m3u",
            MD5 = "md5", ALLMD5 = "*.md5",
            FFP = "ffp", ALLFFP = "*.ffp",
            SHNRPT = "shntool.txt", ALLSHNRPT = "*.shntool.txt",
            INFOTXTdefault = "info.txt",
            INFOCUEdefault = "info.cue",
            NEW = "new",
            LIVE = "Live Recording",
            COMMERCIAL = "Commercial Recording",
            OTHER = "Other",
            INFOFILE = "Infotext",
            CUESHEET = "Cuesheet",
            DIRNAME = "Directory",
            RAWAUDIO = "Raw Audio",
            TRACKEDAUDIO = "Tracked Audio",
            COMPRESSEDAUDIO = "Compressed Audio";
        static string
            // other string values
            RootDir = null,
            ConvertFromBitrate = null,
            ConvertToBitrate = null,
            LOGNAME = "aatb.log",
            LogFilePath = null,
            INFOTXT = null, ALLINFOTXT = null,
            INFOCUE = null, ALLINFOCUE = null,
            // configuration ini file located in c:\Program Files\Audio Archive Toolbox
            ProgramDir = "C:\\Program Files\\Audio Archive Toolbox\\",
            ConfigurationFileName = "aatb_config.ini",
            ConfigurationFilePath = ProgramDir + ConfigurationFileName,
            // regular expression for date format yyyy-mm-dd
            ConcertDateFormat = "((19|20)\\d{2}-\\d{2}-\\d{2})";
        static readonly string[]
            // line delimiters for dos and unix text files
            LineDelimeters = { "\r\n", "\r", "\n" },
            // all audio formats
            // WAV must be the last entry in this list
            AudioFormats = { MP3, M4A, OGG, OPUS, SHN, AIF, WMA, ALAC, FLAC, WAV },
            // allowable compression formats (lossy and lossless)
            AudioCompressionFormats = { MP3, M4A, OGG, OPUS, ALAC, FLAC },
            // allowable decompression formats (lossless)
            AudioDecompressionFormats = { ALAC, FLAC },
            // allowable conversion formats (lossy and lossless)
            AudioConversionFormats = { SHN, AIF, WMA },
            // allowable audio bitrates
            // RAW must be last entry in this list
            AudioBitrates = { BR1644, BR1648, BR2444, BR2448, BR2488, BR2496, RAW },
            // compressed audio directory extensions
            // must correspond to AudioCompressionFormats list
            CompressedDirExtensions = { MP3F, M4AF, OGGF, OPUSF, ALACF, FLACF };
        static List<string>
            // miscellaneous files and directories to delete for cleanup
            FilesToDelete = new List<string>(),
            DirsToDelete = new List<string>(),
            DirsMarkedForDeletion = new List<string>();
        static readonly int[]
            // quality parameter lists { lower, active, upper }
            // must correspond to and be in same order as AudioCompressionFormats list
            mp3Quality = { 0, 0, 4 }, // 0 is best
            aacQuality = { 0, 127, 127 },
            oggQuality = { 0, 8, 10 },
            opusQuality = { 64, 256, 256 }, // placeholder only
            alacQuality = { 0, 0, 0}, // placeholder only
            flacQuality = { 1, 6, 10 };
        static readonly int[][]
            // Two dimensional list of all quality parameters
            // must correspond to and be in same order as AudioCompressionFormats list
            AudioCompressionQuality = 
              { mp3Quality,
                aacQuality,
                oggQuality,
                opusQuality,
                alacQuality,
                flacQuality };
        static bool
            // set default value for bool flags
            ValidConcertDate = false,
            CompressAudio = false,
            VerifyAudio = false,
            DecompressAudio = false,
            ConvertAudio = false,
            JoinWAV = false,
            RenameWAV = false,
            ConvertBitrate = false,
            CreateCuesheet = false,
            DeleteAudio = false,
            DeleteWAVFiles = false,
            DeleteMiscFiles = false,
            Overwrite = false,
            CreateMD5 = false,
            CreateFFP = false,
            CreateSHNRPT = false,
            CreateM3U = false,
            CreateTags = false,
            UseInfotext = false,
            UseCuesheet = false,
            UseLowerCase = false,
            UseTitleCase = false,
            RenameInfoFiles = false,
            UseCurrentDirInfo = false,
            OutputToCurrentDir = false,
            WriteLogMessage = true,
            NoLogMessage = false,
            Verbose = false,
            Debug = false;
        static bool[,]
            // combined audio format and bitrate flag array
            // size must be at least equal to [AudioFormats, AudioBitrates]
            AudioFormatBitrate = new bool[10, 10];

        // = = = = = Main procedure = = = = = //

        static void Main(string[] argv)
        {
            /* Main Procedure
             * initializes data structures
             * gets input from command line, and calls WalkDirectoryTree
             * argv is a list of the command line arguments and options
             *   e.g, arg1[=opt], arg2[=opt], .. argn[=opt]
             */

            string
               arg, opt, UserInput;
            string[]
                ExpandedCommandLineArgs;

            // get starting directory
            RootDir = Directory.GetCurrentDirectory();

            // initialize and write header to log
            LogFilePath = RootDir + BACKSLASH + LOGNAME;
            Log = new AATB_Log(LogFilePath);

            // check to see if command line contains inidebug flag
            //if (argv.Contains("--inidebug")) bool IniDebug = true;
            
            // read configuration file data
            IniData ConfigData = ReadConfiguration(ConfigurationFilePath);

            // get user defined variables from configuration file data, if present
            GetIniData(ConfigData);

            // expand command line with macro definitions from configuration file data, if present
            ExpandedCommandLineArgs = ExpandCommandLineMacros(ConfigData, argv);

            // check arguments exist
            if (ExpandedCommandLineArgs != null)
            {
                // initialize boolean array for selecting format and bitrate
                InitFormatBitrate();

                // parse arguments and options in command line argv
                foreach (string Command in ExpandedCommandLineArgs)
                {
                    // Split each substring into arguments and options, delimited by '='
                    (arg, opt) = SplitString(EQUALS, Command);

                    switch (arg)
                    {
                     
                        // PRIMARY MODES
                        case "-c":
                        case "--compress":
                            CompressAudio = true;
                            break;

                        case "-v":
                        case "--verify":
                            VerifyAudio = true;
                            break;

                        case "-y":
                        case "--convert-to-wav":
                            ConvertAudio = true;
                            break;

                        case "-d":
                        case "--decompress":
                            DecompressAudio = true;
                            break;

                        case "-j":
                        case "--join-wav-files":
                            JoinWAV = true;
                            break;

                        case "-r":
                        case "--rename-wav-files":
                            RenameWAV = true;
                            break;

                        case "-z":
                        case "--convert-to-bitrate":
                            ConvertBitrate = true;
                            switch (opt)
                            {
                                case null: // no options: set conversion bitrate to 16-44
                                    ConvertToBitrate = BR1644;
                                    break;
                                default: // all other options: set conversion bitrate
                                    ConvertToBitrate = opt;
                                    break;
                            }
                            break;

                        case "-s":
                        case "--create-cuesheet":
                            CreateCuesheet = true;
                            break;

                        case "-x":
                        case "--delete":
                            DeleteAudio = true;
                            break;

                        // OTHER FUNCTIONS
                        // MP3 compression
                        case "--mp3":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(MP3, ALLBITRATES);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(MP3, opt);
                                    break;
                            }
                            break;
                        case "--mp3-quality":
                            SetQValue(MP3, opt);
                            break;

                        // AAC compression, M4A container
                        case "--aac":
                        case "--m4a":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(M4A, ALLBITRATES);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(M4A, opt);
                                    break;
                            }
                            break;
                        case "--aac-quality":
                        case "--m4a-quality":
                            SetQValue(M4A, opt);
                            break;

                        // Vorbis compression, OGG container
                        case "--ogg":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(OGG, ALLBITRATES);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(OGG, opt);
                                    break;
                            }
                            break;
                        case "--ogg-quality":
                            SetQValue(OGG, opt);
                            break;

                        // OPUS compression
                        case "--opus":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(OPUS, ALLBITRATES);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(OPUS, opt);
                                    break;
                            }
                            break;
                        case "--opus-quality":
                            SetQValue(OPUS, opt);
                            break;

                        // SHN (Shorten lossless compressed audio format)
                        // decompress/convert to WAV, 16-44 only
                        case "--shn":
                            SetFormatBitrate(SHN, RAW);
                            break;

                        // AIF (Apple native audio format)
                        // convert to WAV only
                        case "--aif":
                            SetFormatBitrate(AIF, RAW);
                            break;

                        // WMA (Windows Media Audio lossless compressed audio format)
                        // convert to WAV only
                        case "--wma":
                            SetFormatBitrate(WMA, RAW);
                            break;

                        // ALAC lossless compression
                        case "--alac":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(ALAC, ALLBITRATES);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(ALAC, opt);
                                    break;
                            }
                            break;
                        case "--alac-quality":
                            // placeholder
                            break;

                        // FLAC lossless compression
                        case "--flac":
                            switch (opt)
                            {
                                case null: // no options: all bitrates + raw
                                case "all":
                                    SetFormatBitrate(FLAC, ALLBITRATES);
                                    SetFormatBitrate(FLAC, RAW);
                                    break;
                                case "raw":
                                    SetFormatBitrate(FLAC, RAW);
                                    break;
                                default:  // all other options: set bitrate
                                    SetFormatBitrate(FLAC, opt);
                                    break;
                            }
                            break;
                        case "--flac-quality":
                            SetQValue(FLAC, opt);
                            break;

                        // WAV file
                        case "--wav":
                            switch (opt)
                            {
                                case null: // no options: all bitrates
                                case "all":
                                    SetFormatBitrate(WAV, ALLBITRATES);
                                    SetFormatBitrate (WAV, RAW);
                                    break;
                                case "raw":
                                    SetFormatBitrate(WAV, RAW);
                                    break;
                                default: // all other options: set bitrate
                                    SetFormatBitrate(WAV, opt);
                                    break;
                            }
                            break;
                        
                        // create md5 checksum file
                        case "--md5":
                            CreateMD5 = true;
                            break;
                        
                        // create FLAC fingerprint file (ffp) file
                        case "--ffp":
                            CreateFFP = true;
                            break;
                        
                        // create shntool report
                        case "--shnrpt":
                            CreateSHNRPT = true;
                            break;
                        
                        // create all checksum and shntool reports
                        case "--all":
                        case "--all-reports":
                            CreateMD5 = true;
                            CreateFFP = true;
                            CreateSHNRPT = true;
                            break;

                        // create m3u playlist
                        case "-p":
                        case "--m3u-playlist":
                            CreateM3U = true;
                            break;

                        // extract metadata from info text file 
                        case "-i":
                        case "--use-infotext":
                            UseInfotext = true;
                            break;

                        // extract metadata from cuesheet 
                        case "-e":
                        case "--use-cuesheet":
                            UseCuesheet = true;
                            break;

                        // create metadata tags
                        case "-t":
                        case "--tag":
                            CreateTags = true;
                            break;

                        // convert to lower case
                        case "--lc":
                        case "--lower-case":
                            UseLowerCase = true;
                            break;

                        // convert to title case
                        case "--tc":
                        case "--title-case":
                            UseTitleCase = true;
                            break;

                        // rename info files
                        case "--ri":
                        case "--rename-info-files":
                            RenameInfoFiles = true;
                            break;

                        // use info files in current directory
                        case "--icd":
                        case "--get-info-from-current-dir":
                            UseCurrentDirInfo = true;
                            break;

                        // output to current directory
                        case "--ocd":
                        case "--output-to-current-dir":
                            OutputToCurrentDir = true;
                            break;

                        // other options
                        // misc files delete
                        case "--misc":
                        case "--misc-files-delete":
                            DeleteMiscFiles = true;
                            break;

                        // overwrite existing files
                        case "-o":
                        case "--overwrite":
                            Overwrite = true;
                            break;

                        // print program options and exit
                        case "-h":
                        case "--help":
                            PrintHelp();
                            Environment.Exit(0);
                            break;

                        // error reporting for external processes
                        case "-hh":
                        case "--verbose":
                            Verbose = true;
                            break;

                        // additonal logging for debugging
                        case "--debug":
                            Debug = true;
                            break;

                        case "--ver":
                        case "--version":
                            // Log initialization has already printed version, so exit
                            Environment.Exit(0);
                            break;

                        // argument not parsed
                        default:
                            Log.WriteLine("Invalid argument: " + Command);
                            Environment.Exit(0);
                            break;
                    }
                }

                // check input validity and print compression options to console
                CheckUserInput();

                // Give user the opportunity to terminate program if input is incorrect
                Console.Write("Do you wish to proceed (y/N)?");
                UserInput = Console.ReadLine();
                Match InputMatch = Regex.Match(UserInput, @"^[yY]");
                if (InputMatch.Success)
                {
                    // write start time stamp to log
                    Log.Start();

                    // start recursive directory search
                    Log.WriteLine("Root Directory: " + RootDir);
                    WalkDirectoryTree(new DirectoryInfo(RootDir));

                    // cleanup - delete directories marked for deletion
                    if (DeleteAudio && DirsMarkedForDeletion != null)
                    {
                        foreach (string dirtodelete in DirsMarkedForDeletion)
                        {
                            Log.WriteLine("Deleting directory " + dirtodelete);
                            DeleteDir(dirtodelete);
                        }
                    }

                    // write end time stamp to log
                    Log.End();
                }
                else
                    Log.WriteLine(">>> Terminated by user input");
            }
            else
                // no arguments - print list of commands and exit
                PrintHelp();

        } // end Main
    }
}