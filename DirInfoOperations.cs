using System;
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
             * Other class variables are initially null
             */
            Name = Directory.Name;
            Path = Directory.FullName;
            ParentName = Directory.Parent.Name;
            ParentPath = Directory.Parent.FullName;
            TitleList = new List<string>();
            ArtistList = new List<string>();
            TrackDurationList = new List<string>();
        } // end constructor AATB_DirInfo

    } // end class AATB_DirInfo
}
