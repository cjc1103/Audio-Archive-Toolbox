using System;
using System.IO;

namespace AATB
{
    public partial class AATB_Main
    {
        static void TagCompressedAudio(string CompType, AATB_DirInfo Dir, FileInfo[] CompFileList)
        {
            /* Creates metadata tags for the files in the input list
             * Inputs:
             *   CompType     String representing what audio compression codec is to be used
             *   Dir          Input directory as AATB_DirInfo class
             *   CompFileList List of all files to be modified
             * Calls external programs, defined in code
             * Outputs:
             *   writes the appropriate tags to flac files in the list
             */
            string
                TrackNumberStr,
                CompFilePath,
                ExternalProgram = null,
                ExternalArguments = null,
                ExternalOutput;
            int
                TrackNumber = 0;

            Log.WriteLine("    Creating tags");
            foreach (FileInfo fi in CompFileList)
            {
                // increment track number and convert to two place string
                TrackNumber++;
                TrackNumberStr = TrackNumber.ToString("00");
                // build filenames
                CompFilePath = fi.FullName;

                switch (CompType)
                {
                    case MP3:
                    {
                        ExternalProgram = "id3.exe";
                        ExternalArguments = "-d"
                                          + " -M"
                                          + " -t " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " -a " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " -l " + DBLQ + Dir.Album + DBLQ
                                          + " -y " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " -n " + TrackNumberStr
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case M4A:
                    {
                        ExternalProgram = "NeroAacTag.exe";
                        ExternalArguments = "-meta:title=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " -meta:artist=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " -meta:album=" + DBLQ + Dir.Album + DBLQ
                                          + " -meta:year=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + " -meta:track=" + TrackNumberStr
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case OGG:
                    {
                        ExternalProgram = "vorbiscomment.exe";
                        ExternalArguments = "--write " + DBLQ + CompFilePath + DBLQ
                                          + " -t TITLE=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " -t ARTIST=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " -t ALBUM=" + DBLQ + Dir.Album + DBLQ
                                          + " -t DATE=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + " -t TRACKNUMBER=" + TrackNumberStr;
                        break;
                    }
                    case OPUS:
                    {
                        // placeholder for future tagging, feature not yet available
                        break;
                    }
                    case ALAC:
                    {
                        // placeholder for future tagging, feature not yet available
                        break;
                    }
                    case FLAC:
                    {
                        ExternalProgram = "metaflac.exe";
                        // remove existing tags
                        ExternalArguments = "--preserve-modtime"
                                          + " --remove-all-tags"
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
                        // write new tags
                        ExternalArguments = "--preserve-modtime"
                                          + " --set-tag=TITLE=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --set-tag=ARTIST=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --set-tag=ALBUM=" + DBLQ + Dir.Album + DBLQ
                                          + " --set-tag=DATE=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --set-tag=TRACKNUMBER=" + TrackNumberStr
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                }

                // run external process
                ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
            }
        } // end TagCompressedAudio
    }
}