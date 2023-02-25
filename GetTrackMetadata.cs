using System;
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
            // get metadata from infotext
            if (UseInfotext
                && Dir.MetadataSource == INFOFILE)
                GetTrackMetadataFromInfotext(Dir, FileList);
            
            // get metadata from cuesheet
            else if (UseCuesheet
                && Dir.MetadataSource == CUESHEET)
                GetTrackMetadataFromCuesheet(Dir, FileList);
            
            // if infotext or cuesheet data is not valid, source reverts to DIRNAME
            // get metadata from file names in FileList
            if (Dir.MetadataSource == DIRNAME)
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
             * Infotext file
             *   Track format is: "{dd}<.> title [Artist]", where dd is one or two digits 0-9
             *   Each track is assumed to be in sequence, information is added to next position in list
             *   Track numbers in info file can restart for multiple sets/discs, and tracks may not
             *   be on contiguous lines, so "TrackNumber" variable counts tracks instead
             * Notes:
             *   (1) Max track number is 99
             *   (2) Comments in brackets ( ) are ignored (typically songwriter name)
             *   (3) Comments in square brackets [ ] assumed to be artist name (overrides "Album Artist")
             */
            bool
                ArtistFound = false;
            int
                i,
                TrackNumber = 0,
                FileListCount;
            string[]
                DataList;
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
            if (File.Exists(Dir.InfotextPath))
            {
                // get info file name
                InfotextFileName = SplitFileName(Dir.InfotextPath);
                Log.WriteLine("  Reading track metadata from info file: " + InfotextFileName);
                // read infotext file
                DataList = ReadTextFile(Dir.InfotextPath);
                FileListCount = FileList.Length;

                // read info file, ignoring concert info on first 5 lines)
                for (i = 5; i < DataList.Length; i++)
                {
                    DataLine = DataList[i];
                    if (Debug) Console.WriteLine("dbg: {0:D2} Data: {1}", i, DataLine);
                    
                    // check for track number prefix
                    // match one or two digits at beginning of line, optional period and space
                    TrackPatternMatch = Regex.Match(DataLine, @"^\d{1,2}\.?\s*");
                    if (TrackPatternMatch.Success)
                    {
                        // increment track number and convert to two place string
                        TrackNumber++;
                        TrackNumberStr = TrackNumber.ToString("00");
                        
                        // remove track number prefix with optional period and spaces (allows for blank title)
                        DataLine = Regex.Replace(DataLine, @"^\d{1,2}\.?\s*", "");

                        // remove comments enclosed in brackets
                        DataLine = Regex.Replace(DataLine, @"\(.*\)", "");

                        // search for artist name within square brackets 
                        ArtistPatternMatch = Regex.Match(DataLine, @"\[.*\]$");
                        if (ArtistPatternMatch.Success)
                        {
                            // extract artist name from within brackets
                            ArtistFound = true;
                            TrackArtist = DataLine.Substring(ArtistPatternMatch.Index);
                            TrackArtist = Regex.Replace(TrackArtist, @"\[", "");
                            TrackArtist = Regex.Replace(TrackArtist, @"\]\s*$", "");
                            // remove artist name from data line
                            DataLine = Regex.Replace(DataLine, @"\[.*\]", "");
                        }
                        else
                        {
                            // use album artist as track artist
                            TrackArtist = Dir.AlbumArtist;
                        }
                        // add track artist to Dir ArtistList
                        Dir.ArtistList.Add(TrackArtist);

                        // keep alphanumeric, special characters (escaped with \), spaces
                        DataLine = Regex.Replace(DataLine, @"[^a-zA-Z0-9\.\,\!\?\-\>\'\s]", "");

                        // remove any trailing spaces
                        DataLine = Regex.Replace(DataLine, @"\s*$", "");

                        // TrackTitle is remainder of data line
                        TrackTitle = DataLine;

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
                        if (ArtistFound && TrackArtist != Dir.AlbumArtist)
                            Log.Write(" [" + TrackArtist + "]");
                        Log.WriteLine();

                        // reset ArtistFound flag
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
                    Dir.MetadataSource = DIRNAME;
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
                TrackNumber,
                FileListCount;
            string[]
                DataList;
            string
                CuesheetFileName,
                DataLine,
                TrackNumberStr = null,
                TrackTitle = null,
                TrackFilePath,
                TrackArtist = null;
            Match
                PatternMatch;

            // check infotext file exists
            if (File.Exists(Dir.CuesheetPath))
            {
                // get cuesheet filename
                CuesheetFileName = SplitFileName(Dir.CuesheetPath);
                Log.WriteLine("  Reading track metadata from cuesheet: " + CuesheetFileName);
                // read cuesheet
                DataList = ReadTextFile(Dir.CuesheetPath);
                FileListCount = FileList.Length;
                // initialize title and artist flags
                TitleFound = ArtistFound = false;
                TrackNumberStr = String.Empty;
                TrackNumber = 0;

                // read cuesheet, search for keywords
                for (i = 0; i < DataList.Length; i++)
                {
                    DataLine = DataList[i];
                    if (Debug) Console.WriteLine("dbg: {0:D2} Data: {1}", i, DataLine);

                    // remove leading spaces
                    DataLine = Regex.Replace(DataLine, @"^\s*", "");

                    // search for next track
                    PatternMatch = Regex.Match(DataLine, @"^TRACK \d{2} AUDIO");
                    if (PatternMatch.Success)
                    {
                        // populate previous track data
                        // track number bounds check, don't write data for first track number
                        if (TrackNumber > 0 && TrackNumber <= FileListCount)
                        {
                            // find corresponding audio file and populate track duration
                             TrackFilePath = FileList[TrackNumber - 1].FullName;
                            Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                            Dir.TitleList.Add(TrackTitle);
                            Dir.ArtistList.Add(TrackArtist);

                            // write extracted track and artist info to log
                            Log.Write("    " + TrackNumberStr + SPACE + TrackTitle);
                            if (ArtistFound && TrackArtist != Dir.AlbumArtist)
                                Log.Write(" [" + TrackArtist + "]");
                            Log.WriteLine();
                        }

                        // extract track number for next track
                        TrackNumberStr = DataLine.Substring(6, 2);
                        TrackNumber = Convert.ToInt32(TrackNumberStr);

                        // reset artist and title flags
                        ArtistFound = TitleFound = false;
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
                            TrackArtist = CleanDataString(DataLine.Substring(9));
                        else
                            TrackArtist = String.Empty;
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
                            TrackTitle = CleanDataString(DataLine.Substring(5));
                        else
                            TrackTitle = String.Empty;
                    }

                } // end cuesheet read loop

                // populate last track data
                // track number bounds check, don't write data for first track number
                if (TrackNumber > 0  && TrackNumber <= FileListCount)
                {
                    // find corresponding audio file and populate track duration
                    TrackFilePath = FileList[TrackNumber - 1].FullName;
                    Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                    Dir.TitleList.Add(TrackTitle);
                    Dir.ArtistList.Add(TrackArtist);

                    // write extracted track and artist info to log
                    Log.Write("    " + TrackNumberStr + SPACE + TrackTitle);
                    if (ArtistFound && TrackArtist != Dir.AlbumArtist)
                        Log.Write(" [" + TrackArtist + "]");
                    Log.WriteLine();
                }

                // check track number is correct, otherwise revert to directory metadata
                // (Note: track number will be one more than actual number)
                if (TrackNumber != FileListCount)
                {
                    Log.WriteLine("*** Cuesheet tracks: " + TrackNumberStr
                                + "; Actual number of tracks: " + Convert.ToString(FileListCount));
                    Dir.MetadataSource = DIRNAME;
                }
            }
            else
                Log.WriteLine("*** Cuesheet not found: " + Dir.CuesheetPath);

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
    }
}