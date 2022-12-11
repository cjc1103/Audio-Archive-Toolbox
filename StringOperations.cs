using System;
using System.Text.RegularExpressions;

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
             *   Returns Filename - characters after last backslash
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
             *   Returns tuple (Path, FileName) where
             *     Path - characters before last delimiter
             *     FileName - characters after last delimiter
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
             *   Returns tuple (Prefix, Suffix) where
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
             *   Returns CurrentDirPath - current subdirectory path below root directory
             */
            string Data;

            // root dir path will always start at char=0, add one
            if (CurrentDirPath.Length > RootDirPath.Length)
                Data = CurrentDirPath.Substring(RootDirPath.Length + 1);
            else
                Data = CurrentDirPath;
            return Data;
        } // end SplitDirPath


        static string[] SplitDataByLine(string Data)
        {
            /* Splits a text string into lines separated by dos and unix delimeters
             * Input:
             *   Data       Text string
             * Constant:
             *   LineDelimeters is a static string array defined in Program.cs
             * Output:
             *   Returns string array, with one line to each element of array, empty lines removed
             */
            string[] DataList = Data.Split(LineDelimeters, StringSplitOptions.RemoveEmptyEntries);
            return DataList;
        } // end SplitDataByLine

        static string SearchList(string[] DataList, string SearchTerm)
        {
            /* Inputs:
             *   DataList   list containing data
             *   Name       string search term
             * Outputs:
             *   Returns string found by pattern match, null if not found
             */
            int i;
            string
                Data = null;
            Match
                PatternMatch;

            for (i = 0; i < DataList.Length; i++)
            {
                // search for pattern in string
                PatternMatch = Regex.Match(DataList[i], @SearchTerm);
                if ((PatternMatch.Success)
                    && (DataList[i].Length > SearchTerm.Length))
                {
                    // extract matching data substring from Datalist line
                    Data = DataList[i].Substring(PatternMatch.Index + SearchTerm.Length);
                    // remove extraneous characters
                    Data = CleanDataString(Data);
                    // exit loop, only first match in list will be used
                    break;
                }
            }
            return Data;
        } // end SearchList

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