using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace nsAATB
{
    public partial class clMain
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

        static (string, string) SplitFilePath(string InputPath)
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
        } // end SplitFilePath

        static (string, string) SplitString(string Delimiter, string InputName)
        {
            /* Separates input name into two strings separated by the last occurrence
             *   of the input delimiter
             * Inputs:
             *   InputName: Directory or file name (e.g., filename.extension)
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

        static string SplitDirPath(string RootDirPath, string CurrentDirPath)
        {
            /* Separates subdirectory path from current path
             * Note: Assumes root path is contained in current path
             * Inputs:
             *   RootDirPath     root directory path
             *   CurrentDirPath  current directory path (subdirectory path from root)
             * Outputs:
             *   Data            current subdirectory path below root directory
             */
            string Data;

            // root dir path will always start at char=0, add one
            if (CurrentDirPath.Length > RootDirPath.Length)
                Data = CurrentDirPath.Substring(RootDirPath.Length + 1);
            else
                Data = CurrentDirPath;
            return Data;
        } // end SplitDirPath

        static bool BaseNamesAreEqual(string Filename1, string Filename2)
        {
            /* Checks if input filenames are equal
             * Inputs:
             *   File name 1, File name 2
             * Outputs:
             *   Boolean value representing equivalency
             */
            string
                BaseName1,
                BaseName2,
                Extension1,
                Extension2;

            // extract basename from each input string
            (BaseName1, Extension1) = SplitString(PERIOD, Filename1);
            (BaseName2, Extension2) = SplitString(PERIOD, Filename2);

            // compare both basenames and return true if they are equal
            return (BaseName1 == BaseName2);
        } // end BaseNamesAreEqual

        static string GetDataAfterSearchTerm(string SearchTerm, string[] DataList)
        {
            /* Returns data after the first instance of the search term in list
             * Search term is considered case insensitive
             * Inputs:
             *   DataList   list containing data
             *   Name       string containing Regex search term
             * Outputs:
             *   Data       string after pattern match, null if not found
             */
            int i;
            string Data = null;
            Match PatternMatch;

            for (i = 0; i < DataList.Length; i++)
            {
                // search for pattern in string
                PatternMatch = Regex.Match(DataList[i], @SearchTerm, RegexOptions.IgnoreCase);
                // check that there are characters in data string after search term
                if (PatternMatch.Success
                    && (DataList[i].Length > SearchTerm.Length))
                {
                    // get index of data following SearchName
                    // add 1 to index to allow for a following space
                    Data = DataList[i].Substring(PatternMatch.Index + SearchTerm.Length + 1);
                    // remove extraneous characters
                    Data = CleanDataString(Data);
                    // exit loop, only first match in list will be used
                    break;
                }
            }
            return Data;
        } // end GetDataAfterSearchTerm

        static int GetLineNumberOfSearchTerm(int StartIndex, string SearchTerm, string[] DataList)
        {
            /* Returns the line number of the first instance of the search term in list
             * Inputs:
             *   StartIndex   Start index in datalist to search
             *   SearchTerm   Regular expression
             *   DataList     list containing data
 
             * Outputs:
             *   line number of search term found by pattern match, -1 if not found
             */
            int i;
            Match PatternMatch;

            for (i = StartIndex; i < DataList.Length; i++)
            {
                // search for pattern in string, case insensitive
                PatternMatch = Regex.Match(DataList[i], @SearchTerm, RegexOptions.IgnoreCase);
                if (PatternMatch.Success)
                    return i;
            }
            // search term not found
            return -1;
        } // end GetLineNumberOfSearchTerm

        static void PrintFileList(string FileType, FileInfo[] FileList)
        {
            /* print contents of FileList for debugging purposes
             */
            Console.WriteLine("dbg: File dump type: {0}", FileType);
            for (int i = 0; i < FileList.Length; i++)
                Console.WriteLine("dbg: FileList Name {0}", FileList[i].Name);
        } //end PrintFileList

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

            return OutputName;
        } // end ConvertCase

        static bool ValidateDate(string InputDate)
        {
            // DateFormat is a global match string
            if (!string.IsNullOrEmpty(InputDate))
                return Regex.Match(InputDate, @DateFormat).Success;
            else
                return false;
        }

        static string CleanDataString(string Data)
        {
            // remove leading spaces
            Data = Regex.Replace(Data, @"^\s*", "");
            // remove any trailing spaces
            Data = Regex.Replace(Data, @"\s*$", "");
            // remove prefix quotes
            Data = Regex.Replace(Data, @"^\""", "");
            // remove suffix quotes
            Data = Regex.Replace(Data, @"\""$", "");
            return Data;
        } // end CleanDataString
    }
}