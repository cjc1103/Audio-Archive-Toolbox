﻿using System;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Appointments.AppointmentsProvider;

namespace nsAATB
{
    public partial class clMain
    {
        static void GetDirInformation(clDirInfo Dir)
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

        static void GetDirTextFiles(clDirInfo Dir, FileInfo[] InfotextList, FileInfo[] CuesheetList)
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

                    // rename infotext file if needed
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

                    // rename cuesheet file if needed
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

        static void GetDirMetadata(clDirInfo Dir)
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
             *   Live recording: <artist> <date> <optional set or stage qualifier>
             *     Note: date is in format: yyyy-mm-dd (global string constant DateFormat)
             *     Note: BaseNameTemp1 = artist, BaseNameTemp2 = <optional set or stage>
             *   Commercial recording: <artist> - <album>
             *     Note: BaseNameTemp1 = artist, BaseNameTemp2 = <album>
             *   Other: all other formats
             */
            string BaseName;

            // parent base name will be used to create compressed audio subdirectories
            // remove prefix number if it exists
            // (from beginning of line: one or more digits, optional period, one or more spaces)
            BaseName = Regex.Replace(Dir.ParentName, @"^\d+\.?\s+", "");

            // check for live recording format: embedded date "yyyy-mm-dd stage"
            Dir.PatternMatchDate = Regex.Match(BaseName, @DateFormat);
            // check for commercial recording format: embedded " - "
            Dir.PatternMatchSHS = Regex.Match(BaseName, @SPACEHYPHENSPACE);

            // live recording format
            if (Dir.PatternMatchDate.Success)
            {
                Dir.RecordingType = LIVE;
                // extract album artist
                Dir.BaseNameTemp1 = BaseName.Substring(0, Dir.PatternMatchDate.Index);
                // remove trailing non-word characters including spaces
                Dir.BaseNameTemp1 = Regex.Replace(Dir.BaseNameTemp1, @"[^A-Za-z0-9]+$", "");
                // convert to title case/lower case if appropriate
                Dir.BaseNameTemp1 = ConvertCase(Dir.BaseNameTemp1);
                // extract concert date, 10 chars long (yyyy-mm-dd)
                Dir.BaseNameTemp2 = BaseName.Substring(Dir.PatternMatchDate.Index, 10);
                // remove leading and trailing non-word characters including spaces
                Dir.BaseNameTemp2 = Regex.Replace(Dir.BaseNameTemp2, @"^[^A-Za-z0-9]", "");
                Dir.BaseNameTemp2 = Regex.Replace(Dir.BaseNameTemp2, @"[^A-Za-z0-9]+$", "");
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
                Dir.ParentBaseName = Regex.Replace(Dir.ParentBaseName, @"\s+", "");
            }

            // commercial recording format
            else if (Dir.PatternMatchSHS.Success)
            {
                Dir.RecordingType = COMMERCIAL;
                Dir.ParentBaseName = BaseName;
                Dir.BaseNameTemp1 = BaseName.Substring(0, Dir.PatternMatchSHS.Index);
                Dir.BaseNameTemp2 = BaseName.Substring(Dir.PatternMatchSHS.Index + 2);
            }

            // other directory format
            else
            {
                Dir.RecordingType = OTHER;
                Dir.ParentBaseName = BaseName;
            }
            if (Debug) Console.WriteLine("dbg: ParentBasename: {0}", Dir.ParentBaseName);

            // for wav or compressed audio directories only
            if ((CompressAudio || CreateCuesheet) && Dir.Type == TRACKEDAUDIO
                || VerifyAudio && Dir.Type == COMPRESSEDAUDIO
                && Dir.Name != RAW)
            {
                // initialize metadata source to directory name
                Dir.DirMetadataSource = DIRNAME;

                // get metadata from infotext
                // if metadata is valid, the Dir metadata source is reset to INFOTXT
                if (UseInfotext)
                    GetDirMetadataFromInfotext(Dir);

                // get metadata from cuesheet
                // if metadata is valid, the Dir metadata source is reset to CUESHEET
                else if (UseCuesheet)
                    GetDirMetadataFromCuesheet(Dir);

                // if infotext or cuesheet data is not valid, get metadata from directory name
                if (Dir.DirMetadataSource == DIRNAME)
                    GetDirMetadataFromDirectoryName(Dir);

                // remove leading/trailing quotes or spaces from Album string
                Dir.Album = CleanDataString(Dir.Album);

                // log metadata info
                Log.WriteLine("  Recording type: " + Dir.RecordingType);
                Log.WriteLine("    Artist: " + Dir.AlbumArtist);
                if (Dir.Event != null) Log.WriteLine("    Event: " + Dir.Event);
                if (Dir.Venue != null) Log.WriteLine("    Venue: " + Dir.Venue);
                if (Dir.Stage != null) Log.WriteLine("    Stage: " + Dir.Stage);
                if (Dir.Location != null) Log.WriteLine("    Location: " + Dir.Location);
                Log.WriteLine("    Album: " + Dir.Album);
            }
        }  // end GetDirMetadata

