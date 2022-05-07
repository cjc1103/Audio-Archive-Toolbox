using System;
using System.IO;

namespace AATB
{
    public partial class AATB_Main
    {
        static FileInfo[] CompressWAV(String CompType, AATB_DirInfo Dir, string CompDirPath, FileInfo[] WAVFileList)
        {
            /* Converts all WAV files in input file list to Freeware Lossless Audio Codec format
             * Inputs:
             *   CompType     String representing what audio compression codec is to be used
             *   Dir          Directory as AATB_DirInfo class instance
             *   CompDirPath  Output directory
             *   WAVFileList  List of all WAV files to be converted
             * Calls external programs, defined in code
             * Outputs:
             *   compressed audio files are written to the output directory
             * Returns:
             *   list of compressed audio files
             */
            FileInfo[]
                CompFileList = new FileInfo[WAVFileList.Length];
            string
                TrackNumberStr,
                BaseFileName,
                Extension,
                WAVFileName,
                WAVFilePath,
                CompFileName,
                CompFilePath,
                ExternalProgram,
                ExternalArguments,
                ExternalOutput;
            int
                TrackNumber = 0,
                CompTypeIndex,
                QualityValue;

            Log.Write("    Track ");
            foreach (FileInfo fi in WAVFileList)
            {
                // increment track number and convert to two place string
                TrackNumber++;
                TrackNumberStr = TrackNumber.ToString("00");
                Log.Write(TrackNumberStr + "..");
                // build filenames
                WAVFileName = fi.Name;
                WAVFilePath = fi.FullName;
                (BaseFileName, Extension) = SplitString(WAVFileName, PERIOD);
                CompFileName = BaseFileName + PERIOD + CompType;
                CompFilePath = CompDirPath + BACKSLASH + CompFileName;
                // convert FileName to FileInfo type and build FLACFileList
                CompFileList[TrackNumber - 1] = new FileInfo(CompFilePath);
                CompTypeIndex = Array.IndexOf(AudioFormats, CompType);
                QualityValue = CompressedAudioQuality[CompTypeIndex][ACTIVE];
                ExternalProgram = ExternalArguments = null;

                switch (CompType)
                {
                    case MP3:
                    {
                        ExternalProgram = "lame.exe";
                        ExternalArguments = "-V" + QualityValue
                                          + " --add-id3v2"
                                          + " --tt " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --ta " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --tl " + DBLQ + Dir.Album + DBLQ
                                          + " --ty " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --tn " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case M4A:
                    {
                        ExternalProgram = "qaac64.exe";
                        ExternalArguments = "--threading"
                                          + " --tvbr " + QualityValue
                                          + " --title " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album " + DBLQ + Dir.Album + DBLQ
                                          + " --date " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --track " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                            break;
                    }
                    case OGG:
                    {
                        ExternalProgram = "oggenc2.exe";
                        ExternalArguments = "-q " + QualityValue
                                          + " --title=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album=" + DBLQ + Dir.Album + DBLQ
                                          + " --date=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --tracknum=" + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case OPUS:
                    {
                        ExternalProgram = "opusenc.exe";
                        ExternalArguments = "--vbr"
                                          + " --bitrate " + QualityValue
                                          + " --comp 10"
                                          + " --title=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album=" + DBLQ + Dir.Album + DBLQ
                                          + " --date=" + DBLQ + Dir.ConcertDate + DBLQ
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + SPACE + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case ALAC:
                    {
                        ExternalProgram = "qaac64.exe";
                        ExternalArguments = "--alac"
                                          + " --threading"
                                          + " --title " + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                          + " --artist " + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                          + " --album " + DBLQ + Dir.Album + DBLQ
                                          + " --date " + DBLQ + Dir.ConcertDate + DBLQ
                                          + " --track " + TrackNumberStr
                                          + SPACE + DBLQ + WAVFilePath + DBLQ
                                          + " -o " + DBLQ + CompFilePath + DBLQ;
                        break;
                    }
                    case FLAC:
                    {
                        ExternalProgram = "flac.exe";
                        if (Dir.Type == WAVAUDIO)
                        {
                            ExternalArguments = "-" + QualityValue
                                                + " --force"
                                                + " --verify"
                                                + " --tag=TITLE=" + DBLQ + Dir.TitleList[TrackNumber - 1] + DBLQ
                                                + " --tag=ARTIST=" + DBLQ + Dir.ArtistList[TrackNumber - 1] + DBLQ
                                                + " --tag=ALBUM=" + DBLQ + Dir.Album + DBLQ
                                                + " --tag=DATE=" + DBLQ + Dir.ConcertDate + DBLQ
                                                + " --tag=TRACKNUMBER=" + TrackNumberStr
                                                + SPACE + DBLQ + WAVFilePath + DBLQ
                                                + " --output-name " + DBLQ + CompFilePath + DBLQ;
                        }
                        else if (Dir.Type == RAWAUDIO)
                        {
                            ExternalArguments = "-" + QualityValue
                                                + " --force"
                                                + " --verify"
                                                + SPACE + DBLQ + WAVFilePath + DBLQ
                                                + " --output-name " + DBLQ + CompFilePath + DBLQ;
                        }
                        break;
                    }
                }

                // run external program
                ExternalOutput = RunProcess(ExternalProgram, ExternalArguments);
            }
            Log.WriteLine();
            return CompFileList;
        } // end CompressWAV
    }
}