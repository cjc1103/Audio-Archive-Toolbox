﻿using System;

namespace AATB
{
    public partial class AATB_Main
    {
        static void PrintHelp()
        {
            Console.WriteLine
            ( "Primary options - mutually exclusive\n"
            + "[-c|--compress]\n"
            + "  [-i|--use-infotext] | [-e|--use-cuesheet]\n"
            + "  [-p|--m3u-playlist]\n"
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
            + "  [-p|--m3u-playlist]\n"
            + "  --mp3[=<bitrate>](16-44)\n"
            + "  --[aac|m4a]=[<bitrate>|all](all)\n"
            + "  --ogg[=<bitrate>|all](all)\n"
            + "  --opus[=<bitrate>|all](all)\n"
            + "  --alac[=<bitrate>|all](all)\n"
            + "  --flac[=<bitrate>|all](all)\n"
            + "  [--md5, --ffp, --shnrpt]|[-a|--all-reports]\n"
            + "  [-t|--tag]\n"
            + "[-d|--decompress]\n"
            + "  --alac[=<bitrate>|all](all except raw)\n"
            + "  --flac[=<bitrate>|raw|all](all)\n"
            + "[-j|--join-wav-files]\n"
            + "  --wav=<bitrate>\n"
            + "[-r|rename-wav-files]\n"
            + "  --wav=<bitrate>\n"
            + "[-y|--convert-to-wav]\n"
            + "  --shn\n"
            + "  --aif\n"
            + "  --wma\n"
            + "[-z|--convert-to-bitrate]=<bitrate>(16-44)\n"
            + "  --wav=<convert from bitrate>\n"
            + "[-s|--create-cuesheet]\n"
            + "  [-i|--use-infotext]\n"
            + "  --wav[=<bitrate>|all](all)\n"
            + "[-x|--delete]\n"
            + "  --wav[=<bitrate>|raw|all](all)\n"
            + "  [--misc|--misc-files-delete]\n"
            + "Additional options\n"
            + "[--lc|--lower-case]\n"
            + "[--tc|--title-case]\n"
            + "[--ri|--rename-info-files]"
            + "[--icd|--get-info-from-current-dir]\n"
            + "[--ocd|--output-to-current-dir]\n"
            + "[-o|--overwrite]\n"
            + "[-h|--help]\n"
            + "[--hh|--verbose]\n"
            + "Notes:\n"
            + "  o Valid compression formats are mp3, aac, ogg, opus, alac, and flac\n"
            + "  o Valid decompression and conversion formats are shn, aif, wma, alac, and flac\n"
            + "  o Bitrates are in the format <bitdepth-samplerate>\n"
            + "    Valid bitrates are 16-44, 16-48, 24-44, 24-48, 24-88, and 24-96\n"
            + "  o Tracked wav files are in <bitrate> subdirectory\n"
            + "  o Raw (unedited or tracked) audio files are in <" + RAW + "> subdirectory\n"
            + "  o FLAC is the only audio compression format supported for raw files\n"
            + "  o AAC compressed files are saved in MPEG-4 Audio (.m4a) container\n"
            + "  o If compression bitrates are not specified, all bitrates are assumed\n"
            + "  o Metadata in infotext and info.cue files can be used to tag compressed audio\n"
            + "  o Command line macros can be defined in the aatb_config.ini configuration file\n"
            );
        } // end PrintHelp
    }
}