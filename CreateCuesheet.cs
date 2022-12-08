using System;

namespace AATB
{
    public partial class AATB_Main
    {
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
            Dir.ParentCuesheetPath = Dir.ParentPath + BACKSLASH + CuesheetFileName;

            if (!File.Exists(Dir.ParentCuesheetPath)
                || (File.Exists(Dir.ParentCuesheetPath) && Overwrite))
            {
                // check the number of Tracks > 0
                TrackCount = Dir.TitleList.Count;
                if (Debug) Console.WriteLine("Track Count: {0}", TrackCount);

                if (TrackCount > 0)
                {
                    Log.WriteLine("  Creating cuesheet: " + CuesheetFileName);
                    if (CreateFile(Dir.ParentCuesheetPath))
                    {
                        WAVFileName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + WAV;
                        // write header info to cuesheet
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "REM Created by Audio Archive Toolbox" + Environment.NewLine);
                        // write wav filename. This is solely for convention
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "FILE " + DBLQ + WAVFileName + DBLQ + " WAVE" + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "PERFORMER " + DBLQ + Dir.AlbumArtist + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "ALBUM " + DBLQ + Dir.Album + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "EVENT " + DBLQ + Dir.Event + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "VENUE " + DBLQ + Dir.Venue + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "STAGE " + DBLQ + Dir.Stage + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "LOCATION " + DBLQ + Dir.Location + DBLQ + Environment.NewLine);
                        File.AppendAllText(Dir.ParentCuesheetPath,
                            "DATE " + DBLQ + Dir.ConcertDate + DBLQ + Environment.NewLine);

                        // initialize cumulative track duration seconds
                        decCumTrackDurationSec = 0;

                        // create entries for each track
                        // first iteration duration is 0 seconds, then track duration is read for next iteration
                        for (TrackNumber = 1; TrackNumber <= TrackCount; TrackNumber++)
                        {
                            // write track, title, and artist info to cuesheet
                            File.AppendAllText(Dir.ParentCuesheetPath,
                                "  TRACK " + String.Format("{0:D2}", TrackNumber) + " AUDIO" + Environment.NewLine);
                            File.AppendAllText(Dir.ParentCuesheetPath,
                                "    TITLE " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ + Environment.NewLine);
                            File.AppendAllText(Dir.ParentCuesheetPath,
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
                            File.AppendAllText(Dir.ParentCuesheetPath, "    INDEX 01 " + strTrackDuration + Environment.NewLine);

                            // read previous track duration entry in msec, convert to decimal seconds
                            TrackDuration = Dir.TrackDurationList[TrackNumber - 1];
                            decTrackDurationSec = Convert.ToDecimal(TrackDuration) / 1000;

                            // update cumulative duration seconds for next loop iteration
                            decCumTrackDurationSec += decTrackDurationSec;
                        }
                    }
                }
                else
                {
                    Log.WriteLine("*** Track metadata cannot be found, cuesheet not created");
                }
            }
            else
            {
                Log.WriteLine("*** Cuesheet exists, use overwrite to replace: " + CuesheetFileName);
            }
        } // end CreateCuesheetFile
    }
}