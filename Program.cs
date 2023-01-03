/*
 * Audio Archive Toolbox
 * Author: Christopher J. Cantwell
 * 
 * Released under the GPL 3.0 software license
 * https://www.gnu.org/licenses/gpl-3.0.en.html
 * 
 * Overview
 * Audio Archive Toolbox (AATB) is a command line utility to perform audio file compression,
 * decompression, tagging, and conversion from one bitrate to another. It must be started from
 * the command line in the parent directory to the directory(s) containing the input wav files.
 * The program walks the directory tree under the starting directory to find and operate on
 * relevant audio files in various formats.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            BLANK = "",
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
            RAW = "Audio",  // subdirectory containing raw wav audio files
            ALLFORMATS = "allformats",
            ALLBITRATES = "allbitrates",
            ANYFORMAT = "anyformat",
            ANYBITRATE = "anybitrate",
            WAV = "wav", ALLWAV = "*.wav", // subdir name for wav files is bitrate
            MP3 = "mp3", MP3F = "mp3f",
            M4A = "m4a", M4AF = "m4af", // M4A file format is MPEG-4 Audio wrapper for AAC
            OGG = "ogg", OGGF = "oggf",
            OPUS = "opus", OPUSF = "opusf",
            ALAC = "alac", ALACF = "alacf",
            FLAC = "flac", ALLFLAC = "*.flac", FLACF = "flacf",
            M3U = "m3u",
            MD5 = "md5", ALLMD5 = "*.md5",
            FFP = "ffp", ALLFFP = "*.ffp",
            SHN = "shntool.txt", ALLSHN = "*.shntool.txt",
            INFOTXT = "info.txt", ALLINFOTXT = "*.info.txt",
            INFOCUE = "info.cue", ALLINFOCUE = "*.info.cue",
            NEW = "new",
            LOGNAME = "aatb.log.txt",
            LIVE = "Live Recording",
            CD = "Commercial CD",
            OTHER = "Other",
            INFOFILE = "Infotext File",
            CUESHEET = "Cuesheet",
            DIRNAME = "Directory Name",
            RAWAUDIO = "Raw Audio Dir",
            TRACKEDAUDIO = "Tracked Audio Dir",
            COMPRESSEDAUDIO = "Compressed Audio Dir";
        static bool
            // set default value
            CompressAudio = false,
            VerifyAudio = false,
            DeleteAudio = false,
            DecompressAudio = false,
            JoinWAV = false,
            ConvertAudioBitrate = false,
            Overwrite = false,
            CreateMD5 = false,
            CreateFFP = false,
            CreateSHN = false,
            CreateM3U = false,
            CreateCuesheet = false,
            UpdateTags = false,
            UseInfotext = false,
            UseCuesheet = false,
            UseLowerCase = false,
            UseTitleCase = false,
            RenameInfoFiles = false,
            UseCurrentDirInfo = false,
            WriteLogMessage = true,
            NoLogMessage = false,
            Verbose = false,
            Debug = false;
        static string
            LogFilePath = null,
            RootDir = null,
            ConversionFromBitrate = null,
            ConversionToBitrate = null;
        static bool[,]
            // combined audio format and bitrate flag array
            // this must be at least the size of AudioFormats, AudioBitrates arrays
            AudioFormatBitrate = new bool[7, 7];
        static readonly string[]
            // line delimiters for dos and unix text files
            LineDelimeters = { "\r\n", "\r", "\n" },
            // audio formats (FLAC and WAV must be the last two entries in this list)
            AudioFormats = { MP3, M4A, OGG, OPUS, ALAC, FLAC, WAV },
            // audio bitrates (RAW must be last entry in this list)
            AudioBitrates = { BR1644, BR1648, BR2444, BR2448, BR2488, BR2496, RAW },
            // compressed audio directory extensions
            CompressedDirExtensions = { MP3F, M4AF, OGGF, OPUSF, ALACF, FLACF },
            // miscellaneous files to delete for cleanup
            FilesToDelete = { ".npr", ".HDP", ".H2", ".sfk", ".bak", ".BAK", "BAK.VIP", ".peak", ".reapeaks", ".tmp" },
            // miscellaneous directories to delete for cleanup
            DirsToDelete = { "Images" };
        static int[]
            // quality parameter lists { lower, active, upper }
            mp3Quality = { 0, 0, 4 }, // 0 is best
            aacQuality = { 0, 127, 127 },
            oggQuality = { 0, 8, 10 },
            opusQuality = { 64, 256, 256 },
            alacQuality = { 0, 0, 0}, // not applicable, placeholder only
            flacQuality = { 1, 6, 10 };
        static int[][]
            // Two dimensional list of all quality parameters - must be in same order as AudioFormats list
            CompressedAudioQuality = new[]
              { mp3Quality,
                aacQuality,
                oggQuality,
                opusQuality,
                alacQuality,
                flacQuality };
        static List<string>
            DirsMarkedForDeletion = new List<string>();

        // = = = = = Main procedure = = = = = //

        static void Main(string[] argv)
        {
            // main procedure
            // argv is a list of the command line arguments and options
            //   arg1[=opt], arg2[=opt], .. argn[=opt]

            string
               arg, opt,
               UserInput;

            // get starting directory
            RootDir = Directory.GetCurrentDirectory();

            // initialize and write header to log
            LogFilePath = RootDir + BACKSLASH + LOGNAME;
            Log = new AATB_Log(LogFilePath);

            // check arguments exist
            if (argv.Length > 0)
            {
                // initialize boolean array for selecting format and bitrate
                InitFormatBitrate();

                // parse arguments and options in command line argv
                foreach (string s in argv)
                {
                    // Split each substring s into arguments and options, delimited by '='
                    (arg, opt) = SplitString(s, EQUALS);
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

                        case "-d":
                        case "--decompress":
                            DecompressAudio = true;
                            break;

                        case "-j":
                        case "--join":
                            JoinWAV = true;
                            break;

                        case "-x":
                        case "--delete":
                            DeleteAudio = true;
                            break;

                        case "-z":
                        case "--convert-to-bitrate":
                            ConvertAudioBitrate = true;
                            switch (opt)
                            {
                                case null: // no options: set conversion bitrate to 16-44
                                    ConversionToBitrate = BR1644;
                                    break;
                                default: // all other options: set conversion bitrate
                                    ConversionToBitrate = opt;
                                    break;
                            }
                            break;

                        case "-r":
                        case "--create-cuesheet":
                            CreateCuesheet = true;
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

                        // ALAC compression
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

                        // FLAC compression
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
                                case null: // no options: all bitrates + raw
                                case "all":
                                    SetFormatBitrate(WAV, ALLBITRATES);
                                    SetFormatBitrate(WAV, RAW);
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
                        case "--shn":
                            CreateSHN = true;
                            break;
                        
                        // create all checksum and shntool reports
                        case "-a":
                        case "--all-reports":
                            CreateMD5 = true;
                            CreateFFP = true;
                            CreateSHN = true;
                            break;

                        // create m3u playlist
                        case "-m":
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
                            UpdateTags = true;
                            break;

                        // convert to lower case
                        case "-l":
                        case "--lower-case":
                            UseLowerCase = true;
                            break;

                        // convert to title case
                        case "-u":
                        case "--title-case":
                            UseTitleCase = true;
                            break;

                        // rename info files
                        case "-r":
                        case "--rename-infofiles":
                            RenameInfoFiles = true;
                            break;

                        case "-f":
                        case "--use-currentdirinfo":
                            UseCurrentDirInfo = true;
                            break;

                        // overwrite existing files
                        case "-o":
                        case "--overwrite":
                            Overwrite = true;
                            break;

                        // other options
                        case "--cjc":
                            // that's my initials.. :-)
                            // --compress --aac|m4a=all --flac=all --all-reports --m3u-playlist
                            CompressAudio = true;
                            SetFormatBitrate(M4A, ALLBITRATES);
                            SetFormatBitrate(FLAC, ALLBITRATES);
                            SetFormatBitrate(FLAC, RAW);
                            CreateMD5 = true;
                            CreateFFP = true;
                            CreateSHN = true;
                            CreateM3U = true;
                            break;

                        // error reporting for external processes
                        case "-b":
                        case "--verbose":
                            Verbose = true;
                            break;

                        // print program options and exit
                        case "-h":
                        case "--help":
                            PrintHelp();
                            Environment.Exit(0);
                            break;

                        // additonal logging for debugging
                        case "--debug":
                            Debug = true;
                            break;

                        // argument not parsed
                        default:
                            Log.WriteLine("Invalid argument: " + s);
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