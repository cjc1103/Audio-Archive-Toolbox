using System;
using System.Linq;

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
            int i, j, BitrateSet;

            // check for conflicts and errors in command line parameters
            // check mutually exclusive flags, only one should be set
            if (Convert.ToInt32(CompressAudio )
              + Convert.ToInt32(VerifyAudio )
              + Convert.ToInt32(DecompressAudio)
              + Convert.ToInt32(DeleteAudio )
              + Convert.ToInt32(JoinWAV)
              + Convert.ToInt32(ConvertAudioBitrate)
              + Convert.ToInt32(CreateCuesheet) != 1)
            {
                Log.WriteLine("Error: Conflicting options\n"
                   + "Choose compress, verify, decompress, delete, create cuesheet, or convert wav bitrate");
                Environment.Exit(0);
            }
            if (CreateCuesheet && UseCuesheet)
            {
                Log.WriteLine("Error: Conflicting Cue options\n"
                   + "Choose create cuesheet --create-cuesheet, or use cuesheet metadata --compress --use-cuesheet");
                Environment.Exit(0);
            }
            if (UseCuesheet && UseInfotext)
            {
                Log.WriteLine("Error: Conflicting metadata options\n"
                   + "Choose use cuesheet metadata --use-cuesheet, or use info text metadata --use-infotext");
                Environment.Exit(0);
            }

            // Primary modes
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
                        Log.WriteLine("Input error: specify at least one compressed audio format and bitrate\n"
                                    + "             raw is only valid for flac format");
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
                // ID3 tags
                if (CreateTags)
                    Log.WriteLine("Create ID3 tags");
                // M3U Playlist
                if (CreateM3U)
                    Log.WriteLine("CreateM3U playlist");
            }

            if (VerifyAudio )
            {
                Log.WriteLine("Verify compressed audio files");
                if (CheckFormatBitrate(ANYFORMAT, RAW))
                {
                    Log.WriteLine("Verification of raw audio files is not supported");
                }
                // if no format or bitrate is set, then assume all formats and bitrates
                if (!CheckFormatBitrate(ANYFORMAT, ANYBITRATE)
                    && !CheckFormatBitrate(ANYFORMAT, RAW))
                    SetFormatBitrate(ALLFORMATS, ALLBITRATES);

                if (CreateMD5 || CreateFFP || CreateSHN || CreateTags || CreateM3U)
                {
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
                    if (CreateTags)
                        Log.WriteLine("Create/update ID3 tags and MD5 checksum");
                    // M3U Playlist
                    if (CreateM3U)
                        Log.WriteLine("Create/Update M3U playlist");
                }
                else
                {
                    Log.WriteLine("Input error: specify options [--md5 --ffp --shn]|--all-reports, --tag, --m3u");
                    Environment.Exit(0);
                }
            }

            if (DecompressAudio)
            {
                if (CheckFormatBitrate(FLAC, ANYBITRATE)
                    || CheckFormatBitrate(FLAC, RAW))
                    Log.WriteLine("Decompress FLAC audio files");
                else
                {
                    Log.WriteLine("Input error: specify flac format and bitrate/raw to decompress");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            if (DeleteAudio )
            {
                Log.WriteLine("Delete redundant audio files");
                if (!CheckFormatBitrate(WAV, ANYBITRATE))
                {
                    Log.WriteLine("Input error: Use '--wav=[<bitrate>|raw|all]' to delete redundant wav files");
                    Environment.Exit(0);
                }
                PrintCompressionOptions();
            }

            if (JoinWAV)
            {
                Log.WriteLine("Join tracked WAV audio files");
                PrintCompressionOptions();
            }

            if (ConvertAudioBitrate)
            {
                // Conversion of WAV files to another bitrate
                // (1) --wav=<ConversionFromBitrate>
                //     Only the first WAV bitrate set in AudioFormatBitrate list is used
                //     Multiple WAV bitrates are undefined
                // (2) -z|--convert-to-bitrate=<ConversionToBitrate>
                //     Multiple WAV bitrates are undefined, ignore RAW flags
                ConversionFromBitrate = null;
                BitrateSet = 0;
                i = Array.IndexOf(AudioFormats, WAV);
                for (j = 0; j <= AudioBitrates.Length - 2; j++)
                    if (AudioFormatBitrate[i, j])
                    {
                        ConversionFromBitrate = AudioBitrates[j];
                        BitrateSet++;
                    }
                if (BitrateSet != 1)
                {
                    Log.WriteLine("Input error: Multiple wav conversion bitrates and/or raw format selected");
                    Environment.Exit(0);
                }
                if (ConversionFromBitrate == null)
                {
                    Log.WriteLine("Input error: Conversion from bitrate not set. Use --wav=<bitrate>");
                    Environment.Exit(0);
                }
                else if (ConversionFromBitrate == ConversionToBitrate)
                { 
                    Log.WriteLine("Input error: Conversion from and to bitrate arguments must be different");
                    Environment.Exit(0);
                }
                else
                    Log.WriteLine("Convert WAV audio files from "
                        + ConversionFromBitrate + " to " + ConversionToBitrate);
            }

            if (CreateCuesheet)
            {
                Log.WriteLine("  Create cuesheet from WAV audio files");
                PrintCompressionOptions();
                if (!CheckUniqueBitrate(WAV))
                {
                    Log.WriteLine("Input error: specify only one WAV bitrate to create a cuesheet from");
                    Environment.Exit(0);
                }
            }

            // Secondary options
            if ((CompressAudio  || VerifyAudio  || CreateCuesheet) && UseInfotext)
                Log.WriteLine("Use metadata from information file (info.txt)");

            if ((CompressAudio  || VerifyAudio ) && UseCuesheet)
                Log.WriteLine("Use metadata from cuesheet");

            if (UseTitleCase && UseLowerCase)
            {
                Log.WriteLine("Input error, can't use lower case and title case together");
                Environment.Exit(0);
            }
            if (UseTitleCase)
                Log.WriteLine("Convert directory names to title case");

            if (UseLowerCase)
                Log.WriteLine("Convert directory names to lower case");

            if (!DeleteAudio  && Overwrite)
                Log.WriteLine("Overwrite existing files");

            // debug mode - print out AudioFormatBitrate array
            if (Debug) PrintFormatBitrate();
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

            // RAW flag is only valid for FLAC and WAV so no need to recheck
            for (i = 0; i <= AudioFormats.Length - 1; i++)
            {
                AudioCompFormat = AudioFormats[i];
                if (CheckFormatBitrate(AudioCompFormat, ANYBITRATE)
                    || CheckFormatBitrate(AudioCompFormat, RAW))
                {
                    Log.Write("  " + AudioCompFormat.ToUpper());
                    if (CheckFormatBitrate(AudioCompFormat, ALLBITRATES))
                        Log.Write(" (All bitrates)");
                    else
                        for (j = 0; j <= AudioBitrates.Length - 2; j++)
                            if (AudioFormatBitrate[i, j])
                                Log.Write(" (" + AudioBitrates[j] + ")");
                    if (CheckFormatBitrate(AudioCompFormat, RAW))
                        Log.Write(" (" + RAW + ")");

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