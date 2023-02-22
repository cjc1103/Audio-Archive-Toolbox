using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void PrintHelp()
        {
            Console.WriteLine
            ("Primary options - mutually exclusive\n"
            + "[-c|--compress]\n"
            + "  [-i|--use-infotext] | [-e|--use-cuesheet]\n"
            + "  [-p|--m3u]\n"
            + "  --mp3[=<bitrate>](16-44)\n"
            + "  --[aac|m4a]=[<bitrate>|all](all)\n"
            + "  --ogg[=<bitrate>|all](all)\n"
            + "  --opus[=<bitrate>|all](all)\n"
            + "  --alac[=<bitrate>|all](all)\n"
            + "  --flac[=<bitrate>|all](all)\n"
            + "  --mp3-quality=<q>\n"
            + "  --aac-quality=<q>\n"
            + "  --ogg-quality=<q>\n"
            + "  --flac-quality=<q>\n"
            + "[-v|--verify]\n"
            + "  [-i|--use-infotext] | [-e|--use-cuesheet]\n"
            + "  [-p|--m3u]\n"
            + "  --mp3[=<bitrate>](16-44)\n"
            + "  --[aac|m4a]=[<bitrate>|all](all)\n"
            + "  --ogg[=<bitrate>|all](all)\n"
            + "  --opus[=<bitrate>|all](all)\n"
            + "  --alac[=<bitrate>|all](all)\n"
            + "  --flac[=<bitrate>|all](all)\n"
            + "  [--md5, --ffp, --shn]|[-a|--all]\n"
            + "  [-t|--tag]\n"
            + "[-d|--decompress]\n"
            + "  --flac[=<bitrate>|all](all)\n"
            + "[-j|--join]\n"
            + "  --wav=<bitrate>\n"
            + "[-x|--delete]\n"
            + "  --wav[=<bitrate>|raw|all](all)\n"
            + "[-y|--convert-aif-to-wav]\n"
            + "[-z|--convert-to-bitrate]=<bitrate>(16-44)\n"
            + "  --wav=<convert from bitrate>\n"
            + "[-s|--create-cuesheet]\n"
            + "  [-i|--use-infotext]\n"
            + "  --wav[=<bitrate>|all](all)\n"
            + "Additional options\n"
            + "[-lc|--lower-case]\n"
            + "[-tc|--title-case]\n"
            + "[-ri|--rename-infofiles]"
            + "[-cd|--use-currentdirinfo]\n"
            + "[-o|--overwrite]\n"
            + "[-h|--help]\n"
            + "[-hh|--verbose]\n"
            + "Notes:\n"
            + "  o Valid compression types are mp3, aac, ogg, opus, alac, and flac\n"
            + "  o Bitrates are in the format <bitdepth-samplerate>\n"
            + "    Valid bitrates are 16-44, 16-48, 24-44, 24-48, 24-88, and 24-96\n"
            + "  o Tracked wav files are in <bitrate> subdirectory\n"
            + "  o Raw (unedited or tracked) audio files are in <" + RAW + "> subdirectory\n"
            + "  o FLAC is only compression type supported for raw files\n"
            + "  o AAC compressed files are saved in MPEG-4 Audio (.m4a) format\n"
            + "  o If compression bitrates are not specified, all bitrates are assumed\n"
            + "  o Metadata in info.txt and info.cue files can be used to tag compressed audio\n"
            );
        } // end PrintHelp

        static void PrintFileList(string FileType, FileInfo[] FileList)
        {
            /* print contents of FileList for debugging purposes
             */
            Console.WriteLine("dbg: File dump type: {0}", FileType);
            for (int i = 0; i < FileList.Length; i++)
                Console.WriteLine("dbg: FileList Name {0}", FileList[i].Name);
        } //end PrintFileList
    }
}