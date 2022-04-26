using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static string GetTrackBitrate(string AudioFilePath)
        {
            /* Calculates the audio bit rate of an audio file
             * Inputs:
             *   AudioFilePath  pathname of audio file
             * Calls external program:
             *   sox (Sound Output eXchange utility)
             *     --info : returns information on audio files
             *     -b : returns audio bitrate, e.g 24 = 24bit
             *     -r : returns sample rate, e.g. 48000 = 48Khz
             * Outputs:
             *   Audio file bitrate in format <bitlength-samplerate>
             *   e.g, 24-48
             */
            string
                BitLength,
                SampleRate,
                BitRate,
                ExternalProgram = "sox.exe",
                ExternalArguments,
                ExternalOutput;

            // get bit length
            ExternalArguments = "--info"
                              + " -b"
                              + SPACE + DBLQ + AudioFilePath + DBLQ;
            ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
            // trim newline character from end
            BitLength = ExternalOutput.TrimEnd();

            // get bitrate
            ExternalArguments = "--info"
                              + " -r"
                              + SPACE + DBLQ + AudioFilePath + DBLQ;
            ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
            // extract first two characters to get sample rate in thousands
            SampleRate = ExternalOutput.Substring(0, 2);

            // assemble bitrate
            BitRate = BitLength + HYPHEN + SampleRate;
            return BitRate;
        } // end GetTrackBitrate

        static string GetTrackDuration(string AudioFilePath)
        {
            /* Calculates the duration of an audio file in seconds
             * Inputs:
             *   AudioFilePath  Compressed audio file path
             *   Note: This procedure does not verify input audio file format. If input
             *   file is in an unsupported format, it will fail gracefully and return null
             * Calls external program:
             *   MediaInfo utility
             *     --Inform=Audio;%Duration% : returns duration of audio file in milliseconds
             *       (e.g. "68567" = 68.567 secs) An invalid input returns nothing
             * Outputs:
             *   Returns the the number of seconds as a string (truncated, not rounded)
             */
            string
                DurationMSec = null,
                ExternalProgram = "Mediainfo.exe",
                ExternalArguments,
                ExternalOutput;

            ExternalArguments = "--Inform=Audio;%Duration%"
                              + SPACE + DBLQ + AudioFilePath + DBLQ;
            // run external program
            ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);

            // check for invalid output - probably using GUI version of MediaInfo
            if (ExternalOutput == null)
            {
                Log.WriteLine("*** MediaInfo returns null, are you using the CLI version?");
                Environment.Exit(0);
            }
            // External Output is song duration in milliseconds with three trailing spaces
            if (ExternalOutput.Length >= 3)
                DurationMSec = ExternalOutput.Substring(0, ExternalOutput.Length - 2);

            // return duration in milliseconds
            return DurationMSec;
        } // end GetTrackDuration
    }
}