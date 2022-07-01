using System;
using System.Globalization;

namespace AATB
{
    public partial class AATB_Main
    {
        static string ConvertCase(string InputName)
        {
            string OutputName;
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;

            // title case and lowercase flags are mutually exclusive
            if (UseTitleCase)
                // capitalizes the first letter of each word in InputName
                OutputName = ti.ToTitleCase(InputName);
            else if (UseLowerCase)
                // converts InputName string to lower case
                OutputName = InputName.ToLower();
            else
                // no change
                OutputName = InputName;

            return (OutputName);
        } // end ConvertCase
    }
}