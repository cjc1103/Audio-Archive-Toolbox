using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static string SplitFileName(string InputPath)
        {
            /* Separates filename from the input filepath
             * Inputs:
             *   Directory or file path
             * Output:
             *   FileName - characters after last backslash (\)
             */
            int
                index;
            string
                FileName = null;

            if (InputPath != null)
            {
                index = InputPath.LastIndexOf(BACKSLASH);
                if (index > 0)
                    FileName = InputPath.Substring(index + 1);
                else
                    FileName = InputPath;
            }
            return FileName;
        } // end SplitFileName

        static (string, string) SplitFilePathName(string InputPath)
        {
            /* Separates input path into two strings separated by the last occurrence
             *   of the delimiter BACKSLASH
             * Inputs:
             *   Directory or file path
             * Outputs:
             *   Tuple (Path, FileName) where
             *     Path - characters before last delimiter
             *     FileName - characters after last delimter
             */
            int
                index;
            string
                Path = null,
                FileName = null;

            if (InputPath != null)
            {
                index = InputPath.LastIndexOf(BACKSLASH);
                if (index > 0)
                {
                    Path = InputPath.Substring(0, index);
                    FileName = InputPath.Substring(index + 1);
                }
                else
                {
                    Path = null;
                    FileName = InputPath;
                }
            }
            return (Path, FileName);
        } // end SplitFilePathName

        static (string, string) SplitString(string InputName, string Delimiter)
        {
            /* Separates input name into two strings separated by the last occurrence
             *   of the input delimiter
             * Inputs:
             *   Directory or file name (e.g., filename.extension)
             *   Delimiter: character used to delimit name and extension
             * Outputs:
             *   Tuple (Prefix, Suffix) where
             *     Prefix - characters before last delimiter
             *     Suffix - characters after last delimiter
             */
            int 
                index;
            string
                Prefix = null,
                Suffix = null;

            if (InputName != null)
            {
                index = InputName.LastIndexOf(Delimiter);
                if (index > 0)
                {
                    Prefix = InputName.Substring(0, index);
                    Suffix = InputName.Substring(index + 1);
                }
                else
                {
                    Prefix = InputName;
                    Suffix = null;
                }
            }
            return (Prefix, Suffix);
        } // end SplitString
    }
}