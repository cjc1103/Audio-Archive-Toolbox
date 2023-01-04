using System;

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
                TrackDurationSec,
                M3UOutputString1,
                M3UOutputString2;

            Log.WriteLine("    Creating M3U Playlist");
            if (CreateFile(M3UFilePath))
            {
                File.AppendAllText(M3UFilePath, "#EXTM3U" + Environment.NewLine);
                foreach (FileInfo fi in FileList)
                {
                    // increment track number
                    TrackNumber++;
                    // get track duration in milliseconds
                    TrackDuration = Dir.TrackDurationList[TrackNumber - 1];
                    try
                    {
                        // convert to seconds
                        decTrackDurationSec = Convert.ToDecimal(TrackDuration) / 1000;
                        intTrackDurationSec = Convert.ToInt32(Decimal.Round(decTrackDurationSec));
                        TrackDurationSec = Convert.ToString(intTrackDurationSec);
                    }
                    catch (Exception e)
                    {
                        TrackDurationSec = "0";
                        // flush output buffer, write exception message and exit
                        Log.WriteLine("*** Track duration conversion error in track " + TrackNumber);
                        Log.WriteLine(e.Message);
                    }
                    // first line
                    M3UOutputString1 = ("#EXTINF:"
                                     + TrackDurationSec + ","
                                     + Dir.AlbumArtist + " - "
                                     + Dir.TitleList[TrackNumber - 1]
                                     + Environment.NewLine);
                    if (Debug) Console.Write("dbg: " + M3UOutputString1);
                    File.AppendAllText(M3UFilePath, M3UOutputString1);
                    // second line
                    M3UOutputString2 = (fi.Name
                                     + Environment.NewLine);
                    if (Debug) Console.Write("dbg: " + M3UOutputString2);
                    File.AppendAllText(M3UFilePath, M3UOutputString2);
                }
            }
        } // end CreateM3UPlaylist
    }
}