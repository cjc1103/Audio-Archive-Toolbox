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

            // UseInfotext and UseCuesheet flags are mutually exclusive
            // if UseInfotext flag is set, read metadata from info.txt file
            if (UseInfotext
                && File.Exists(Dir.ParentInfotextPath)
                && Dir.Name != RAW)
                GetTrackMetadataFromInfotext(Dir, FileList);

            // if UseCuesheet flag is set, read metadata from cuesheet
            else if (UseCuesheet
                && File.Exists(Dir.ParentCuesheetPath)
                && Dir.Name != RAW)
                GetTrackMetadataFromCuesheet(Dir, FileList);

            // otherwise derive metadata from file names in FileList
            else
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
            int
                i,
                TrackNumber = 0,
                FileListLineCount,
                InfotextLineCount;
            string[]
                InfotextList;
            string
                InfotextFileName,
                InfotextFilePath,
                DataLine,
                TrackNumberStr,
                TrackTitle,
                TrackFilePath;
            Match
                TrackPatternMatch;

            // check infotext file exists
            if (File.Exists(Dir.ParentInfotextPath))
            {
                // get info file name, discard path
                (InfotextFilePath, InfotextFileName) = SplitPath(Dir.ParentInfotextPath);
                Log.WriteLine("  Reading track metadata from info file: " + InfotextFileName);
                // read infotext file
                InfotextList = ReadTextFile(Dir.ParentInfotextPath);
                InfotextLineCount = InfotextList.Length;
                FileListLineCount = FileList.Length;
                if (Debug) Console.WriteLine("(dbg) FileList Count {0} ", FileListLineCount);
                // read info file line by line, ignoring concert info on first 5 lines)
                for (i = 5; i < InfotextLineCount; i++)
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
                        // remove any trailing spaces
                        TrackTitle = Regex.Replace(TrackTitle, @"\s*$", "");
                        // if TrackTitle is empty, change it to "Track dd"
                        if (TrackTitle == String.Empty)
                            TrackTitle = "Track " + TrackNumberStr;
                        // write extracted track info to log
                        Log.WriteLine("    " + TrackNumberStr + SPACE + TrackTitle);
                        // add extracted track title to Dir TitleList
                        Dir.TitleList.Add(TrackTitle);
                        // add artist name to Dir ArtistList
                        // (note: use cuesheet to specify a different artist for each track)
                        Dir.ArtistList.Add(Dir.AlbumArtist);
                        // find corresponding audio file in FileList and add track duration to Dir TrackDurationList
                        TrackFilePath = FileList[TrackNumber - 1].FullName;
                        Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                    }
                    // exit loop when TrackNumber = FileListCount so remainder of file is not read
                    if (TrackNumber == InfotextLineCount)
                        break;
                }
                // check track number is correct, otherwise revert to directory metadata
                // (Note: track number will be one more than actual number) 
                if (TrackNumber != FileListLineCount)
                {
                    Log.WriteLine("*** Info file tracks: " + Convert.ToString(TrackNumber)
                                + "; Actual number of tracks: " + Convert.ToString(FileListLineCount));
                    GetTrackMetadataFromFileNames(Dir, FileList);
                }
            }
            else
                Log.WriteLine("*** Infotext file not found: " + Dir.ParentInfotextPath);

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
                TrackNumber = 0,
                CueTrackNumber = 0,
                CuesheetLineCount,
                FileListLineCount;
            string[]
                CuesheetList;
            string
                CuesheetFilePath,
                CuesheetFileName,
                DataLine,
                CueTrackNumberStr = null,
                TrackNumberStr,
                TrackTitle = null,
                TrackFilePath,
                Artist = null;
            Match
                PatternMatch;

            // check infotext file exists
            if (File.Exists(Dir.ParentCuesheetPath))
            {
                // get cuesheet filename, discard path
                (CuesheetFilePath, CuesheetFileName) = SplitPath(Dir.ParentCuesheetPath);
                Log.WriteLine("  Reading track metadata from cuesheet: " + CuesheetFileName);
                // read cuesheet
                CuesheetList = ReadTextFile(Dir.ParentCuesheetPath);
                CuesheetLineCount = CuesheetList.Length;
                FileListLineCount = FileList.Length;
                if (Debug) Console.WriteLine("(dbg) FileList Count {0} ", FileListLineCount);
                // initialize title and artist flags
                TitleFound = ArtistFound = false;
                // search cuesheet data for keywords
                for (i = 0; i < CuesheetLineCount; i++)
                {
                    DataLine = CuesheetList[i];
                    // remove any trailing spaces
                    DataLine = Regex.Replace(DataLine, @"\s*$", "");
                    // search for next track
                    PatternMatch = Regex.Match(DataLine, @"TRACK \d{2} AUDIO");
                    if (PatternMatch.Success)
                    {
                        if (TitleFound | ArtistFound)
                            Log.WriteLine("*** Track information is out of order at line no. " + Convert.ToString(i));
                        // extract track number
                        CueTrackNumberStr = DataLine.Substring(PatternMatch.Index + 6, 2);
                        CueTrackNumber = Convert.ToInt32(CueTrackNumberStr);
                        // skip to next line
                        break;
                    }
                    // search for title
                    PatternMatch = Regex.Match(DataLine, @"TITLE ");
                    if (PatternMatch.Success)
                    {
                        if (TitleFound)
                            Log.WriteLine("*** Two successive title entries in cuesheet");
                        TrackTitle = DataLine.Substring(PatternMatch.Index + 6);
                        // remove "dd " or "dd. " track number prefix
                        TrackTitle = Regex.Replace(TrackTitle, @"^\d{2}\.?\s", "");
                        // remove 3 - 5 character extension
                        TrackTitle = Regex.Replace(TrackTitle, @"\.[a-z0-9]{3,5}$", "");
                        // remove prefix and suffix quotes
                        TrackTitle = Regex.Replace(TrackTitle, @"^\""", "");
                        TrackTitle = Regex.Replace(TrackTitle, @"\""$", "");
                        // populate track  and track duration metadata
                        Dir.TitleList.Add(TrackTitle);
                        TitleFound = true;
                    }
                    // search for track artist (performer) - may be different than the album artist
                    PatternMatch = Regex.Match(DataLine, @"PERFORMER ");
                    if (PatternMatch.Success)
                    {
                        if (ArtistFound)
                            Log.WriteLine("*** Two successive performer entries in cuesheet");
                        Artist = DataLine.Substring(PatternMatch.Index + 10);
                        // remove prefix and suffix quotes
                        Artist = Regex.Replace(Artist, @"^\""", "");
                        Artist = Regex.Replace(Artist, @"\""$", "");
                        // populate Artist metadata
                        Dir.ArtistList.Add(Artist);
                        ArtistFound = true;
                    }
                    // both title and artist information for this track have been found
                    if (TitleFound && ArtistFound)
                    {
                        // increment track number
                        TrackNumber++;
                        TrackNumberStr = TrackNumber.ToString("00");
                        // FileList bounds check
                        if (TrackNumber <= FileListLineCount)
                        {
                            // find corresponding audio file and populate track duration
                            TrackFilePath = FileList[TrackNumber - 1].FullName;
                            Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                            // write track info to log
                            Log.WriteLine("    " + CueTrackNumberStr + SPACE + TrackTitle
                                        + " [" + Artist + "]");
                            if (CueTrackNumber != TrackNumber)
                                Log.WriteLine("*** Track number mismatch, track number should be " + TrackNumberStr);
                        }
                        // reset title and artist flags
                        TitleFound = ArtistFound = false;
                    }
                }
                // end of cuesheet - check track number is correct, otherwise revert to directory metadata
                // (Note: track number will be one more than actual number)
                if (TrackNumber != FileListLineCount)
                {
                    Log.WriteLine("*** Cuesheet tracks: " + Convert.ToString(TrackNumber)
                                + "; Actual number of tracks: " + Convert.ToString(FileListLineCount));
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
                // remove "dd " or "dd. " track number prefix
                TrackTitle = Regex.Replace(TrackTitle, @"^\d{2}\.?\s", "");
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
    }
}