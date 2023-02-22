﻿using System;

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

            // sanity check for conflicts and errors in command line parameters
            // if any of these conditions is true, the program is terminated immediately  
            // check mutually exclusive flags, only one should be set
            if (Convert.ToInt32(CompressAudio )
              + Convert.ToInt32(VerifyAudio )
              + Convert.ToInt32(DecompressAudio)
              + Convert.ToInt32(JoinWAV)
              + Convert.ToInt32(DeleteAudio)
              + Convert.ToInt32(ConvertAIF)
              + Convert.ToInt32(ConvertBitrate)
              + Convert.ToInt32(CreateCuesheet) != 1)
            {
                Log.WriteLine("Input error: Conflicting options\n"
                   + "Choose compress, verify, decompress, join, delete, convert aif, convert bitrate, or create cuesheet");
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
                    if (CreateSHN)
                        Log.WriteLine("Create SHNTool report (SHN)");
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
                if (!CreateMD5 && !CreateFFP && !CreateSHN && !UpdateTags & !CreateM3U)
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
                    if (CreateSHN)
                        Log.WriteLine("  Verify SHNTool report (SHN)");
                }
                // ID3 tags
                if (UpdateTags)
                    Log.WriteLine("Create/update ID3 tags and MD5 checksum");
                // M3U Playlist
                if (CreateM3U)
                    Log.WriteLine("Create/Update M3U playlist");
            }

            if (DecompressAudio)
            {
                Log.WriteLine("Decompress FLAC audio files");
                // check for unique FLAC bitrate or raw
                if (!CheckUniqueBitrate(FLAC)
                    && !CheckFormatBitrate(FLAC, RAW))
                {
                    Log.WriteLine("Input error: Use '--flac=[<bitrate>|raw|all]'");
                    Environment.Exit(0);
                }
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

            if (ConvertAIF)
            {
                Log.WriteLine("Convert AIF format audio to WAV format");
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

            if (UseTitleCase && UseLowerCase)
            {
                Log.WriteLine("Input error: Can't use both lower case and title case options");
                Environment.Exit(0);
            }
            if (UseTitleCase)
                Log.WriteLine("Convert directory names to title case");

            if (UseLowerCase)
                Log.WriteLine("Convert directory names to lower case");

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
            string AudioCompFormat;
            int i, j;

            // Loop through all compressed audio formats
            for (i = 0; i <= AudioFormats.Length - 1; i++)
            {
                AudioCompFormat = AudioFormats[i];
                // check for any valid bitrates for each format
                if (CheckFormatBitrate(AudioCompFormat, ANYBITRATE)
                    || CheckFormatBitrate(AudioCompFormat, RAW))
                {
                    Log.Write("  " + AudioCompFormat.ToUpper());
                    if (CheckFormatBitrate(AudioCompFormat, ALLBITRATES))
                        Log.Write(" (All bitrates)");
                    else
                        // print all valid bitrates, RAW is last bitrate in array
                        for (j = 0; j <= AudioBitrates.Length - 1; j++)
                            if (AudioFormatBitrate[i, j])
                                Log.Write(" (" + AudioBitrates[j] + ")");
                    //if (CheckFormatBitrate(AudioCompFormat, RAW))
                    //    Log.Write(" (" + RAW + ")");

                    // print q values in CompressedAudioQuality list for CompressAudio function
                    if (CompressAudio)
                    {
                        Log.Write("  <" + Convert.ToString(CompressedAudioQuality[i][LOWER])
                                + ".. q=" + Convert.ToString(CompressedAudioQuality[i][ACTIVE])
                                + " .." + Convert.ToString(CompressedAudioQuality[i][UPPER]) + ">");
                    }
                    // flush print buffer
                    Log.WriteLine();
                }
            }
        } // end PrintCompressionOptions
    }
}