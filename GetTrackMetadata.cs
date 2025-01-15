using System;
using System.Text.RegularExpressions;

namespace nsAATB
{
    public partial class clMain
    {
        static void GetTrackMetadata(clDirInfo Dir, FileInfo[] FileList)
        {
            /* Extract track metadata
             * Use infotext information file if it exists (generally for live concerts)
             * Use cuesheet if it exists (generally for commercial CDs)
             * Otherwise get metadata from file names
             * Inputs:
             *   Dir            Directory as clDirInfo class instance
             *   FileList       List of files in directory
             *   InfoSheetPath  Path to infotext file
             *   CuesheetPath   Path to .cue file
             * Outputs:
             *   None - called methods will populate metadata
             */

            // clear any entries from Dir lists
            Dir.TitleList.Clear();
            Dir.TrackDurationList.Clear();

            // Get track metadata
            // Initialize track metadata location to directory name
            Dir.TrackMetadataSource = DIRNAME;
            
            // read track metadata from infotext file, if it exists
            if (UseInfotext && File.Exists(Dir.InfotextPath))
                GetTrackMetadataFromInfotext(Dir, FileList);

            // read track metadata from cuesheet, if it exists
            else if (UseCuesheet && File.Exists(Dir.CuesheetPath))
                GetTrackMetadataFromCuesheet(Dir, FileList);
            
            // if infotext or cuesheet data is not valid, source reverts to DIRNAME
            // get track metadata from file names in FileList
            if (Dir.TrackMetadataSource == DIRNAME)
                GetTrackMetadataFromFileNames(Dir, FileList);

            // update track duration list
            PopulateTrackDurationList(Dir, FileList);

        } // end GetTrackMetadata

