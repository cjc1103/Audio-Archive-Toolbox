﻿using System;
using System.Text.RegularExpressions;

namespace AATB
{
    public class AATB_DirInfo
    {
        public string
            Name,
            Path,
            ParentName,
            ParentPath,
            BaseNameTemp1,
            BaseNameTemp2,
            BaseNameTemp3,
            BaseName,
            Extension,
            ParentBaseName,
            Bitrate,
            AudioCompressionFormat,
            Type,
            RecordingType,
            AlbumArtist,
            Album,
            Event,
            Venue,
            Location,
            Stage,
            ConcertDate,
            Comment,
            MetadataSource,
            ParentInfotextPath,
            ParentCuesheetPath;
        public bool
            MultipleMetadataSources;
        public Match
            PatternMatchDate,
            PatternMatchSHS;
        public List<string>
            TitleList,
            ArtistList,
            TrackDurationList;

        public AATB_DirInfo(DirectoryInfo Directory)
        {
            /* Constructor for AATB_DirInfo class
             * Creates data structures and populates directory names
             * Other class variables are populated as the directories are
             * traversed from the WalkDirectoryTree method
             */
            Name = Directory.Name;
            Path = Directory.FullName;
            ParentName = Directory.Parent.Name;
            ParentPath = Directory.Parent.FullName;
            BaseName = null;
            Extension = null;
            ParentBaseName = null;
            Bitrate = null;
            AudioCompressionFormat = null;
            Type = null;
            RecordingType = null;
            AlbumArtist = null;
            Album = null;
            Event = null;
            Venue = null;
            Location = null;
            Stage = null;
            ConcertDate = null;
            Comment = null;
            MetadataSource = null;
            ParentInfotextPath = null;
            ParentCuesheetPath = null;
            TitleList = new List<string>();
            ArtistList = new List<string>();
            TrackDurationList = new List<string>();
        } // end constructor AATB_DirInfo

    } // end class AATB_DirInfo
}
