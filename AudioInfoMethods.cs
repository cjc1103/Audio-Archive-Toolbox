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

        static void CreateCuesheetFile(AATB_DirInfo Dir)
        {
            /* Creates the CD cuesheet from input directory data structure, which is
             * populated in GetTrackMetadata
             * Inputs:
             *   Dir          Directory as AATB_DirInfo class instance
             *     Dir.TitleList    List of track titles
             *     Dir.ArtistList   List of artists corresponding to each track
             *   CuesheetPath   Path to cuesheet
             * Outputs:
             *   Cuesheet in the current directory
             * Cuesheet format:
             * REM Comment
             * FILE "WAV file name"
             * TRACK dd AUDIO
             *    PERFORMER
             *    TITLE
             *    INDEX 01 MM:SS:FF
             * Note: MM is minutes, SS is seconds, and FF is frames (75 frames = 1 second)
             */
            int
                TrackNumber,
                TrackCount,
                intTrackDurationMin,
                intTrackDurationSec,
                intCumTrackDurationSecFloor,
                intTrackDurationFrames;
            decimal
                decTrackDurationSec,
                decCumTrackDurationSec,
                decCumTrackDurationRemSec,
                decCumTrackDurationSecFloor;
            string
                CuesheetFileName,
                WAVFileName,
                TrackDuration,
                strTrackDuration;

            // build cuesheet filename
            CuesheetFileName = Dir.ParentBaseName + PERIOD + INFOCUE;
            Dir.CuesheetPath = Dir.ParentPath + BACKSLASH + CuesheetFileName;

            if (!File.Exists(Dir.CuesheetPath)
                || (File.Exists(Dir.CuesheetPath) && Overwrite))
            {
                // check the number of Tracks > 0
                TrackCount = Dir.TitleList.Count;
                if (Debug) Console.WriteLine("Track Count: {0}", TrackCount);

                if (TrackCount > 0)
                {
                    Log.WriteLine("  Creating cuesheet: " + CuesheetFileName);
                    if (CreateFile(Dir.CuesheetPath))
                    {
                        WAVFileName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + WAV;
                        // write header info to cuesheet
                        File.AppendAllText(Dir.CuesheetPath,
                            "REM Created by Audio Archive Toolbox" + Environment.NewLine);
                        // write wav filename. This is solely for convention
                        File.AppendAllText(Dir.CuesheetPath,
                            "FILE " + DBLQ + WAVFileName + DBLQ + " WAV" + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "PERFORMER " + DBLQ + Dir.AlbumArtist + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "TITLE " + DBLQ + Dir.Album + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "EVENT " + DBLQ + Dir.Event + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "VENUE " + DBLQ + Dir.Venue + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "STAGE " + DBLQ + Dir.Stage + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "LOCATION " + DBLQ + Dir.Location + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.CuesheetPath,
                            "DATE " + DBLQ + Dir.ConcertDate + DBLQ + Environment.NewLine);

                        // initialize cumulative track duration seconds
                        decCumTrackDurationSec = 0;

                        // create entries for each track
                        // first iteration duration is 0 seconds, then track duration is read for next iteration
                        for (TrackNumber = 1; TrackNumber <= TrackCount; TrackNumber++)
                        {
                            // write track, title, and artist info to cuesheet
                            File.AppendAllText(Dir.CuesheetPath,
                                "  TRACK " + String.Format("{0:D2}", TrackNumber) + " AUDIO" + Environment.NewLine);
                            File.AppendAllText(Dir.CuesheetPath,
                                "    TITLE " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ + Environment.NewLine);
                            File.AppendAllText(Dir.CuesheetPath,
                                "    PERFORMER " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ + Environment.NewLine);

                            // get cumulative seconds floor
                            decCumTrackDurationSecFloor = Decimal.Floor(decCumTrackDurationSec);
                            intCumTrackDurationSecFloor = Convert.ToInt32(decCumTrackDurationSecFloor);

                            // get minutes and seconds
                            intTrackDurationSec = intCumTrackDurationSecFloor - (intCumTrackDurationSecFloor % 60);
                            intTrackDurationMin = intTrackDurationSec / 60;
                            intTrackDurationSec = intCumTrackDurationSecFloor - intTrackDurationSec;

                            // get decimal remainder in seconds and convert to frames
                            decCumTrackDurationRemSec = decCumTrackDurationSec - decCumTrackDurationSecFloor;
                            intTrackDurationFrames = Convert.ToInt32(Decimal.Round(decCumTrackDurationRemSec * 75));

                            // build index string and write to cuesheet
                            strTrackDuration = String.Format("{0:D2}", intTrackDurationMin) + COLON
                                             + String.Format("{0:D2}", intTrackDurationSec) + COLON
                                             + String.Format("{0:D2}", intTrackDurationFrames);
                            File.AppendAllText(Dir.CuesheetPath, "    INDEX 01 " + strTrackDuration + Environment.NewLine);

                            // read previous track duration entry in msec, convert to decimal seconds
                            TrackDuration = Dir.TrackDurationList[TrackNumber - 1];
                            decTrackDurationSec = Convert.ToDecimal(TrackDuration) / 1000;

                            // update cumulative duration seconds for next loop iteration
                            decCumTrackDurationSec += decTrackDurationSec;
                        }
                    }
                }
                else
                    Log.WriteLine("*** Track metadata cannot be found, cuesheet not created");
            }
            else
                Log.WriteLine("*** Cuesheet exists, use overwrite to replace: " + CuesheetFileName);

        } // end CreateCuesheetFile
    }
}