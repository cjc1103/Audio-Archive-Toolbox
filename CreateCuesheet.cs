using System;
using System.IO;

namespace AATB
{
    public partial class AATB_Main
    {
        static void CreateCuesheetFile(AATB_DirInfo Dir, string CuesheetPath)
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
                FilePath,
                CuesheetName,
                WAVFileName,
                strTrackDuration;

            // check the number of Tracks > 0
            TrackCount = Dir.TitleList.Count;
            (FilePath, CuesheetName) = SplitPath(CuesheetPath);
            if (TrackCount > 0)
            {
                Log.WriteLine("    Creating cuesheet: " + CuesheetName);
                if (CreateFile(CuesheetPath))
                {
                    WAVFileName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + WAV;
                    // write header info to cuesheet
                    File.AppendAllText(CuesheetPath,
                        "REM Created by Audio Archive Toolbox" + Environment.NewLine);
                    // write wav filename. This is solely for convention
                    File.AppendAllText(CuesheetPath,
                        "FILE " + DBLQ + WAVFileName + DBLQ + " WAVE" + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "PERFORMER " + DBLQ + Dir.AlbumArtist + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "TITLE " + DBLQ + Dir.Album + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "EVENT " + DBLQ + Dir.Event + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "VENUE " + DBLQ + Dir.Venue + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "STAGE " + DBLQ + Dir.Stage + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "LOCATION " + DBLQ + Dir.Location + DBLQ + Environment.NewLine);
                    File.AppendAllText(CuesheetPath,
                        "DATE " + DBLQ + Dir.ConcertDate + DBLQ + Environment.NewLine);

                    // initialize cumulative track duration seconds
                    decCumTrackDurationSec = 0;

                    // create entries for each track
                    for (TrackNumber = 1; TrackNumber <= TrackCount; TrackNumber++)
                    {
                        // write track, title, and artist info to cuesheet
                        File.AppendAllText(CuesheetPath,
                            "  TRACK " + String.Format("{0:D2}", TrackNumber) + " AUDIO" + Environment.NewLine);
                        File.AppendAllText(CuesheetPath,
                            "    TITLE " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ + Environment.NewLine);
                        File.AppendAllText(CuesheetPath,
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
                        File.AppendAllText(CuesheetPath, "    INDEX 01 " + strTrackDuration + Environment.NewLine);

                        // read next track duration entry in msec, convert to decimal seconds
                        decTrackDurationSec = Convert.ToDecimal(Dir.TrackDurationList[TrackNumber - 1]) / 1000;

                        // update cumulative duration seconds for next loop iteration
                        decCumTrackDurationSec += decTrackDurationSec;
                    }
                }
            }
            else
            {
                Log.WriteLine("*** Track metadata cannot be found, cuesheet not created");
            }
        } // end CreateCuesheetFile
    }
}