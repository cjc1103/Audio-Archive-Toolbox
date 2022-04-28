using System;
using System.Text.RegularExpressions;

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
            SampleRate = ExternalOutput[..2];

            // assemble bitrate
            BitRate = BitLength + HYPHEN + SampleRate;
            return BitRate;
        } // end GetTrackBitrate

        static string GetTrackDuration(string AudioFilePath)
        {
            /* Calculates the duration of an audio file
             * Inputs:
             *   AudioFilePath  input audio file path
             *   Note: This procedure does not verify input audio file format. If input
             *   file is in an unsupported format, it will fail gracefully and return null
             * Calls external program:
             *   MediaInfo utility (CLI version)
             *     --Inform=Audio;%Duration% : returns duration of audio file in milliseconds
             *       (e.g. "68567" = 68.567 secs) An invalid input returns nothing
             * Outputs:
             *   Returns audio file duration in seconds
             */
            string
                ExternalProgram = "Mediainfo.exe",
                ExternalArguments,
                ExternalOutput,
                DurationSec;
            decimal
                decDurationSec;

            ExternalArguments = "--Inform=Audio;%Duration%"
                              + SPACE + DBLQ + AudioFilePath + DBLQ;
            // run external program
            // output is a string representing song duration in milliseconds
            ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);

            // check for invalid output - perhaps using GUI instead of CLI version of MediaInfo
            if (ExternalOutput == null)
            {
                Log.WriteLine("*** MediaInfo returns null, are you using the CLI version?");
                Environment.Exit(0);
            }
            // remove trailing spaces
            ExternalOutput = Regex.Replace(ExternalOutput, @"\s*$", "");
            // convert to decimal seconds
            decDurationSec = Convert.ToDecimal(ExternalOutput) / 1000;
            // round file duration to nearest second and convert back to string
            DurationSec = Convert.ToString(Decimal.Round(decDurationSec));

            return DurationSec;
        } // end GetTrackDuration
    }
}