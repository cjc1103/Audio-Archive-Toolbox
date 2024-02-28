using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void SetQValue(string AudioCompFormat, string qValueStr)
        {
            /* Inputs:
             *   AudioCompFormat - Compression format, i.e., flac
             *   qValueStr - string of command line argument for quality calculation
             * Outputs:
             *   sets quality value within compression format bounds
             *   
             * AudioCompressionQuality - global two dimension list defined in Program
             * [LOWER, ACTIVE, UPPER] - global constants defined in Program
             */
            int qValue;

            if (qValueStr == null)
                qValue = 0;
            else
                qValue = Convert.ToInt32(qValueStr);
            // search for AudioCompFormat in AudioFormats list
            int index = Array.IndexOf(AudioFormats, AudioCompFormat);
            if (index >= 0)
            {
                // index corresponds to audio compression format
                // and is also the location of q bounds for that format in CompresssionAudioQuality list
                // verify qValue is between LOWER and UPPER bounds
                qValue = Math.Max(qValue, AudioCompressionQuality[index][LOWER]);
                qValue = Math.Min(qValue, AudioCompressionQuality[index][UPPER]);
                // set CompresssionAudioQuality array index to qValue
                // The active qValue is retrieved when printing out program parameters
                AudioCompressionQuality[index][ACTIVE] = qValue;
            }
        } // end SetQValue
    }
}