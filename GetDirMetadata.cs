using System;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static void GetDirInformation(AATB_DirInfo Dir, FileInfo[] ParentInfotextList, FileInfo[] ParentCuesheetList)
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
             *     Dir.ParentInfotextPath
             *     Dir.ParentCuesheetPath
             */
            int i;

             // get directory basename and extension
            (Dir.BaseName, Dir.Extension) = SplitString(Dir.Name, PERIOD);

            // determine type of directory
            if (Dir.Name == RAW)
                Dir.Type = RAWAUDIO;
            else if (AudioBitrates.Contains(Dir.Name))
                Dir.Type = TRACKEDAUDIO;
            else if (CompressedDirExtensions.Contains(Dir.Extension))
                Dir.Type = COMPRESSEDAUDIO;
            else
                Dir.Type = OTHER;
            if (Debug) Console.WriteLine("dbg: Dir type: {0}", Dir.Type);

            // get audio compression format from directory extension
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
            if (Debug) Console.WriteLine("dbg: Dir ext: {0}  Dir bitrate: {1}", Dir.Extension, Dir.Bitrate);

            // get first parent directory infotext file - ignore multiple infotext files
            if (UseInfotext && ParentInfotextList.Length >= 1)
            {
                Dir.ParentInfotextPath = ParentInfotextList[0].FullName;
                if (ParentInfotextList.Length > 1)
                    Log.WriteLine("*** Multiple infotext files exist, using: " + ParentInfotextList[0].Name);
            }

            // get first parent directory cuesheet - ignore multiple cuesheets
            if (UseCuesheet && ParentCuesheetList.Length >= 1)
            {
                Dir.ParentCuesheetPath = ParentCuesheetList[0].FullName;
                if (ParentInfotextList.Length > 1)
                    Log.WriteLine("*** Multiple cuesheet files exist, using: " + ParentCuesheetList[0].Name);
            }

        } //end GetDirInformation

        static void GetDirMetadata(AATB_DirInfo Dir)
        {
            /* Extract directory metadata
             * Inputs:
             *   Dir class
             *   InfotextPath location of info.txt file with recording information
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
            string
                BaseName,
                TargetInfotextFilePath,
                TargetCuesheetFilePath;

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
                    // concatentate to album and parent base name
                    // Dir.Album += (SPACE + Dir.Stage);
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
            if (Debug) Console.WriteLine("dbg: ParentBasename = {0}", Dir.ParentBaseName);

            // check metadata info file exists and is in the correct format
            if (UseInfotext
                && Dir.ParentInfotextPath != null)
            {
                // if infotext filepath is not in correct format, rename it
                TargetInfotextFilePath = Dir.ParentPath + BACKSLASH + Dir.ParentBaseName + PERIOD + INFOTXT;
                if (RenameInfoFiles
                    && (Dir.ParentInfotextPath != TargetInfotextFilePath))
                {
                    Log.WriteLine("  Renaming info.txt file to: " + TargetInfotextFilePath);
                    if (MoveFile(Dir.ParentInfotextPath, TargetInfotextFilePath))
                        Dir.ParentInfotextPath = TargetInfotextFilePath;
                }
            }
            else if (UseCuesheet
                     && Dir.ParentCuesheetPath != null)
            {
                // if cuesheet filepath is not in correct format, rename it
                TargetCuesheetFilePath = Dir.ParentPath + BACKSLASH + Dir.ParentBaseName + PERIOD + INFOCUE;
                if (RenameInfoFiles
                    && (Dir.ParentCuesheetPath != TargetCuesheetFilePath))
                {
                    Log.WriteLine("  Renaming cuesheet file to: " + TargetCuesheetFilePath);
                    if (MoveFile(Dir.ParentCuesheetPath, TargetCuesheetFilePath))
                        Dir.ParentCuesheetPath = TargetCuesheetFilePath;
                }
            }

            // for wav or compressed audio directories only
            if ((CompressAudio || CreateCuesheet) && Dir.Type == TRACKEDAUDIO
                || VerifyAudio && Dir.Type == COMPRESSEDAUDIO
                && Dir.Name != RAW)
            {
                // initialize metadata source to directory name
                // infotext or cuesheet information may be used later to overwrite this
                // metadata later, if the file is present and the information is correct
                Dir.MetadataSource = DIRNAME;

                // get metadata from infotext
                if (UseInfotext)
                    GetDirMetadataFromInfotext(Dir);

                // get metadata from cuesheet
                else if (UseCuesheet)
                    GetDirMetadataFromCuesheet(Dir);

                // if infotext or cuesheet data is not valid, MetadataSource remains DIRNAME
                if (Dir.MetadataSource == DIRNAME)
                    GetDirMetadataFromDirectoryName(Dir);

                // remove any leading spaces from Album string
                Dir.Album = Regex.Replace(Dir.Album, @"^\s*", "");

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
             *   InfotextPath   location of info.txt file with recording information
             *   Info header format with four lines:
             *     Artist
             *     Venue
             *     Location
             *     Concert Date (yyyy-mm-dd)
             *   Info header format with five lines:
             *     Artist
             *     Event
             *     Stage
             *     Location
             *     Concert Date (yyyy-mm-dd)
             *   Info alternate header using labels
             *     Artist: <artist>
             *     Event: <event>
             *     Venue: <venue>
             *     Stage: <stage>
             *     Location: <location>
             *     Date: <yyyy-mm-dd>
             *     
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
                InfotextFileName;
            string[]
                DataList;
            Match
                DateMatchLine4,
                DateMatchLine5;

            InfotextFileName = SplitFileName(Dir.ParentInfotextPath);
            if (File.Exists(Dir.ParentInfotextPath))
            {
                Log.WriteLine("  Reading album metadata from info file: " + InfotextFileName);
                // read data from text file
                DataList = ReadTextFile(Dir.ParentInfotextPath);
                if (DataList.Length > 4)
                {
                    // check for date on 4th and 5th lines
                    // Regex expression to match date format 1xxx-xx-xx or 2xxx-xx-xx
                    DateMatchLine4 = Regex.Match(DataList[3], @"^[1-2]\d{3}-\d{2}-\d{2}");
                    DateMatchLine5 = Regex.Match(DataList[4], @"^[1-2]\d{3}-\d{2}-\d{2}");
                    // date is on 4th line
                    if (DateMatchLine4.Success)
                    {
                        Dir.AlbumArtist = DataList[0];
                        Dir.Event = null;
                        Dir.Venue = DataList[1];
                        Dir.Stage = null;
                        Dir.Location = DataList[2];
                        Dir.ConcertDate = DataList[3];
                    }
                    // date is on 5th line
                    else if (DateMatchLine5.Success)
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
                        Dir.AlbumArtist = SearchList(DataList, "Artist: ");
                        Dir.Event = SearchList(DataList, "Event: ");
                        Dir.Venue = SearchList(DataList, "Venue: ");
                        Dir.Stage = SearchList(DataList, "Stage: ");
                        Dir.Location = SearchList(DataList, "Location: ");
                        Dir.ConcertDate = SearchList(DataList, "Date: ");
                    }
                }

                // album = event + venue + date + stage, separated by spaces
                Dir.Album = Dir.Event;
                if (Dir.Venue != null)
                    Dir.Album += (SPACE + Dir.Venue);
                if (Dir.ConcertDate != null)
                    Dir.Album += (SPACE + Dir.ConcertDate);
                if (Dir.Stage != null)
                    Dir.Album += (SPACE + Dir.Stage);

                // verify metadata has been found and reset MetadataSource
                // artist and concert date are minimum required to document concert
                if (Dir.AlbumArtist != null && Dir.ConcertDate != null)
                    Dir.MetadataSource = INFOFILE;
                else
                    Log.WriteLine("*** Artist and concert date missing from info file");
            }
            else
                Log.WriteLine("*** Infotext file not found");

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
            CuesheetFileName = SplitFileName(Dir.ParentCuesheetPath);
            if (File.Exists(Dir.ParentCuesheetPath))
            {
                Log.WriteLine("  Reading album metadata from cuesheet: " + CuesheetFileName);

                // read data from text file
                DataList = ReadTextFile(Dir.ParentCuesheetPath);

                // search for standard metadata labels
                Dir.AlbumArtist = SearchList(DataList, "PERFORMER ");
                Dir.Album = SearchList(DataList, "TITLE ");
                Dir.Event = SearchList(DataList, "EVENT ");
                Dir.Venue = SearchList(DataList, "VENUE ");
                Dir.Stage = SearchList(DataList, "STAGE ");
                Dir.Location = SearchList(DataList, "LOCATION ");
                Dir.ConcertDate = SearchList(DataList, "DATE ");

                // if album not found in cuesheet, then build album string
                // album = event + venue + date + stage, separated by spaces
                if (Dir.Album == null)
                {
                    Dir.Album = Dir.Event;
                    if (Dir.Venue != null && Dir.Venue.Length > 0)
                        Dir.Album += (SPACE + Dir.Venue);
                    if (Dir.ConcertDate != null && Dir.ConcertDate.Length > 0)
                        Dir.Album += (SPACE + Dir.ConcertDate);
                    if (Dir.Stage != null && Dir.Stage.Length > 0)
                        Dir.Album += (SPACE + Dir.Stage);
                }

                // verify metadata has been found and reset MetadataSource
                // artist and album name are minimum required
                if (Dir.AlbumArtist != null && Dir.Album != null)
                    Dir.MetadataSource = CUESHEET;
                // otherwise Dir.Metadatasource remains Directory
                else
                    Log.WriteLine("*** Performer and Title information missing from cuesheet");
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
             *     Dir.ConcertDate
             */

            Log.WriteLine("  Deriving album metadata from directory name");
            switch (Dir.RecordingType)
            {
                case LIVE:
                {
                    Dir.AlbumArtist = Dir.BaseNameTemp1;
                    // album = artist + date + stage, separated by spaces
                    Dir.Album = Dir.AlbumArtist;
                    Dir.ConcertDate = Dir.BaseNameTemp2;
                    if (Dir.ConcertDate != String.Empty)
                        Dir.Album += SPACE + Dir.ConcertDate;
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