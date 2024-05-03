using System;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static void GetDirInformation(AATB_DirInfo Dir)
        {
            /* Populate basic directory information
             * Inputs:
             *   Dir class
             * Outputs:
             *   Dir class
             *     Dir.BaseName
             *     Dir.Extension
             *     Dir.Type
             *     Dir.AudioCompressionFormat
             *     Dir.Bitrate
             *     Dir.InfotextPath
             *     Dir.CuesheetPath
             */
            int i;

            // get directory basename and extension
            (Dir.BaseName, Dir.Extension) = SplitString(PERIOD, Dir.Name);

            // determine type of directory
            if (Dir.Name == RAW)
                Dir.Type = RAWAUDIO;
            else if (AudioBitrates.Contains(Dir.Name))
                Dir.Type = TRACKEDAUDIO;
            else if (CompressedDirExtensions.Contains(Dir.Extension))
                Dir.Type = COMPRESSEDAUDIO;
            else
                Dir.Type = OTHER;

            // get audio compression format from directory name
            // ignore last entry in list = WAV
            if (Dir.Extension != null)
                for (i = 0; i <= AudioFormats.Length - 2; i++)
                {
                    if (Dir.Extension.Contains(AudioFormats[i]))
                    {
                        Dir.AudioCompressionFormat = AudioFormats[i];
                        break;
                    }
                }
            // get audio bitrate (if exists) from directory name
            if (Dir.BaseName != null)
                for (i = 0; i <= AudioBitrates.Length - 1; i++)
                {
                    if (Dir.BaseName.Contains(AudioBitrates[i]))
                    {
                        Dir.Bitrate = AudioBitrates[i];
                        break;
                    }
                }
            if (Debug) Console.WriteLine("dbg: Dir type: {0}  Extension: {1}  Comp Format: {2}  Bitrate: {3}",
                                        Dir.Type, Dir.Extension, Dir.AudioCompressionFormat, Dir.Bitrate);

        } // end GetDirInformation

        static void GetDirTextFiles(AATB_DirInfo Dir, FileInfo[] InfotextList, FileInfo[] CuesheetList)
        {
            /* Reads infotext and cuesheet lists to get the first entry in the list
             * and rename it if appropriate. UseInfotext and UseCuesheet flags are mutually exclusive
             * Inputs:
             *   Lists of all text files in the directory
             *     Dir.InfotextList
             *     Dir.CuesheetList
             * Outputs:
             *   The correct text file path
             *     Dir.InfotextPath
             *     Dir.CuesheetPath
             * Note: if the inputlist is empty, the Dir values will not be populated
             * and an error message will be generated later when trying to open an empty file
             */
            string TargetFilePath;

            if (UseInfotext)
            {
                if (InfotextList.Length > 1)
                    Log.WriteLine("*** Multiple infotext files exist, using: " + InfotextList[0].Name);

                if (InfotextList.Length >= 1)
                {
                    Dir.InfotextPath = InfotextList[0].FullName;
                    // choose current or parent directory
                    if (UseCurrentDirInfo)
                        TargetFilePath = Dir.Path + BACKSLASH + Dir.BaseName + PERIOD + INFOTXT;
                    else
                        TargetFilePath = Dir.ParentPath + BACKSLASH + Dir.ParentBaseName + PERIOD + INFOTXT;

                    // rename info file if needed
                    if (RenameInfoFiles
                        && (Dir.InfotextPath != TargetFilePath))
                    {
                        Log.WriteLine("  Renaming infotext file to: " + TargetFilePath);
                        if (MoveFile(Dir.InfotextPath, TargetFilePath))
                            Dir.InfotextPath = TargetFilePath;
                    }
                }
            }

            if (UseCuesheet)
            {
                if (CuesheetList.Length > 1)
                    Log.WriteLine("*** Multiple cuesheet files exist, using: " + CuesheetList[0].Name);

                if (CuesheetList.Length >= 1)
                {
                    Dir.CuesheetPath = CuesheetList[0].FullName;
                    // choose current or parent directory
                    if (UseCurrentDirInfo)
                        TargetFilePath = Dir.Path + BACKSLASH + Dir.BaseName + PERIOD + INFOCUE;
                    else
                        TargetFilePath = Dir.ParentPath + BACKSLASH + Dir.ParentBaseName + PERIOD + INFOCUE;

                    // rename info file if needed
                    if (RenameInfoFiles
                        && (Dir.CuesheetPath != TargetFilePath))
                    {
                        Log.WriteLine("  Renaming cuesheet file to: " + TargetFilePath);
                        if (MoveFile(Dir.CuesheetPath, TargetFilePath))
                            Dir.CuesheetPath = TargetFilePath;
                    }
                }
            }
        } //end GetDirTextFiles

        static void GetDirMetadata(AATB_DirInfo Dir)
        {
            /* Extract directory metadata
             * Inputs:
             *   Dir class
             *   InfotextPath location of infotext file with recording information
             *   CuesheetPath location of cuesheet file
             * Outputs:
             *   Dir class
             *     Dir.RecordingType
             *     Dir.ParentBaseName
             *     Dir.AlbumArtist
             *     Dir.ConcertDate
             *     Dir.Album
             *     Dir.Stage
             *   
             * Directory name formats
             *   Live recording: <artist> <date>, <artist>-<date>, <artist>_<date>, <artist><date>
             *     datestring is in format: yyyy-mm-dd
             *   Commercial recording: <artist> - <album>
             *   Other: all other formats
             */
            string BaseName;

            // parent base name will be used to create compressed audio subdirectories
            // remove prefix number if it exists
            // (from beginning of line: one or more digits, optional period, one or more spaces)
            BaseName = Regex.Replace(Dir.ParentName, @"^\d+\.?\s+", "");

            // check for live recording format: embedded date "yyyy-mm-dd stage"
            Dir.PatternMatchDate = Regex.Match(BaseName, @"[1-2]\d{3}-\d{2}-\d{2}");
            // check for commercial CD format: embedded " - "
            Dir.PatternMatchSHS = Regex.Match(BaseName, @SPACEHYPHENSPACE);

            // live recording format
            if (Dir.PatternMatchDate.Success)
            {
                Dir.RecordingType = LIVE;
                // extract album artist
                Dir.BaseNameTemp1 = BaseName.Substring(0, Dir.PatternMatchDate.Index);
                // remove trailing non-word [A-Z][a-z][0-9] character including whitespace
                Dir.BaseNameTemp1 = Regex.Replace(Dir.BaseNameTemp1, @"[^A-Za-z0-9]$", "");
                // convert to title case/lower case if appropriate
                Dir.BaseNameTemp1 = ConvertCase(Dir.BaseNameTemp1);
                // extract concert date, 10 chars long (yyyy-mm-dd)
                Dir.BaseNameTemp2 = BaseName.Substring(Dir.PatternMatchDate.Index, 10);
                // build parent basename = <artist>_<date>.<stage>
                Dir.ParentBaseName = Dir.BaseNameTemp1 + UNDERSCORE + Dir.BaseNameTemp2;
                // extract stage name if it exists (skip char 11 delimiter between date and stage name)
                if (BaseName.Length > Dir.PatternMatchDate.Index + 11)
                {
                    Dir.BaseNameTemp3 = BaseName.Substring(Dir.PatternMatchDate.Index + 11);
                    // convert to title case/lower case if appropriate
                    Dir.BaseNameTemp3 = ConvertCase(Dir.BaseNameTemp3);
                    Dir.ParentBaseName += (PERIOD + Dir.BaseNameTemp3);
                }
                // remove any remaining embedded spaces in basename
                Dir.ParentBaseName = Regex.Replace(Dir.ParentBaseName, @"\s", "");
            }

            // commercial recording format
            else if (Dir.PatternMatchSHS.Success)
            {
                Dir.RecordingType = CD;
                Dir.ParentBaseName = BaseName;
                Dir.BaseNameTemp1 = BaseName.Substring(0, Dir.PatternMatchSHS.Index);
                Dir.BaseNameTemp2 = BaseName.Substring(Dir.PatternMatchSHS.Index + 2);
            }

            // other directory format
            else
            {
                Dir.RecordingType = OTHER;
                Dir.ParentBaseName = BaseName;
                // no further metadata or parent base name required
            }
            if (Debug) Console.WriteLine("dbg: Recording Type: {0}", Dir.RecordingType);
            if (Debug) Console.WriteLine("dbg: ParentBasename: {0}", Dir.ParentBaseName);

            // for wav or compressed audio directories only
            if ((CompressAudio || CreateCuesheet) && Dir.Type == TRACKEDAUDIO
                || VerifyAudio && Dir.Type == COMPRESSEDAUDIO
                && Dir.Name != RAW)
            {
                // initialize metadata source to directory name
                // infotext or cuesheet information may be used later to overwrite this
                // metadata later, if the file is present and the information is correct
                Dir.DirMetadataSource = DIRNAME;

                // get metadata from infotext (live concerts only)
                if (UseInfotext)
                    GetDirMetadataFromInfotext(Dir);

                // get metadata from cuesheet
                else if (UseCuesheet)
                    GetDirMetadataFromCuesheet(Dir);

                // if infotext or cuesheet data is not valid, DirMetadataSource remains DIRNAME
                if (Dir.DirMetadataSource == DIRNAME)
                    GetDirMetadataFromDirectoryName(Dir);

                // remove leading/trailing quotes or spaces from Album string
                Dir.Album = CleanDataString(Dir.Album);

                // log metadata info
                Log.WriteLine("    Artist: " + Dir.AlbumArtist);
                Log.WriteLine("    Album: " + Dir.Album);
                // the following metadata is extracted from info file or cuesheet
                // not applicable to CD recording
                if (Dir.RecordingType != CD)
                {
                    if (Dir.Venue != null) Log.WriteLine("    Venue: " + Dir.Venue);
                    if (Dir.Stage != null) Log.WriteLine("    Stage: " + Dir.Stage);
                    if (Dir.Location != null) Log.WriteLine("    Location: " + Dir.Location);
                    if (Dir.ConcertDate != null) Log.WriteLine("    Date: " + Dir.ConcertDate);
                }
            }
        }  // end GetDirMetadata

        static void GetDirMetadataFromInfotext(AATB_DirInfo Dir)
        {
            /* Extract directory metadata from infotext file
             * Note: will overwrite existing metadata derived from directory name
             * Inputs:
             *   Dir class
             *   InfotextPath   location of infotext file with recording information
             *   Info header format with four lines:
             *     Performer (Artist)
             *     Venue
             *     Location
             *     Concert Date (yyyy-mm-dd)
             *   Info header format with five lines:
             *     Performer (Artist)
             *     Event
             *     Stage
             *     Location
             *     Concert Date (yyyy-mm-dd)
             *   Info alternate header using labels
             *     PERFORMER <artist>
             *     EVENT<event>
             *     VENUE <venue>
             *     STAGE <stage>
             *     LOCATION <location>
             *     DATE <yyyy-mm-dd>
             * Outputs:
             *   Dir class
             *     Dir.AlbumArtist
             *     Dir.Event
             *     Dir.Venue 
             *     Dir.Stage
             *     Dir.Location
             *     Dir.ConcertDate
             */
            int DateLineNumber;
            string
                InfotextFileName;
            string[]
                DataList;

            InfotextFileName = SplitFileName(Dir.InfotextPath);
            if (File.Exists(Dir.InfotextPath))
            {
                Log.WriteLine("  Reading album metadata from info file: " + InfotextFileName);
                // read data from text file
                DataList = ReadTextFile(Dir.InfotextPath);
                // search for date format yyyy-mm-dd, where year=19xx or 20xx 
                // returns zero based line number for valid date, otherwise 0
                DateLineNumber = GetLineNumberOfSearchTerm(0, "^((19|20)\\d{2}-\\d{2}-\\d{2})", DataList);
                // valid date on line number 4
                if (DateLineNumber == 3)
                {
                    Dir.AlbumArtist = DataList[0];
                    Dir.Event = null;
                    Dir.Venue = DataList[1];
                    Dir.Stage = null;
                    Dir.Location = DataList[2];
                    Dir.ConcertDate = DataList[3];
                }
                // valid date on line number 5
                else if (DateLineNumber == 4)
                {
                    Dir.AlbumArtist = DataList[0];
                    Dir.Event = DataList[1];
                    Dir.Venue = null;
                    Dir.Stage = DataList[2];
                    Dir.Location = DataList[3];
                    Dir.ConcertDate = DataList[4];
                }
                // otherwise search for metadata labels, find first instance of each label
                else
                {
                    Dir.AlbumArtist = GetDataAfterSearchTerm("PERFORMER", DataList);
                    Dir.Album = GetDataAfterSearchTerm("TITLE", DataList);
                    Dir.Event = GetDataAfterSearchTerm("EVENT", DataList);
                    Dir.Venue = GetDataAfterSearchTerm("VENUE", DataList);
                    Dir.Stage = GetDataAfterSearchTerm("STAGE", DataList);
                    Dir.Location = GetDataAfterSearchTerm("LOCATION", DataList);
                    Dir.ConcertDate = GetDataAfterSearchTerm("DATE", DataList);
                }

                // verify minimum metadata has been found
                if (Dir.AlbumArtist == null)
                    Log.WriteLine("*** Artist missing from info file");
                switch (Dir.RecordingType)
                {
                    case LIVE:
                    {
                        if (Dir.Album == null)
                        {
                            // build album string: event + venue + location + date + stage
                            Dir.Album += ( Dir.Event + SPACE
                                         + Dir.Venue + SPACE
                                         + Dir.Location + SPACE
                                         + Dir.ConcertDate + SPACE
                                         + Dir.Stage);
                            // remove multiple spaces
                            Dir.Album = Regex.Replace(Dir.Album, @"\s+", SPACE);
                        }
                        if (Dir.ConcertDate == null)
                            Log.WriteLine("*** Concert date missing from info file");
                        if (Dir.AlbumArtist != null && Dir.ConcertDate != null)
                            Dir.DirMetadataSource = INFOFILE;
                        break;
                    }
                    case CD:
                    case OTHER:
                    {
                        if (Dir.Album == null)
                            Log.WriteLine("*** Album title missing from info file");
                        if (Dir.AlbumArtist != null && Dir.Album != null)
                            Dir.DirMetadataSource = INFOFILE;
                        break;
                    }
                }
            }
            else
                Log.WriteLine("*** Infotext file not found:" + InfotextFileName);

        } // end GetDirMetadataFromInfotext

        static void GetDirMetadataFromCuesheet(AATB_DirInfo Dir)
        {
            /* Extract directory metadata from cuesheet
             * Finds first instance of each label, others are ignored
             * Note: will overwrite existing metadata derived from directory name
             * Inputs:
             *   Dir class
             *   CuesheetPath   location of .cue file with recording information
             *   Info header format
             *     PERFORMER <artist>
             *     TITLE <album>
             *     EVENT <event>
             *     VENUE <venue>
             *     STAGE <stage>
             *     LOCATION <location>
             *     DATE <yyyy-mm-dd>
             * Outputs:
             *   Dir class
             *     Dir.AlbumArtist
             *     Dir.Event
             *     Dir.Venue 
             *     Dir.Stage
             *     Dir.Location
             *     Dir.ConcertDate
             */
            string
                CuesheetFileName;
            string[]
                DataList;

            // get parent directory cuesheet filename
            CuesheetFileName = SplitFileName(Dir.CuesheetPath);
            if (File.Exists(Dir.CuesheetPath))
            {
                Log.WriteLine("  Reading album metadata from cuesheet: " + CuesheetFileName);

                // read data from text file
                DataList = ReadTextFile(Dir.CuesheetPath);

                // search for standard metadata labels
                Dir.AlbumArtist = GetDataAfterSearchTerm("PERFORMER", DataList);
                Dir.Album = GetDataAfterSearchTerm("TITLE", DataList);
                Dir.Event = GetDataAfterSearchTerm("EVENT", DataList);
                Dir.Venue = GetDataAfterSearchTerm("VENUE", DataList);
                Dir.Stage = GetDataAfterSearchTerm("STAGE", DataList);
                Dir.Location = GetDataAfterSearchTerm("LOCATION", DataList);
                Dir.ConcertDate = GetDataAfterSearchTerm("DATE", DataList);

                // verify minimum metadata has been found
                if (Dir.AlbumArtist == null)
                    Log.WriteLine("*** Artist missing from cuesheet");
                switch (Dir.RecordingType)
                {
                    case LIVE:
                    {
                        if (Dir.Album == null)
                        {
                            // build album string: event + venue + location + date + stage
                            Dir.Album += ( Dir.Event + SPACE
                                         + Dir.Venue + SPACE
                                         + Dir.Location + SPACE
                                         + Dir.ConcertDate + SPACE
                                         + Dir.Stage);
                            // remove multiple spaces
                            Dir.Album = Regex.Replace(Dir.Album, @"\s+", SPACE);
                        }
                        if (Dir.ConcertDate == null)
                            Log.WriteLine("*** Concert date missing from cuesheet");
                        if (Dir.AlbumArtist != null && Dir.ConcertDate != null)
                            Dir.DirMetadataSource = CUESHEET;
                        break;
                    }
                    case CD:
                    case OTHER:
                    {
                        if (Dir.Album == null)
                            Log.WriteLine("*** Album title missing from cuesheet");
                        if (Dir.AlbumArtist != null && Dir.Album != null)
                            Dir.DirMetadataSource = CUESHEET;
                        break;
                    }
                }
            }
            else
                Log.WriteLine("*** Cuesheet not found: " + CuesheetFileName);

        } // end GetDirMetadataFromCuesheet

        static void GetDirMetadataFromDirectoryName(AATB_DirInfo Dir)
        {
            /* Get directory metadata from directory name
             * Inputs:
             *   Dir   Directory as AATB_DirInfo class instance
             *     Dir.BaseNameTemp1
             *     Dir.BaseNameTemp2
             *     Dir.BaseNameTemp3
             *     Dir.RecordingType
             * Outputs:
             *   Dir   Directory as AATB_DirInfo class instance
             *     Dir.AlbumArtist
             *     Dir.Album
             *     Dir.ConcertDate (live recording)
             *     Dir.Stage (live recording)
             */

            Log.WriteLine("  Deriving album metadata from directory name");
            switch (Dir.RecordingType)
            {
                case LIVE:
                {
                    Dir.AlbumArtist = Dir.BaseNameTemp1;
                    Dir.Album = Dir.AlbumArtist;
                    // concatenate date to album if it exists
                    Dir.ConcertDate = Dir.BaseNameTemp2;
                    if (Dir.ConcertDate != String.Empty)
                        Dir.Album += SPACE + Dir.ConcertDate;
                    // concatenate stage to album if it exists
                    Dir.Stage = Dir.BaseNameTemp3;
                    if (Dir.BaseNameTemp3 != String.Empty)
                        Dir.Album += SPACE + Dir.Stage;
                    break;
                }
                case CD:
                {
                    Dir.AlbumArtist = Dir.BaseNameTemp1;
                    Dir.Album = Dir.BaseNameTemp2;
                    break;
                }
                case OTHER:
                {
                    Dir.AlbumArtist = Dir.ParentBaseName;
                    Dir.Album = Dir.ParentBaseName;
                    break;
                }
            }
            // convert to title case/lower case if appropriate
            Dir.AlbumArtist = ConvertCase(Dir.AlbumArtist);
            Dir.Album = ConvertCase(Dir.Album);

        } // end GetDirMetadataFromDirectoryName
    }
}