        static void GetDirMetadataFromInfotext(clDirInfo Dir)
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
             *     Location
             *     Stage
             *     Concert Date (yyyy-mm-dd)
             *   Info alternate header using labels (case insensitive, optional colon)
             *     PERFORMER <artist>
             *     EVENT<event>
             *     VENUE <venue>
             *     LOCATION <location>
             *     STAGE <stage>
             *     DATE <yyyy-mm-dd>
             * Outputs:
             *   Dir class
             *     Dir.AlbumArtist
             *     Dir.Event
             *     Dir.Venue 
             *     Dir.Location
             *     Dir.Stage
             *     Dir.ConcertDate
             */
            int DateLineNumber;
            string
                InfotextFileName;
            string[]
                DataList;
            bool
                ValidConcertDate = false;

            InfotextFileName = SplitFileName(Dir.InfotextPath);
            if (File.Exists(Dir.InfotextPath))
            {
                Log.WriteLine("  Reading album metadata from infotext file: " + InfotextFileName);
                // read data from text file
                DataList = ReadTextFile(Dir.InfotextPath);

                switch (Dir.RecordingType)
                {
                    case LIVE:
                    {
                        // search DataList for concert date at beginning of line (col 0)
                        // returns line number of date, -1 if date not found
                        DateLineNumber = GetLineNumberOfSearchTerm(0, "^" + DateFormat, DataList);
                        // valid date on line number 4
                        if (DateLineNumber == 3)
                        {
                            Dir.AlbumArtist = DataList[0];
                            Dir.Event = null;
                            Dir.Venue = DataList[1];
                            Dir.Stage = null;
                            Dir.Location = DataList[2];
                            Dir.ConcertDate = DataList[3];
                            ValidConcertDate = true;
                            if (Debug) Console.WriteLine("dbg: Found concert date on infotext line 4");
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
                            ValidConcertDate = true;
                            if (Debug) Console.WriteLine("dbg: Found concert date on infotext line 5");
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
                            ValidConcertDate = ValidateDate(Dir.ConcertDate);
                        }
                        // if album string was not found, build it
                        if (Dir.Album == null)
                        {
                            Dir.Album = ( Dir.Event + SPACE
                                        + Dir.Venue + SPACE
                                        + Dir.Stage + SPACE
                                        + Dir.Location + SPACE
                                        + Dir.ConcertDate);
                            // remove redundant multiple spaces
                            Dir.Album = Regex.Replace(Dir.Album, @"\s+", SPACE);
                        }
                        // verify minimum metadata has been found
                        if (Dir.AlbumArtist == null)
                            Log.WriteLine("*** Artist missing from infotext");
                        if (!ValidConcertDate)
                            Log.WriteLine("*** Concert date is missing/incorrect format from infotext");
                        // if metadata is valid, reset metadata source
                        if (Dir.AlbumArtist != null && ValidConcertDate)
                            Dir.DirMetadataSource = INFOFILE;
                        break;
                    }
                    case COMMERCIAL:
                    case OTHER:
                    {
                        // search for metadata labels, find first instance of each label
                        Dir.AlbumArtist = GetDataAfterSearchTerm("PERFORMER", DataList);
                        Dir.Album = GetDataAfterSearchTerm("TITLE", DataList);
                        // verify minimum metadata has been found
                        if (Dir.AlbumArtist == null)
                            Log.WriteLine("*** Artist missing from infotext");
                        if (Dir.Album == null)
                            Log.WriteLine("*** Album information missing from infotext");
                        // if metadata is valid, reset metadata source
                        if (Dir.AlbumArtist != null && Dir.Album != null)
                            Dir.DirMetadataSource = INFOFILE;
                        break;
                    }
                }
            }
            else
                Log.WriteLine("*** Infotext file not found");

        } // end GetDirMetadataFromInfotext

        static void GetDirMetadataFromCuesheet(clDirInfo Dir)
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
            bool
                ValidConcertDate = false;

            // get parent directory cuesheet filename
            CuesheetFileName = SplitFileName(Dir.CuesheetPath);
            if (File.Exists(Dir.CuesheetPath))
            {
                Log.WriteLine("  Reading album metadata from cuesheet: " + CuesheetFileName);
                // read data from text file
                DataList = ReadTextFile(Dir.CuesheetPath);

                // search for common metadata
                Dir.AlbumArtist = GetDataAfterSearchTerm("PERFORMER", DataList);
                Dir.Album = GetDataAfterSearchTerm("TITLE", DataList);

                switch (Dir.RecordingType)
                {
                    case LIVE:
                    {
                        // search for live metadata
                        Dir.Event = GetDataAfterSearchTerm("EVENT", DataList);
                        Dir.Venue = GetDataAfterSearchTerm("VENUE", DataList);
                        Dir.Stage = GetDataAfterSearchTerm("STAGE", DataList);
                        Dir.Location = GetDataAfterSearchTerm("LOCATION", DataList);
                        Dir.ConcertDate = GetDataAfterSearchTerm("DATE", DataList);
                        ValidConcertDate = ValidateDate(Dir.ConcertDate);
                        // if album string was not found, build it
                        if (Dir.Album == null)
                        {
                            Dir.Album = ( Dir.Event + SPACE
                                        + Dir.Venue + SPACE
                                        + Dir.Stage + SPACE
                                        + Dir.Location + SPACE
                                        + Dir.ConcertDate);
                            // remove redundant multiple spaces
                            Dir.Album = Regex.Replace(Dir.Album, @"\s+", SPACE);
                        }
                        // verify minimum metadata has been found
                        if (Dir.AlbumArtist == null)
                            Log.WriteLine("*** Artist missing from cuesheet");
                        if (!ValidConcertDate)
                            Log.WriteLine("*** Concert date is missing/incorrect format from cuesheet");
                        // if metadata is valid, reset metadata source
                        if (Dir.AlbumArtist != null && ValidConcertDate)
                            Dir.DirMetadataSource = CUESHEET;
                        break;
                    }
                    case COMMERCIAL:
                    case OTHER:
                    {
                        // verify minimum metadata has been found
                        if (Dir.AlbumArtist == null)
                            Log.WriteLine("*** Artist missing from cuesheet");
                        if (Dir.Album == null)
                            Log.WriteLine("*** Album information missing from cuesheet");
                        // if metadata is valid, reset metadata source
                        if (Dir.AlbumArtist != null && Dir.Album != null)
                            Dir.DirMetadataSource = CUESHEET;
                        break;
                    }
                }
            }
            else
                Log.WriteLine("*** Cuesheet not found");

        } // end GetDirMetadataFromCuesheet

        static void GetDirMetadataFromDirectoryName(clDirInfo Dir)
        {
            /* Get directory metadata from directory name
             * Inputs:
             *   Dir   Directory as clDirInfo class instance
             *     Dir.BaseNameTemp1
             *     Dir.BaseNameTemp2
             *     Dir.BaseNameTemp3
             *     Dir.RecordingType
             * Outputs:
             *   Dir   Directory as clDirInfo class instance
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
                    // build album name
                    Dir.Album = Dir.AlbumArtist;
                    // concatenate concert date if it exists
                    // basenametemp2 has been validated as the correct date format
                    Dir.ConcertDate = Dir.BaseNameTemp2;
                    if (Dir.ConcertDate != String.Empty)
                        Dir.Album += SPACE + Dir.ConcertDate;
                    // concatenate stage if it exists
                    Dir.Stage = Dir.BaseNameTemp3;
                    if (Dir.BaseNameTemp3 != String.Empty)
                        Dir.Album += SPACE + Dir.Stage;
                    break;
                }
                case COMMERCIAL:
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
        } // end GetDirMetadataFromDirectoryName
    }
}