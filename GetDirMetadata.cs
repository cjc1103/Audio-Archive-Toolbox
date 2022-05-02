using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AATB
{
    public partial class AATB_Main
    {
        static void GetDirInformation(AATB_DirInfo Dir)
        {
            /* populate basic directory information
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             * Outputs:
             *   Dir.BaseName
             *   Dir.Extension
             *   Dir.Type
             *   Dir.AudioCompressionFormat
             *   Dir.Bitrate
             */
            int i;

            if (Debug) Console.WriteLine("dbg: GetDirInformation method");

            // get directory basename and extension
            (Dir.BaseName, Dir.Extension) = SplitString(Dir.Name, PERIOD);

            // determine type of directory
            if (Dir.Name == RAW)
                Dir.Type = RAWAUDIO;
            else if (AudioBitrates.Contains(Dir.Name))
                Dir.Type = WAVAUDIO;
            else if (CompressedDirExtensions.Contains(Dir.Extension))
                Dir.Type = COMPAUDIO;
            else
                Dir.Type = OTHER;
            if (Debug) Console.WriteLine("dbg: Dir type = " + Dir.Type);

            // Get audio compression format from directory extension
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
            // Get audio bitrate (if exists) from directory name
            if (Dir.BaseName != null)
                for (i = 0; i <= AudioBitrates.Length - 1; i++)
                {
                    if (Dir.BaseName.Contains(AudioBitrates[i]))
                    {
                        Dir.Bitrate = AudioBitrates[i];
                        break;
                    }
                }
        } //end GetDirInformation

        static void GetDirMetadata(AATB_DirInfo Dir)
        {
            /* extract directory metadata
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             *   InfotextPath location of info.txt file with recording information
             *   CuesheetPath location of cuesheet file
             * Outputs:
             *   Dir.RecordingType
             *   Dir.ParentBaseName
             *   Dir.AlbumArtist
             *   Dir.ConcertDate
             *   Dir.Album
             *   Dir.Stage
             *   
             * Directory name formats
             *   Live recording: <artist> <date>, <artist>-<date>, <artist>_<date>, <artist><date>
             *     datestring is in format: yyyy-mm-dd
             *   Commercial recording: <artist> - <album>
             *   Other: all other formats
             */
            string TempBaseName;

            if (Debug) Console.WriteLine("dbg: GetDirMetadata method");

            // parent base name will be used to create compressed audio subdirectories
            // remove prefix number if it exists
            // (from beginning of line: one or more digits, optional period, one or more spaces)
            TempBaseName = Regex.Replace(Dir.ParentName, @"^\d+\.?\s+", "");

            // check for live recording format: embedded date "yyyy-mm-dd stage"
            Dir.PatternMatchDate = Regex.Match(TempBaseName, @"[1-2]\d{3}-\d{2}-\d{2}");
            // check for commercial CD format: embedded " - "
            Dir.PatternMatchSHS = Regex.Match(TempBaseName, @SPACEHYPHENSPACE);

            // first derive metadata from parent directory name
            // infotext or cuesheet information may be used later to overwrite this
            // metadata later, if the file is present and the information is correct
            // initialize metadata source to parent directory name
            Dir.MetadataSource = DIRNAME;

            // live recording format
            if (Dir.PatternMatchDate.Success)
            {
                Dir.RecordingType = LIVE;
                // extract album artist
                Dir.AlbumArtist = TempBaseName.Substring(0, Dir.PatternMatchDate.Index);
                // remove leading index number, if it exists ^ddd<sp>
                Dir.AlbumArtist = Regex.Replace(Dir.AlbumArtist, @"^\d+\s", "");
                // remove trailing non-word [A-Z][a-z][0-9] character including whitespace
                Dir.AlbumArtist = Regex.Replace(Dir.AlbumArtist, @"[^A-Za-z0-9]$", "");
                // convert to title case/lower case if appropriate
                Dir.AlbumArtist = ConvertCase(Dir.AlbumArtist);
                // extract concert date, 10 chars long (yyyy-mm-dd)
                Dir.ConcertDate = TempBaseName.Substring(Dir.PatternMatchDate.Index, 10);
                // build album name
                Dir.Album = Dir.AlbumArtist + SPACE + Dir.ConcertDate;
                // build parent basename = <artist>_<date>.<stage>
                Dir.ParentBaseName = Dir.AlbumArtist + UNDERSCORE + Dir.ConcertDate;
                // extract stage name if it exists (skip char 11 delimiter between date and stage name)
                if (TempBaseName.Length > Dir.PatternMatchDate.Index + 11)
                {
                    Dir.Stage = TempBaseName.Substring(Dir.PatternMatchDate.Index + 11);
                    // convert to title case/lower case if appropriate
                    Dir.Stage = ConvertCase(Dir.Stage);
                    // concatentate to album and parent base name
                    Dir.Album += (SPACE + Dir.Stage);
                    Dir.ParentBaseName += (PERIOD + Dir.Stage);
                }
                // remove any remaining embedded spaces in basename
                Dir.ParentBaseName = Regex.Replace(Dir.ParentBaseName, @"\s", "");

                // build album string
                Dir.Album = Dir.Event;
                if (Dir.Venue != null)
                    Dir.Album += (SPACE + Dir.Venue);
                if (Dir.ConcertDate != null)
                    Dir.Album += (SPACE + Dir.ConcertDate);
                if (Dir.Stage != null)
                    Dir.Album += (SPACE + Dir.Stage);
            }

            // commercial recording format
            else if (Dir.PatternMatchSHS.Success)
            {
                Dir.RecordingType = CD;
                // extract artist and album from dir name
                Dir.AlbumArtist = TempBaseName.Substring(0, Dir.PatternMatchSHS.Index);
                Dir.Album = TempBaseName.Substring(Dir.PatternMatchSHS.Index + 3);
                // no further operations needed to build parent base name
                Dir.ParentBaseName = TempBaseName;
            }

            // other recording format
            else
            {
                Dir.RecordingType = OTHER;
                Dir.AlbumArtist = TempBaseName;
                Dir.Album = TempBaseName;
                Dir.ParentBaseName = TempBaseName;
                // no further metadata or parent base name required
            }
            if (Debug) Console.WriteLine("dbg: ParentBasename = " + Dir.ParentBaseName);

            // for wav or compressed audio directories only
            // if infotext or cuesheet flags are set, and the infotext or cuesheet files exist
            // metadata from these sources will overwrite existing directory metadata
            // but will only be used for identifying tracks and not modify parent basename
            // if metadata source file is valid, metadata source is reset appropriately
            if ( (CompressAudio && Dir.Type == WAVAUDIO)
                || (VerifyAudio && Dir.Type == COMPAUDIO)
                && (Dir.Name != RAW))
            {
                if (UseInfotext)
                    GetDirMetadataFromInfotext(Dir);

                else if (UseCuesheet)
                    GetDirMetadataFromCuesheet(Dir);

                // if Dir.MetadataSource has not changed, the metadata source reverts to directory name
                if (Dir.MetadataSource == DIRNAME)
                    Log.WriteLine("  Deriving album metadata from directory name");

                // remove any leading space
                Dir.Album = Regex.Replace(Dir.Album, @"^\s", "");

                // log metadata info
                Log.WriteLine("    Artist: " + Dir.AlbumArtist);
                Log.WriteLine("    Album: " + Dir.Album);
                // the following metadata is extracted from info file or cuesheet
                if (Dir.RecordingType == LIVE)
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
            /* Extract directory metadata from Info File
             * Note: will overwrite existing metadata derived from directory name
             * Inputs:
             *   Dir class
             *   InfotextPath   location of info.txt file with recording information
             * Outputs:
             *   Dir.AlbumArtist
             *   Dir.Album
             *   Dir.Event
             *   Dir.Venue 
             *   Dir.Stage
             *   Dir.Location
             *   Dir.ConcertDate
             */
            string
                InfotextFilePath,
                InfotextFileName;
            string[]
                DataList;
            Match
                DateMatchLine4,
                DateMatchLine5;

            if (Debug) Console.WriteLine("dbg: GetDirMetadataFromInfotext method");
            (InfotextFilePath, InfotextFileName) = SplitPath(Dir.ParentInfotextPath);

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
                    // otherwise search for metadata labels
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

                // AlbumArtist and ConcertDate are minimum required to document concert
                if (Dir.AlbumArtist != null && Dir.ConcertDate != null)
                    Dir.MetadataSource = INFOFILE;
                else
                    Log.WriteLine("*** Artist and concert date missing from info file");
            }
            else
                Log.WriteLine("*** Info file not found: " + InfotextFileName);
        } // end GetDirMetadataFromInfotext

        static void GetDirMetadataFromCuesheet(AATB_DirInfo Dir)
        {
            /* Extract directory metadata from Cuesheet
             * Note: will overwrite existing metadata derived from directory name
             * Inputs:
             *   Dir class
             *   CuesheetPath   location of .cue file with recording information
             * Outputs:
             *   Dir.AlbumArtist
             *   Dir.Album
             *   Dir.Event
             *   Dir.Venue 
             *   Dir.Stage
             *   Dir.Location
             *   Dir.ConcertDate
             */
            string
                CuesheetFilePath,
                CuesheetFileName;
            string[]
                DataList;

            if (Debug) Console.WriteLine("dbg: GetDirMetadataFromCuesheet method");
            // get parent directory cueshee filepath
            (CuesheetFilePath, CuesheetFileName) = SplitPath(Dir.ParentCuesheetPath);

            if (File.Exists(Dir.ParentCuesheetPath))
            {
                Log.WriteLine("  Reading album metadata from cuesheet: " + CuesheetFileName);

                // read data from text file
                DataList = ReadTextFile(Dir.ParentCuesheetPath);

                // search for metadata labels
                Dir.AlbumArtist = SearchList(DataList, "PERFORMER ");
                Dir.Album = SearchList(DataList, "TITLE ");
                Dir.Event = SearchList(DataList, "EVENT ");
                Dir.Venue = SearchList(DataList, "VENUE ");
                Dir.Stage = SearchList(DataList, "STAGE ");
                Dir.Location = SearchList(DataList, "LOCATION ");
                Dir.ConcertDate = SearchList(DataList, "DATE ");

                // AlbumArtist and ConcertDate are minimum required to document concert
                if (Dir.AlbumArtist != null && Dir.ConcertDate != null)
                    Dir.MetadataSource = CUESHEET;
                else
                    Log.WriteLine("*** Artist and concert date missing from cuesheet");
            }
            else
                Log.WriteLine("*** Cuesheet not found: " + CuesheetFileName);
        } // end GetDirMetadataFromCuesheet

        static string SearchList(string[] DataList, string Name)
        {
            /* Inputs:
             *   DataList   list containing data
             *   Name       string search term, e.g: "Artist: "
             * Outputs:
             *   Data       string found by pattern match, null if not found
             */
            int i;
            string
                Data = null;
            Match
                PatternMatch;

            for (i = 0; i < DataList.Length; i++)
            {
                // search for pattern at beginning of string
                PatternMatch = Regex.Match(DataList[i], @"^"+Name);
                if ((PatternMatch.Success)
                    && (DataList[i].Length > Name.Length))
                {
                    //Data = DataList[i].Substring(PatternMatch.Index + Name.Length);
                    Data = DataList[i].Substring(Name.Length);
                    // remove quotation marks, if they exist
                    Data = Regex.Replace(Data, @"""", "");
                    // exit loop, only first match in list will be used
                    break;
                }
            }
            return Data;
        } // end SearchList
    }
}