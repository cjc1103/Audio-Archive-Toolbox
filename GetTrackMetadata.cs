using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static void GetTrackMetadata(AATB_DirInfo Dir, FileInfo[] FileList)
        {
            /* Extract track metadata
             * Use info.txt information file if it exists (generally for live concerts)
             * Use cuesheet if it exists (generally for commercial CDs)
             * Otherwise get metadata from file names
             * Inputs:
             *   Dir            Directory as AATB_DirInfo class instance
             *   FileList       List of files in directory
             *   InfoSheetPath  Path to info.txt file
             *   CuesheetPath   Path to .cue file
             * Outputs:
             *   None - called methods will populate metadata
             */

            // clear any entries from Dir lists
            Dir.TitleList.Clear();
            Dir.TrackDurationList.Clear();

            // Determine where to get track metadata
            // Dir.MetadataSource is previously set in GetDirMetadata
            // from infotext source
            if (UseInfotext
                && Dir.MetadataSource == INFOTXT
                && Dir.Name != RAW)
                GetTrackMetadataFromInfotext(Dir, FileList);
            // from cuesheet source
            else if (UseCuesheet
                && Dir.MetadataSource == CUESHEET
                && Dir.Name != RAW)
                GetTrackMetadataFromCuesheet(Dir, FileList);
            // from file names in FileList
            else if (Dir.MetadataSource == DIRNAME)
                GetTrackMetadataFromFileNames(Dir, FileList);

        } // end GetTrackMetadata

        static void GetTrackMetadataFromInfotext(AATB_DirInfo Dir, FileInfo[] FileList)
        {
            /* Get track metadata from "info.txt" information file if it exists
             *   otherwise get metadata from file names
             * Inputs: 
             *   Dir            Current directory class
             *   FileList       List of files in directory
             *   InfoSheetPath  Path to info.txt file
             * Outputs:
             *   Dir            Current directory class
             * Note: the Artist for each track is assumed to be the Album Artist.
             */
            bool
                ArtistFound = false;
            int
                i,
                TrackNumber = 0,
                FileListCount;
            string[]
                InfotextList;
            string
                InfotextFileName,
                DataLine,
                TrackNumberStr,
                TrackTitle,
                TrackFilePath,
                TrackArtist;
            Match
                TrackPatternMatch,
                ArtistPatternMatch;

            // check infotext file exists
            if (File.Exists(Dir.ParentInfotextPath))
            {
                // get info file name
                InfotextFileName = SplitFileName(Dir.ParentInfotextPath);
                Log.WriteLine("  Reading track metadata from info file: " + InfotextFileName);
                // read infotext file
                InfotextList = ReadTextFile(Dir.ParentInfotextPath);
                FileListCount = FileList.Length;

                // read info file, ignoring concert info on first 5 lines)
                for (i = 5; i < InfotextList.Length; i++)
                {
                    DataLine = InfotextList[i];
                    // track format is: "{dd} title", "{dd}. title", where dd is one or two digits 0-9
                    // each track is assumed to be in sequence, information is added to next position in list
                    // track numbers in info file can restart for multiple sets/discs, and tracks may not
                    // be on contiguous lines, so "TrackNumber" variable counts tracks instead
                    // limitations: max track number is 99

                    // check for track number prefix
                    // one or two digits at beginning of line, optional period and space
                    TrackPatternMatch = Regex.Match(DataLine, @"^\d{1,2}\.?\s*");
                    if (TrackPatternMatch.Success)
                    {
                        // increment track number and convert to two place string
                        TrackNumber++;
                        TrackNumberStr = TrackNumber.ToString("00");
                        // extract track title from each line
                        TrackTitle = DataLine;
                        // remove track number prefix with optional period and spaces (allows for blank title)
                        TrackTitle = Regex.Replace(TrackTitle, @"^\d{1,2}\.?\s*", "");
                        // remove comments starting with a left bracket and ending with a right bracket
                        TrackTitle = Regex.Replace(TrackTitle, @"\(.*\)", "");
                        // search for "[..]" sequence with artist info
                        ArtistPatternMatch = Regex.Match(DataLine, @"\[.*\]$");
                        if (ArtistPatternMatch.Success)
                        {
                            // extract artist name from within brackets
                            ArtistFound = true;
                            TrackArtist = TrackTitle.Substring(ArtistPatternMatch.Index - 2);
                            TrackArtist = Regex.Replace(TrackArtist, @"\[", "");
                            TrackArtist = Regex.Replace(TrackArtist, @"\]\s*$", "");
                            // remove artist name from track title
                            TrackTitle = Regex.Replace(TrackTitle, @"\[.*\]", "");
                        }
                        else
                        {
                            // use album artist as track artist
                            TrackArtist = Dir.AlbumArtist;
                        }
                        // add track artist to Dir ArtistList
                        Dir.ArtistList.Add(TrackArtist);
                        // remove any trailing spaces
                        TrackTitle = Regex.Replace(TrackTitle, @"\s*$", "");
                        // if TrackTitle is empty, change it to "Track dd"
                        if (TrackTitle == String.Empty)
                            TrackTitle = "Track " + TrackNumberStr;
                        // add track title to Dir TitleList
                        Dir.TitleList.Add(TrackTitle);
                        // find corresponding audio file in FileList and add track duration to Dir TrackDurationList
                        TrackFilePath = FileList[TrackNumber - 1].FullName;
                        Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));

                        // write extracted track and artist info to log
                        Log.Write("    " + TrackNumberStr + SPACE + TrackTitle);
                        if (ArtistFound) Log.Write(" [" + TrackArtist + "]");
                        Log.WriteLine();
                        ArtistFound = false;
                    }
                    // exit loop when TrackNumber = FileListLineCount so remainder of file is not read
                    if (TrackNumber == FileListCount)
                        break;
                }
                // check track number is correct, otherwise revert to directory metadata
                // (Note: track number will be one more than actual number) 
                if (TrackNumber != FileListCount)
                {
                    Log.WriteLine("*** Info file tracks: " + Convert.ToString(TrackNumber)
                                + "; Actual number of tracks: " + Convert.ToString(FileListCount));
                    GetTrackMetadataFromFileNames(Dir, FileList);
                }
            }
            else
                Log.WriteLine("*** Infotext file not found");

        }  // end GetTrackMetadataFromInfotext

        static void GetTrackMetadataFromCuesheet(AATB_DirInfo Dir, FileInfo[] FileList)
        {
            /* Get track metadata from cuesheet *.cue if it exists
             *   otherwise get metadata from file names
             * Inputs: 
             *   Dir            Current directory class
             *   FileList       List of files in directory
             *   CuesheetPath   Path to .cue file
             * Outputs:
             *   Dir            Current directory class
             */
            bool
                TitleFound,
                ArtistFound;
            int
                i,
                CueTrackNumber,
                FileListCount;
            string[]
                CuesheetList;
            string
                CuesheetFileName,
                DataLine,
                CueTrackNumberStr = null,
                TrackTitle = null,
                TrackFilePath,
                TrackArtist = null;
            Match
                PatternMatch;

            // check infotext file exists
            if (File.Exists(Dir.ParentCuesheetPath))
            {
                // get cuesheet filename
                CuesheetFileName = SplitFileName(Dir.ParentCuesheetPath);
                Log.WriteLine("  Reading track metadata from cuesheet: " + CuesheetFileName);
                // read cuesheet
                CuesheetList = ReadTextFile(Dir.ParentCuesheetPath);
                FileListCount = FileList.Length;
                // initialize title and artist flags
                TitleFound = ArtistFound = false;
                CueTrackNumberStr = String.Empty;
                CueTrackNumber = 0;

                // read cuesheet, search for keywords
                for (i = 0; i < CuesheetList.Length; i++)
                {
                    DataLine = CuesheetList[i];
                    // remove leading spaces
                    DataLine = Regex.Replace(DataLine, @"^\s*", "");
                    if (Debug) Console.WriteLine("dbg: Line {0} Data: {1}", i, DataLine);

                    // search for next track
                    PatternMatch = Regex.Match(DataLine, @"^TRACK \d{2} AUDIO");
                    if (PatternMatch.Success)
                    {
                        // write data from previous line number
                        // Don't write data for first track number, FileList bounds check
                        if (CueTrackNumber > 0 && CueTrackNumber <= FileListCount)
                        {
                            // find corresponding audio file and populate track duration
                            // cue track numbers start at 01, subtract 1 to get correct index
                            TrackFilePath = FileList[CueTrackNumber - 1].FullName;
                            Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                            Dir.TitleList.Add(TrackTitle);
                            Dir.ArtistList.Add(TrackArtist);
                            // write track info to log
                            Log.WriteLine("    " + CueTrackNumberStr + SPACE + TrackTitle
                                        + " [" + TrackArtist + "]");

                            // write extracted track and artist info to log
                            Log.Write("    " + CueTrackNumberStr + SPACE + TrackTitle);
                            if (ArtistFound) Log.Write(" [" + TrackArtist + "]");
                            Log.WriteLine();
                        }

                        // extract track number for next track
                        CueTrackNumberStr = DataLine.Substring(6, 2);
                        CueTrackNumber = Convert.ToInt32(CueTrackNumberStr);

                        // reset title and artist flags
                        TitleFound = ArtistFound = false;
                    }
                    // search for title
                    PatternMatch = Regex.Match(DataLine, @"^TITLE");
                    if (PatternMatch.Success)
                    {
                        if (TitleFound)
                            Log.WriteLine("*** Two successive title entries in cuesheet");
                        else
                            TitleFound = true;
                        if (DataLine.Length > 5)
                            TrackTitle = CleanData(DataLine.Substring(5));
                        else
                            TrackTitle = String.Empty;
                    }
                    // search for track artist (performer) - this may be different than the album artist
                    PatternMatch = Regex.Match(DataLine, @"^PERFORMER");
                    if (PatternMatch.Success)
                    {
                        if (ArtistFound)
                            Log.WriteLine("*** Two successive performer entries in cuesheet");
                        else
                            ArtistFound = true;
                        if (DataLine.Length > 9)
                            TrackArtist = CleanData(DataLine.Substring(9));
                        else
                            TrackArtist = String.Empty;
                    }

                } // end cuesheet read loop

                // write last track data
                if (CueTrackNumber > 0
                    && CueTrackNumber <= FileListCount)
                {
                    // find corresponding audio file and populate track duration
                    TrackFilePath = FileList[CueTrackNumber - 1].FullName;
                    Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                    Dir.TitleList.Add(TrackTitle);
                    Dir.ArtistList.Add(TrackArtist);
                    // write track info to log
                    Log.WriteLine("    " + CueTrackNumberStr + SPACE + TrackTitle
                                + " [" + TrackArtist + "]");
                }

                // check track number is correct, otherwise revert to directory metadata
                // (Note: track number will be one more than actual number)

                if (CueTrackNumber != FileListCount)
                {
                    Log.WriteLine("*** Cuesheet tracks: " + CueTrackNumberStr
                                + "; Actual number of tracks: " + Convert.ToString(FileListCount));
                    GetTrackMetadataFromFileNames(Dir, FileList);
                }
            }
            else
                Log.WriteLine("*** Cuesheet not found: " + Dir.ParentCuesheetPath);

        }  // end GetTrackMetadataFromCuesheet

        static void GetTrackMetadataFromFileNames(AATB_DirInfo Dir, FileInfo[] FileList)
        {
            /* Extract concert metadata from file names
             * Inputs: 
             *   Dir        Current directory class
             *   FileList   List of files in directory
             * Outputs:
             *   Dir        Current directory class
             */
            int i;
            string
                TrackTitle,
                TrackFilePath;

            Log.WriteLine("  Deriving track metadata from file names");
            // build title for each track from basename, ignore prefix, extension
            // artist name will be the same for each track
            for (i = 0; i < FileList.Length; i++)
            {
                // get filename
                TrackTitle = FileList[i].Name;
                // remove track number prefix
                // (one or more digits, optional period, one or more spaces)
                TrackTitle = Regex.Replace(TrackTitle, @"^\d+\.?\s+", "");
                // remove 3 - 5 letter extension
                TrackTitle = Regex.Replace(TrackTitle, @"\.[a-z0-9]{3,5}$", "");
                // populate track metadata
                Dir.TitleList.Add(TrackTitle);
                TrackFilePath = FileList[i].FullName;
                Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                Dir.ArtistList.Add(Dir.AlbumArtist);
                Log.WriteLine("    " + (i + 1).ToString("00") + SPACE + TrackTitle);
            }
        }  // end GetTrackMetadataFromFileNames

        static string CleanData(string Data)
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
        }
    }
}