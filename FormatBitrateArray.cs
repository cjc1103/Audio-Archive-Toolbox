using System;

namespace AATB
{
    public partial class AATB_Main
    {
        // methods in this section operate on the global AudioFormatBitrate array
        static void InitFormatBitrate()
        {
            /* Initializes boolean AudioFormatBitrate array values to false
             * Inputs:  None
             * Outputs: Array is initialized
            */
            int i, j;
            for (i = 0; i <= AudioFormats.Length - 1; i++)
                for (j = 0; j <= AudioBitrates.Length - 1; j++)
                    AudioFormatBitrate[i, j] = false;
        } // end InitFormatBitrate

        static void SetFormatBitrate(string Format, string Bitrate)
        {
            /* Sets boolean flag in AudioFormatBitrate array
             * Inputs:  Format - Audio format
             *          Bitrate - <bitdepth-samplerate>
             * Outputs: Array flags are set as appropriate
             * Note:    ALLFORMATS includes WAV format (array length - 1)
             *          ALLBITRATES ignores RAW flag (array length - 2)
             * Note:    Invalid inputs will not set any flags in array, as the i,j indexes = -1
             */
            int i, j, k;

            // lookup format and bitrate indexes
            i = Array.IndexOf(AudioFormats, Format);
            j = Array.IndexOf(AudioBitrates, Bitrate);

            // specific audio format
            if (i >= 0)
            {
                // set a specific bitrate
                if (j >= 0)
                    AudioFormatBitrate[i, j] = true;
                // set all bitrates
                else if (Bitrate == ALLBITRATES)
                {
                    for (k = 0; k <= AudioBitrates.Length - 2; k++)
                        AudioFormatBitrate[i, k] = true;
                }
            }
            // all audio formats
            else if (Format == ALLFORMATS)
            {
                for (i = 0; i <= AudioFormats.Length - 1; i++)
                {
                    // set a specific bitrate
                    if (j >= 0)
                        AudioFormatBitrate[i, j] = true;
                    // set all bitrates
                    else if (Bitrate == ALLBITRATES)
                    {
                        for (k = 0; k <= AudioBitrates.Length - 2; k++)
                            AudioFormatBitrate[i, k] = true;
                    }
                }
            }
        } // end SetFormatBitrate

        static bool CheckFormatBitrate(string Format, string Bitrate)
        {
            /* Lookup boolean value in AudioFormatBitrate array
             * Inputs:  Format - Audio format in AudioFormats array
             *          Bitrate - <bitdepth-samplerate> in AudioBitrates array
             * Note:    ALLFORMATS includes WAV format (array length - 1)
             *          ANYFORMAT includes WAV format (array length - 1)
             *          ALLBITRATES ignores RAW flag (array length - 2)
             *          ANYBITRATE ignores RAW flag (array length - 2)
             * Outputs: Boolean value coresponding to input location in array
             */
            int i, j;

            // lookup format and bitrate indexes
            i = Array.IndexOf(AudioFormats, Format);
            j = Array.IndexOf(AudioBitrates, Bitrate);
            
            // specific audio format
            if (i >= 0)
            {
                // specific bitrate
                if (j >= 0)
                    return AudioFormatBitrate[i, j];
                // any bitrate
                else if (Bitrate == ANYBITRATE)
                {
                    for (j = 0; j <= AudioBitrates.Length - 2; j++)
                    {
                        if (AudioFormatBitrate[i, j])
                            return true;
                    }
                    return false;
                }
                // all bitrates
                else if (Bitrate == ALLBITRATES)
                {
                    for (j = 0; j <= AudioBitrates.Length - 2; j++)
                    {
                        if (!AudioFormatBitrate[i, j])
                            return false;
                    }
                    return true;
                }
            }
            // any audio format
            else if (Format == ANYFORMAT)
            {
                // any bitrate
                if (Bitrate == ANYBITRATE)
                {
                    for (i = 0; i <= AudioFormats.Length - 1; i++)
                    {
                        for (j = 0; j <= AudioBitrates.Length - 2; j++)
                        {
                            if (AudioFormatBitrate[i, j])
                                return true;
                        }
                    }
                    return false;
                }
                // specific bitrate
                else if (j >= 0)
                {
                    for (i = 0; i <= AudioFormats.Length - 1; i++)
                    {
                        if (AudioFormatBitrate[i, j])
                            return true;
                    }
                    return false;
                }
            }
            // all formats and bitrates
            else if (Format == ALLFORMATS && Bitrate == ALLBITRATES)
            {
                for (i = 0; i <= AudioFormats.Length - 1; i++)
                {
                    for (j = 0; j <= AudioBitrates.Length - 2; j++)
                    {
                        if (!AudioFormatBitrate[i, j])
                            return false;
                    }
                }
                return true;
            }
            // input values not found
            return false;
        } // end CheckFormatBitrate

        static bool CheckUniqueBitrate(string Format)
        {
            /* Checks bitrate flags set for the input format
             * Inputs:  Format - Audio format in AudioFormats array
             * Note:    ignores RAW flag (array length - 2)
             * Returns: Boolean value
             *          true if one bitrate is set, otherwise false
             */
            int i, j, NumberOfBitratesSet = 0;

            i = Array.IndexOf(AudioFormats, Format);
            for (j = 0; j <= AudioBitrates.Length - 2; j++)
            {
                if (AudioFormatBitrate[i, j])
                    NumberOfBitratesSet += 1;
            }
            if (NumberOfBitratesSet == 1)
                return true;
            else
                return false;
        } //end CheckUniqueBitrate

        static void PrintFormatBitrate()
        {
            /* Print out the AudioFormatBitrate boolean array
             * Note: This is only used for debugging
             */
            int i, j;
            Console.Write("Bitrate");
            for (i = 0; i <= AudioBitrates.Length - 1; i++)
                Console.Write("{0,8}", AudioBitrates[i]);
            Console.WriteLine();
            for (i = 0; i <= AudioFormats.Length - 1; i++)
            {
                Console.Write("{0,4}  |", AudioFormats[i]);
                for (j = 0; j <= AudioBitrates.Length - 1; j++)
                    Console.Write("{0,8}", AudioFormatBitrate[i, j]);
                Console.WriteLine();
            }
        } // end PrintFormatBitrate
    }
}