        static void GetTrackMetadataFromInfotext(clDirInfo Dir, FileInfo[] FileList)
        {
            /* Get track metadata from infotext file
             * Inputs: 
             *   Dir            Current directory class
             *   FileList       List of files in directory
             *   InfoSheetPath  Path to infotext file *must exist*
             * Outputs:
             *   Dir            Current directory class
             * Infotext file
             *   Set track list optionally starts after keyword "Set"
             *   Track format is: "{dd}<.> title [Artist]", where dd is one or two digits 0-9
             *   Each track is assumed to be in sequence, information is added to next position in list
             *   Track numbers in infotext can restart for multiple sets/discs, and tracks may not
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
                TrackNumber,
                StartLineNumber,
                EndLineNumber;
            string[]
                DataList;
            string
                InfotextFileName,
                DataLine,
                TrackNumberStr,
                TrackTitle,
                TrackArtist;
            Match
                TrackPatternMatch,
                ArtistPatternMatch;

            // get infotext filename
            InfotextFileName = SplitFileName(Dir.InfotextPath);
            Log.WriteLine("  Reading track metadata from infotext: " + InfotextFileName);
            // read infotext file
            DataList = ReadTextFile(Dir.InfotextPath);
            // initialize counters
            TrackNumber = 0;
            // get start line number - search for the first instance of keyword
            // if not found (-1) set start line number to 6 to skip header information
            StartLineNumber = GetLineNumberOfSearchTerm(0, "^(Set|Track|Disc)", DataList);
            if (StartLineNumber == -1) StartLineNumber = 6;
            // get end line number - search for "End" at beginning of line
            // if not found (-1) set end line number to length of list
            EndLineNumber = GetLineNumberOfSearchTerm(StartLineNumber, "^End", DataList);
            if (EndLineNumber == -1) EndLineNumber = DataList.Length;
            if (Debug) Console.WriteLine("dbg: Setlist line numbers start: {0:D2}  end: {1:D2}",
                                        StartLineNumber, EndLineNumber);
            // read data from infotext - zero based index, stop before eof
            for (i = StartLineNumber; i < EndLineNumber; i++)
            {
                DataLine = DataList[i];
                if (Debug) Console.WriteLine("dbg: Line: {0:D2} Data: {1}", i, DataLine);

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
                    ArtistPatternMatch = Regex.Match(DataLine, @"\[.*\]");
                    if (ArtistPatternMatch.Success)
                    {
                        // extract artist name from within brackets
                        ArtistFound = true;
                        TrackArtist = ArtistPatternMatch.Value;
                        // remove leading bracket
                        TrackArtist = Regex.Replace(TrackArtist, @"\[", "");
                        // remove trailing bracket and any following characters
                        TrackArtist = Regex.Replace(TrackArtist, @"\].*$", "");
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

                    // remove any trailing characters other than alphabetical, numerical,
                    // and single apostrophe. typically these are footnote annotation marks
                    DataLine = Regex.Replace(DataLine, @"[^a-zA-Z0-9\-\'>]*$", "");
                    if (Debug) Console.WriteLine("dbg: Data length: " + DataLine.Length
                                                        + " " + DataLine + "<<");
                    // TrackTitle is remainder of data line
                    TrackTitle = DataLine;
                    // if TrackTitle is empty, change it to "Track dd"
                    if (TrackTitle == String.Empty)
                        TrackTitle = "Track " + TrackNumberStr;
                    // add track title to Dir TitleList
                    Dir.TitleList.Add(TrackTitle);

                    // write extracted track and artist info to log
                    Log.Write("    " + TrackNumberStr + SPACE + TrackTitle);
                    if (ArtistFound && TrackArtist != Dir.AlbumArtist)
                        Log.Write(" [" + TrackArtist + "]");
                    Log.WriteLine();

                    // reset ArtistFound flag
                    ArtistFound = false;
                }
            }
            // check ending track number in infotext file is correct
            // (the track number will be one more than actual number)
            if (TrackNumber == FileList.Length)
            {
                // reset track metadata source flag
                Dir.TrackMetadataSource = INFOFILE;
            }
            else
            {
                Log.WriteLine("*** Info file tracks: " + Convert.ToString(TrackNumber)
                                + "; Actual number of tracks: " + Convert.ToString(FileList.Length));
                // reset title list
                // title list will be repopulated using directory names
                Dir.TitleList = new List<string>();
            }
        }  // end GetTrackMetadataFromInfotext

        static void GetTrackMetadataFromCuesheet(clDirInfo Dir, FileInfo[] FileList)
        {
            /* Get track metadata from cuesheet file
             * Inputs: 
             *   Dir            Current directory class
             *   FileList       List of files in directory
             *   CuesheetPath   Path to .cue file *must exist*
             * Outputs:
             *   Dir            Current directory class
             * Cuesheet file
             *   Track information format:
             *     TRACK 01 AUDIO
             *       TITLE "Shallow Grave"
             *       PERFORMER "Herman Munster"
             *       INDEX 01 00:00:00
             *     TRACK 02 AUDIO
             *       TITLE "Wait For Me"
             *       PERFORMER "Cruella de Ville"
             *       INDEX 01 04:32:67  
             */
            bool
                TitleFound,
                ArtistFound;
            int
                i,
                TrackNumber,
                DataListCount,
                FileListCount;
            string[]
                DataList;
            string
                CuesheetFileName,
                DataLine,
                TrackNumberStr = null,
                TrackTitle = null,
                TrackArtist = null;
            Match
                PatternMatch;

            // get cuesheet filename
            CuesheetFileName = SplitFileName(Dir.CuesheetPath);
            Log.WriteLine("  Reading track metadata from cuesheet: " + CuesheetFileName);
            // read cuesheet
            DataList = ReadTextFile(Dir.CuesheetPath);
            // initialize flags and counters
            TitleFound = ArtistFound = false;
            DataListCount = DataList.Length;
            FileListCount = FileList.Length;
            TrackNumber = 0;

            // read data from cuesheet file
            for (i = 0; i < DataListCount; i++)
            {
                DataLine = DataList[i];
                if (Debug) Console.WriteLine("dbg: Line: {0:D2} Data: {1}", i, DataLine);

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
            if (TrackNumber > 0 && TrackNumber <= FileListCount)
            {
                Dir.TitleList.Add(TrackTitle);
                Dir.ArtistList.Add(TrackArtist);
                // write extracted track and artist info to log
                Log.Write("    " + TrackNumberStr + SPACE + TrackTitle);
                if (ArtistFound && TrackArtist != Dir.AlbumArtist)
                    Log.Write(" [" + TrackArtist + "]");
                Log.WriteLine();
            }
            // check ending track number in infotext file is correct
            // before reading filelist data
            // (the track number will be one more than actual number)
            if (TrackNumber == FileListCount)
            {
                // reset track metadata source flag
                Dir.TrackMetadataSource = CUESHEET;
            }
            else
            {
                Log.WriteLine("*** Cuesheet tracks: " + TrackNumberStr
                            + "; Actual number of tracks: " + Convert.ToString(FileListCount));
                // reset title list
                // title list will be repopulated using directory names
                Dir.TitleList = new List<string>();
            }
        }  // end GetTrackMetadataFromCuesheet

        static void GetTrackMetadataFromFileNames(clDirInfo Dir, FileInfo[] FileList)
        {
            /* Extract concert metadata from file names
             * Inputs: 
             *   Dir        Current directory class
             *   FileList   List of files in directory
             * Outputs:
             *   Dir        Current directory class
             */
            int
                TrackNumber;
            string
                TrackNumberStr,
                TrackTitle;

            Log.WriteLine("  Deriving track metadata from file names");
            // build title for each track from basename, ignore prefix, extension
            // artist name will be the same for each track

            TrackNumber = 0;
            foreach (FileInfo fi in FileList)
            {
                // increment track number and convert to two place string
                TrackNumber++;
                TrackNumberStr = TrackNumber.ToString("00");
                // get filename without extension
                TrackTitle = Path.GetFileNameWithoutExtension(fi.FullName);
                // remove track number prefix
                // (one or more digits, optional period, one or more spaces)
                TrackTitle = Regex.Replace(TrackTitle, @"^\d+\.?\s+", "");
                 // populate track metadata
                Dir.TitleList.Add(TrackTitle);
                Dir.ArtistList.Add(Dir.AlbumArtist);
                Log.WriteLine("    " + TrackNumberStr + SPACE + TrackTitle);
            }
        }  // end GetTrackMetadataFromFileNames

        static void PopulateTrackDurationList(clDirInfo Dir, FileInfo[] FileList)
        {
            /* Reads track data from FileList, and populates TrackDurationList
             * Inputs:
             *   Dir class
             *   FileList   List of audio tracks
             * Calls:
             *   GetTrackDuration
             * Outputs:
             *   Dir.TrackDurationList
             */
            int i;
            string TrackFilePath;

            for (i = 0; i < FileList.Length; i++)
            {
                try
                {
                    // read audio filename and populate corresponding track duration
                    TrackFilePath = FileList[i].FullName;
                    Dir.TrackDurationList.Add(GetTrackDuration(TrackFilePath));
                }
                catch (Exception e)
                {
                    Log.WriteLine("*** Fatal program exception reading file: " + FileList);
                    if (Debug) Log.WriteLine(e.Message);
                    Environment.Exit(0);
                }
            }
        } // end PopulateTrackDurationList
    }
}