using System;

namespace AATB
{
    public partial class AATB_Main
    {

        static void SetQValue(string AudioCompFormat, string qValueStr)
        {
            /* Inputs:
             *   qInputStr - string of command line argument for quality calculation
             *   qBounds - float array of quality values { LOWER, UPPER, ACTIVE }
             * Outputs:
             *   returns quality value within compression format bounds
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
                // index corresponds to location of bounds in CompresssionAudioQuality list
                // update CompresssionAudioQuality list with qValue between LOWER and UPPER bounds
                qValue = Math.Max(qValue, CompressedAudioQuality[index][LOWER]);
                qValue = Math.Min(qValue, CompressedAudioQuality[index][UPPER]);
                CompressedAudioQuality[index][ACTIVE] = qValue;
            }
        } // end SetQValue
    }
}