using System;
using System.IO;

namespace AATB
{
    public partial class AATB_Main
    {
        static void CreateM3UPlaylist(AATB_DirInfo Dir, string M3UFilePath, FileInfo[] FileList)
        {
            /* Build M3UPlaylist from track information in FileList
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             *     Dir.AlbumArtist
             *     Dir.TitleList
             *   M3UFilePath  M3U file path
             *   FileList     List of all files to inlude in playlist (various formats)
             * Outputs:
             *   M3U Playlist file
             *   Format
             *     #EXTM3U
             *     #EXTINF:secs,Artist - Title
             *     Track Filename
             */
            int
                TrackNumber = 0,
                intTrackDurationSec;
            decimal
                decTrackDurationSec;
            string
                TrackDuration,
                TrackDurationSec;

            Log.WriteLine("    Creating M3U Playlist");
            if (CreateFile(M3UFilePath))
            {
                File.AppendAllText(M3UFilePath, "#EXTM3U" + Environment.NewLine);
                foreach (FileInfo fi in FileList)
                {
                    // increment track number
                    TrackNumber++;
                    // get track duration in milliseconds
                    TrackDuration = GetTrackDuration(fi.FullName);
                    // convert to seconds
                    decTrackDurationSec = (decimal)Convert.ToDouble(TrackDuration) / 1000;
                    intTrackDurationSec = (int)Decimal.Round(decTrackDurationSec);
                    TrackDurationSec = Convert.ToString(intTrackDurationSec);
                    File.AppendAllText(M3UFilePath,
                                       "#EXTINF:"
                                       + TrackDuration + ","
                                       + Dir.AlbumArtist + " - "
                                       + Dir.TitleList[TrackNumber - 1]
                                       + Environment.NewLine);
                    File.AppendAllText(M3UFilePath,
                                       fi.Name
                                       + Environment.NewLine);
                }
            }
        } // end CreateM3UPlaylist
    }
}