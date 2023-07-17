using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void CheckUserInput()
        {
            /* Checks user input for validity, and terminates program for invalid inputs
            *  Inputs: Flags and arguments are set from the command line
            *  Outputs: Log messages
            */
            int i;
            bool FoundValidFormat;

            // sanity check for conflicts and errors in command line parameters
            // if any of these conditions is true, the program is terminated immediately  
            // check mutually exclusive flags, only one should be set
            if (Convert.ToInt32(CompressAudio )
              + Convert.ToInt32(VerifyAudio )
              + Convert.ToInt32(ConvertAudio)
              + Convert.ToInt32(DecompressAudio)
              + Convert.ToInt32(JoinWAV)
              + Convert.ToInt32(RenameWAV)
              + Convert.ToInt32(ConvertBitrate)
              + Convert.ToInt32(CreateCuesheet)
              + Convert.ToInt32(DeleteAudio) != 1)
            {
                Log.WriteLine("Input error: Conflicting options\n"
                   + "Choose compress, verify, decompress, join, rename, delete, convert, convert bitrate, or create cuesheet");
                Environment.Exit(0);
            }
            if (CreateCuesheet && UseCuesheet)
            {
                Log.WriteLine("Input error: Conflicting cuesheet options\n"
                   + "Choose create cuesheet --create-cuesheet, or use cuesheet metadata --compress --use-cuesheet");
                Environment.Exit(0);
            }
            if (UseCuesheet && UseInfotext)
            {
                Log.WriteLine("Input error: Conflicting metadata options\n"
                   + "Choose use cuesheet metadata --use-cuesheet, or use info text metadata --use-infotext");
                Environment.Exit(0);
            }

            // primary modes
            if (CompressAudio )
            {
                Log.WriteLine("Compress WAV audio files");
                if (CheckFormatBitrate(WAV, ANYBITRATE))
                {
                    Log.WriteLine("Input error: WAV is not a valid audio compression format");
                    Environment.Exit(0);
                }
                else
                {
                    // verify at least one compression format selected, including raw
                    if (!CheckFormatBitrate(ANYFORMAT, ANYBITRATE)
                        && !CheckFormatBitrate(FLAC, RAW))
                    {
                        Log.WriteLine("Input error: Specify at least one compressed audio format and bitrate");
                        Environment.Exit(0);
                    }
                }
                PrintCompressionOptions();

                // MD5 Checksum
                if (CreateMD5) Log.WriteLine("Create MD5 checksum");
                // FFP and shntool otions, excluding RAW 
                if (CheckFormatBitrate(FLAC, ANYBITRATE))
                {
                    // FLAC Fingerprint Checksum
                    if (CreateFFP)
                        Log.WriteLine("Create FLAC Fingerprint (FFP)");
                    // shntool report
                    if (CreateSHNRPT)
                        Log.WriteLine("Create SHNTool report (SHNRPT)");
                }
                // M3U Playlist
                if (CreateM3U)
                    Log.WriteLine("Create M3U playlist");
            }

            if (VerifyAudio )
            {
                Log.WriteLine("Verify compressed audio files");
                if (CheckFormatBitrate(FLAC, RAW))
                {
                    Log.WriteLine("Note: Verification of raw audio files is not supported");
                    // continue with program, other modes are supported
                }
                // check at least one flag is set
                if (!CreateMD5 && !CreateFFP && !CreateSHNRPT && !CreateTags & !CreateM3U)
                {
                    Log.WriteLine("Input error: Specify options [--md5 --ffp --shn]|--all-reports, --tag, --m3u");
                    Environment.Exit(0);
                }
                // if no format or bitrate is set, then assume all formats and bitrates
                if (!CheckFormatBitrate(ANYFORMAT, ANYBITRATE)
                    && !CheckFormatBitrate(ANYFORMAT, RAW))
                    SetFormatBitrate(ALLFORMATS, ALLBITRATES);
                PrintCompressionOptions();
                // MD5 Checksum
                if (CreateMD5) Log.WriteLine("  Verify MD5 checksum");
                // FFP and shntool otions, excluding RAW 
                if (CheckFormatBitrate(FLAC, ANYBITRATE))
                {
                    // FLAC Fingerprint Checksum
                    if (CreateFFP)
                        Log.WriteLine("  Verify FLAC Fingerprint (FFP)");
                    // shntool report
                    if (CreateSHNRPT)
                        Log.WriteLine("  Verify SHNTool report (SHNRPT)");
                }
                // ID3 tags
                if (CreateTags)
                    Log.WriteLine("Create/update ID3 tags and MD5 checksum");
                // M3U Playlist
                if (CreateM3U)
                    Log.WriteLine("Create/Update M3U playlist");
            }

            if (DecompressAudio)
            {
                FoundValidFormat = false;
                for (i = 0; i <= AudioDecompressionFormats.Length - 1; i++)
                {
                    if (CheckFormatBitrate(AudioDecompressionFormats[i], ANYBITRATE)
                        || CheckFormatBitrate(AudioDecompressionFormats[i], RAW))
                        FoundValidFormat = true;
                }
                if (!FoundValidFormat)
                {
                    Log.WriteLine("No valid decompression format specified");
                    Environment.Exit(0);
                }
                Log.WriteLine("Decompress audio files to WAV");
                PrintCompressionOptions();
            }
            
            if (ConvertAudio)
            {
                FoundValidFormat = false;
                for (i = 0; i <= AudioConversionFormats.Length - 1; i++)
                {
                    if (CheckFormatBitrate(AudioConversionFormats[i], ANYBITRATE))
                        FoundValidFormat = true;
                }
                if (!FoundValidFormat)
                {
                    Log.WriteLine("No valid conversion format specified");
                    Environment.Exit(0);
                }
                Log.WriteLine("Convert audio files to WAV");
                PrintCompressionOptions();
            }

            if (JoinWAV)
            {
                Log.WriteLine("Join tracked WAV audio files");
                // check for unique WAV bitrate
                if (!CheckFormatBitrate(WAV, ANYBITRATE))
                {
                    Log.WriteLine("Input error: A WAV bitrate was not specified");
                    Environment.Exit(0);
                }
                if (!CheckUniqueBitrate(WAV))
                {
                    Log.WriteLine("Input error: Multiple or invalid WAV conversion bitrates selected");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            if (RenameWAV)
            {
                Log.WriteLine("Rename WAV audio files");
                if (!CheckFormatBitrate(WAV, ANYBITRATE))
                {
                    Log.WriteLine("Input error: A WAV bitrate was not specified");
                    Environment.Exit(0);
                }
                if (!CheckUniqueBitrate(WAV))
                {
                    Log.WriteLine("Input error: Multiple or invalid WAV conversion bitrates selected");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            if (DeleteAudio )
            {
                Log.WriteLine("Delete redundant audio files");
                if (!CheckFormatBitrate(WAV, ANYBITRATE))
                {
                    Log.WriteLine("Input error: Select WAV bitrate '--wav=[<bitrate>|raw|all]'");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            if (ConvertBitrate)
            {
                // Conversion of WAV files to another bitrate
                // (1) --wav=<ConversionFromBitrate>
                //     Multiple or raw bitrates are not allowed
                // (2) -z|--convert-to-bitrate=<ConversionToBitrate>
                //     Must be different from ConversionFromBitrate>
                ConversionFromBitrate = FirstBitrateSet(WAV);
                Log.WriteLine("Convert WAV audio files from "
                              + ConversionFromBitrate + " to " + ConversionToBitrate);
                if (!CheckFormatBitrate(WAV, ConversionFromBitrate))
                {
                    Log.WriteLine("Input error: Conversion from bitrate not valid. Use --wav=<bitrate>");
                    Environment.Exit(0);
                }
                if (!CheckFormatBitrate(WAV, ConversionToBitrate))
                {
                    Log.WriteLine("Input error: Conversion to bitrate not valid. Use -z|--convert-to-bitrate=<bitrate>");
                    Environment.Exit(0);
                }
                if (!CheckUniqueBitrate(WAV))
                {
                    Log.WriteLine("Input error: Multiple or invalid WAV conversion bitrates selected");
                    Environment.Exit(0);
                }
                if (ConversionFromBitrate == ConversionToBitrate)
                { 
                    Log.WriteLine("Input error: Conversion from and to bitrate arguments must be different");
                    Environment.Exit(0);
                }
            }

            if (CreateCuesheet)
            {
                Log.WriteLine("  Create cuesheet from tracked WAV audio files");
                if (!CheckUniqueBitrate(WAV))
                {
                    Log.WriteLine("Input error: Specify only one WAV bitrate");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            // secondary options and other input checking
            if ((CompressAudio  || VerifyAudio  || CreateCuesheet) && UseInfotext)
                Log.WriteLine("Use metadata from information file (info.txt)");

            if ((CompressAudio  || VerifyAudio ) && UseCuesheet)
                Log.WriteLine("Use metadata from cuesheet");

            if (UseLowerCase && UseTitleCase)
            {
                Log.WriteLine("Input error: Can't use both lower case and title case options");
                Environment.Exit(0);
            }

            if (UseLowerCase)
                Log.WriteLine("Convert directory names to lower case");

            if (UseTitleCase)
                Log.WriteLine("Convert directory names to title case");

            if (RenameInfoFiles)
                Log.WriteLine("Rename info files according to directory name convention");

            if (UseCurrentDirInfo)
                Log.WriteLine("Use current directory for info files");

            if (OutputToCurrentDir)
                Log.WriteLine("Output converted files to current directory");

            if (!DeleteAudio  && Overwrite)
                Log.WriteLine("Overwrite existing files");
            
            if (Verbose)
                Log.WriteLine("Verbose mode");

            if (Debug)
            {
                Log.WriteLine("Debug mode");
                // print out AudioFormatBitrate array
                PrintFormatBitrate();
            }

        } // end CheckUserInput

        static void PrintCompressionOptions()
        {
            /* Prints out specified audio format with bitrate flags set for that format
            *  will not print anything if no bitrate is set for the specified format
            *  Note: ANYBITRATE does not include RAW, so must check for both
            *  Inputs: None
            *  Outputs: Log messages
            */
            int i, j;
            string AudioFormat, AudioBitrate;

            // loop through all audio compression formats
            for (i = 0; i <= AudioCompressionFormats.Length - 1; i++)
            {
                AudioFormat = AudioCompressionFormats[i];
                // check for any valid bitrates for each format
                if (CheckFormatBitrate(AudioFormat, ANYBITRATE)
                    || CheckFormatBitrate(AudioFormat, RAW))
                {
                    // print audio format
                    Log.Write("  " + AudioFormat.ToUpper());
                    // print all bitrate(s)
                    if (CheckFormatBitrate(AudioFormat, ALLBITRATES))
                        Log.Write(" (All bitrates)");
                    else
                        // print valid bitrates
                        for (j = 0; j <= AudioBitrates.Length - 1; j++)
                        {
                            AudioBitrate = AudioBitrates[j];
                            if (CheckFormatBitrate(AudioFormat, AudioBitrate))
                                Log.Write(" (" + AudioBitrate + ")");
                        }
                    // print q values in AudioCompressionQuality list for CompressAudio function
                    if (CompressAudio)
                    {
                        Log.Write("  <" + Convert.ToString(AudioCompressionQuality[i][LOWER])
                                + ".. q=" + Convert.ToString(AudioCompressionQuality[i][ACTIVE])
                                + " .." + Convert.ToString(AudioCompressionQuality[i][UPPER]) + ">");
                    }
                    // flush print buffer
                    Log.WriteLine();
                }
            }

            // loop through all audio conversion formats
            for (i = 0; i <= AudioConversionFormats.Length - 1; i++)
            {
                AudioFormat = AudioConversionFormats[i];
                // check only for raw flag
                if (CheckFormatBitrate(AudioFormat, RAW))
                    Log.WriteLine("  " + AudioFormat.ToUpper() + " (Any bitrate)");
            }

        } // end PrintCompressionOptions
    }
}