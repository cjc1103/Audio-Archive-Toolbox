/*
 * Audio Archive Toolbox
 * Author: Christopher J. Cantwell
 * 
 * Released under the GPL 3.0 software license
 * https://www.gnu.org/licenses/gpl-3.0.en.html
 * 
 * Overview
 * Audio Archive Toolbox (AATB) is a command line utility to perform audio file compression,
 * decompression, tagging, and conversion from one bitrate to another. It must be started from
 * the command line in the parent directory to the directory(s) containing the input wav files.
 * The program walks the directory tree under the starting directory to find and operate on
 * relevant audio files in various formats.
 * 
 * Description
 * AATB has seven mutually exclusive primary modes:
 * (1) Create compressed audio files from WAV format audio files. Current audio compression formats
 *     are MP3, AAC (M4A), OGG, OPUS, ALAC, and FLAC, but others may be added.
 *     Secondary functions include:
 *     o Create MD5 checksum for all compressed audio files
 *     o Create FLAC fingerprint (FFP) files for all FLAC files
 *     o Create shntool track length report for 16-44 FLAC files only
 *     o Create M3U playlists for all compressed audio files
 *     o Copy information text file to compressed audio subdirectories
 *     o Create tags for audio tracks using infotext file, or directory name
 *     Note: Only the first parent directory infotext file is used, others are ignored
 *     Note: Only WAV files in the <bitrate> directories are processed, other files are ignored
 *     Note: If the appropriate subdirectory does not exist, then it is created
 *     Note: If the compressed subdirectory exists, then wav files are not converted to the
 *       appropriate format. This behavior may be overridden with overwrite option
 * (2) Verify existing compressed audio files and metadata
 *     o create M3U playlists if they don't exist
 *     o MD5 checksum file for all compressed audio directories
 *     o FFP checksum file for all FLAC directories
 *     o shntool track length reports for 16 bit FLAC directory only
 *     Note: When verifying a compressed directory, the overwrite option is required
 *       to replace existing files
 *     o Information (infotext) file will be recopied to subdirs with overwrite option
 *     o rewrite tags for all compressed audio formats (except opus and alac)
 * (3) Decompress FLAC format audio files to WAV format
 *     o Wav files will be output to the appropriate <bitrate> directory
 *     o Raw audio files will be output to the input directory
 *     o Existing files may be overwritten using the overwrite option.
 *     Note: Only FLAC files in the <bitrate> and <raw> directories are processed,
 *       other files are ignored
 * (4) Join tracked wav files into one contiguous wav file.
 *     o Tracked wav files must be in a subdirectory named "<bitrate>"
 *     o Joined wav file will be written to the parent directory
 * (5) Delete source WAV directories ("<bitrate>") and raw WAV audio directory ("Audio")
 *     Note: A FLAC backup copy of each wav file is verified before the wav
 *     file is deleted
 * (6) Convert bitrate of WAV files into another bitrate format using the sox utility.
 *     e.g. convert wav files in 24-48 format into 16-44 format
 * (7) Create Cuesheet files from WAV audio files in a "<bitrate>" directory
 *     Note: Hydrogen audio has a complete definition of the cuesheet format
 *     This program only uses a subset, enough to read track splits by CD Wave utility
 *
 * Note: default lossy compression parameters have been set to get roughly >=200kbps
 *   Options allow some adjustment of compression parameters
 *
 * Audio compression formats
 * Currently supports the following audio compression formats, in order of quality:
 *   flac (lossless, biggest file size, best quality)
 *   opus (best lossy codec)
 *   ogg (better performance than aac)
 *   aac (Apple Advanced Codec) (m4a extension)
 *   mp3 (use only for backward compatibility for older devices)
 *   Note: program could be extended to support other audio compression formats by
 *      modifying the AudioFormatBitrate array and adding code to handle the new format
 *      
 * Required file input
 *   PCM wav format audio files
 *   Note: Valid bitrates are hard coded in AudioFormatBitrate array as follows:
 *      16 bit, 44.1 kHz (16-44)
 *      16 bit, 48.0 kHz (16-48)
 *      24 bit, 44.1 kHz (24-44)
 *      24 bit, 48.0 kHz (24-48)
 *      24 bit, 88.0 kHz (24-88)
 *      24 bit, 96.0 kHz (24-96)
 *      Entering in a non-supported bitrate will result in no processing
 *   Note: program could be extended to support other audio bitrates by
 *      modifying the AudioFormatBitrate array
 *
 * Directory naming conventions
 *
 * Input directory and file structure format
 *	 Note: <D> signifies directory name <dd> is a two digit string
 *   Commercial recording
 *   <D><dd> <artist> - <album>
 *      <D>16-44 (Note: only valid bitrate, ripped from commercial CDs)
 *      
 *   Live recording [optional stage info]
 *   <D><dd> <artist> <date>[.<stage>]
 *   <D><dd> <artist>_<date>[.<stage>]
 *      <dirname>.infotext (optional concert information file)
 *      <dirname>.info.cue (optional cue file for burning CDs)
 *      <D><bitdepth-samplerate>
 *        Contains the uncompressed wav audio files. The directbory name is
 *        the bitrate of the audio files, e.g, "16-44", "24-48", etc.
 *      <D>Audio: contains "raw" (unedited) wav audio files
 *
 *    Limitations: Any prefix number in directory name is ignored, for when concert
 *	    folder names are prefaced with a sequence number. This means any band with a
 *		name beginning with a number will not work, the artist name will be missing the 
 *		number, e.g. "18 South" => "South". The folders can be renamed afterwards if desired,
 *		without affecting the file names, checksums, or playlists.
 *		
 * Output directory name formats for compressed files, created under the input directory
 *   Commercial recording:
 *      <D><artist> - <album>.<bitrate>.<compressiontype>f
 *   Live recording:
 *      <D><artist>_<date>.<bitrate>.<compressiontype>f
 *      <D><Artist>_<date>.<stage>.<bitrate>.<compressiontype>f
 *   <artist>: artist name
 *   <album>: album name
 *   <stage>: stage name (optional)
 *   <bitrate>: bitdepth-samplerate
 *      e.g 16-44, 24-48, etc.
 *   <compressiontype>
 *      mp3, m4a, ogg, opus, alac, or flac
 *      Note: The letter f is appended to the name to signify a "folder" (directory)\
 *		Note: Although ALAC is a lossless audio compression algorithm, it is treated
 *			  like a lossy algorithm in this program. FLAC is the only lossless
 *			  compression scheme supported to simplify the program.
 *   
 * Info file format
 *   (info header format #1 - no labels):
 *   Artist
 *   Venue
 *   Location
 *   Date
 *   
 *   (info header format #2 - no labels):
 *   Artist
 *   Event
 *   Location
 *   Stage
 *   Date
 *   
 *   (alternate info header format, using labels as follows):
 *   PERFORMER <Artist name>
 *   TITLE <Event name>
 *   VENUE <Venue name>
 *   LOCATION <Location>
 *   STAGE <Stage name>
 *   DATE <yyyy-mm-dd>
 *
 *   Artist member lineup info
 *   
 *   Recording technical data and credits
 *   
 *   Track List
 *   (track number can be in the format dd<sp>, dd.<sp> with/without leading zeroes)
 *   dd
 *   dd
 *   :
 *   :
 *   dd
 *   
 * Cuesheet format: (ref. Hydrogen Audio wiki, amended):
 *   PERFORMER "My Bloody Valentine"
 *   TITLE "Loveless"
 *   EVENT "Vampire Festival"
 *   VENUE "Castle Dracula"
 *   STAGE "Dungeon"
 *   LOCATION "Translyvania"
 *   DATE "Date"
 *   FILE "My Bloody Valentine - Loveless.wav" WAVE
 *     TRACK 01 AUDIO
 *       TITLE "Shallow Grave"
 *       PERFORMER "Herman Munster"
 *       INDEX 01 00:00:00
 *     TRACK 02 AUDIO
 *       TITLE "Wait For Me"
 *       PERFORMER "Cruella de Ville"
 *       INDEX 01 04:32:67
 *   Notes:
 *     Index time is cumulative Min:Sec:Frames (CD Frame = 1/75 sec)
 *     First track INDEX 01 is always 00:00:00
 *   
 * Command line switches
 * basic operations (mutually exclusive)
 *   -c|--compress          compress wav PCM format audio to a compressed audio format
 *       [-i|--use-infotext] | [-e|--use-cuesheet]  
 *                          read metadata from an infotext concert information file, or
 *				            read metadata from cuesheet (.cue) for commercial recordings
 *       -p|--m3u-playlist  create m3u playlist file for all audio files in directory
 *   -v|--verify            verify flac files are correct by checking md5 and ffp checksum files
 *       [-i|--use-infotext] | [-e|--use-cuesheet]  
 *                          read metadata from an infotext concert information file, or
 *				            read metadata from cuesheet (.cue) for commercial recordings
 *       --md5              creates/updates md5 checksum files
 *       --ffp              creates/updates ffp checksum files
 *       --shn              creates/updates shntool report files
 *       -a|--all-reports   combines --md5 --ffp --shn options
 *       -t|--tag			update audio file tags (metadata)
 *   -d|--decompress        decompress lossless files to wav format
 *      --alac=<bitrate>|all  Apple Lossless Audio Compression (except raw files)
 *      --flac=<bitrate>|raw|all  Freeware Lossless Audio Compression
 *   -j|--join				joins tracked wav format files into one contiguous wav file
 *		--wav=<bitrate>		specifies bitrate of wav files to be joined
 *   -r|--rename-wav-files	renames wav files in <bitrate> directory
 *		--wav=<bitrate>		specifies bitrate of wav files to be renamed
 *   -y|--convert-to-wav    convert to wav format
 *       --shn				convert shn lossless format to wav
 *       --aif				convert apple aif format to wav
 *       --wma				convert windows media wma format to wav
 *   -z|--convert-to-bitrate   convert wav files to bitrate
 *      --wav=<bitrate>     convert wav files of bitrate
 *   -s|--create-cuesheet   create cuesheet from wav files
 *      -i|--use-infotext   read metadata from an infotext concert information file
 *      --wav=<bitrate>     use wav files of bitrate
 *   -x|--delete            delete redundant files after compression is complete
 *      --wav=<bitrate>|raw|all  Delete all input wav directories for the specified bitrate
 *      [--misc|--misc-files-delete]  Delete misc files and directories specified in the config file
 *
 * compression and verification arguments
 *   --mp3=<bitrate>        compress wav to mp3 format (.mp3) (16-44 is default)
 *   --mp3-quality=<value>  mp3 compression parameter
 *   [--aac|--m4a]=<bitrate>  compress wav to aac format, mp4 audio container (.m4a)
 *   [--aac-quality|--m4a-quality]=<value>  aac compression parameter
 *   --ogg=<bitrate>        compress wav to vorbis format, ogg container (.ogg)
 *   --ogg-quality=<value>  ogg compression parameter
 *   --opus=<bitrate>       compress wav to opus format (.opus)
 *   --opus-quality=<value> opus compression parameter
 *   --alac=<bitrate>|raw|all Apple Lossless Audio Codec (.alac)
 *   --alac-quality=<value> placeholder, not currently implemented in qaac64
 *   --flac=<bitrate>|raw|all Freeware Lossless Audio Codec (.flac)
 *   --flac-quality=<value> flac compression parameter
 * 
 * compression <bitrate>
 *   =<bitdepth>-<samplerate> e.g 16-44, 24-48, etc.
 *   =raw                   raw audio format, in the "Audio" subdirectory
 *   =all or no option      all bitrates
 *   
 * additional options
 *   --lc|--lower-case      convert subdirectory names to lower case
 *   --tc|--title-case      convert subdirectory names to title case (capitalize first letter of each word)
 *   --ri|--rename-info-files  rename infotext and info.cue files
 *   --icd|--get-info-from-current-dir  use current directory for infotext file when verifying compressed files
 *   --ocd|--output-to-current-dir  redirect decompressed files to current directory
 *   -o|--overwrite         overwrite existing files
 *   -h|--help              display options list on console
 *   --hh|--verbose         write StandardError datastream to console
 *   --debug                switch to write debug files to console
 * 
 * Configuration file
 *   A configuration file can be used for various configuration options
 * 	 The file is called "aatb_config.ini" and is located in the program directory
 *   If the file does not exist or is unreadable, the program will exit.
 * 
 *   # Comments start with "#" character
 *   # format: key = arguments
 *   # arguments do not need to be enclosed in quotes
 *   [Settings]
 *   InfoFileExtension = <extension>
 *   CuesheetFileExtension = <extension>
 *   [FilesToDelete]
 *   # entries must have unique key, use consecutive numerals
 *   01 = <extension>
 *   02 = <extension>
 *   [DirsToDelete]
 *   # entries must have unique key, use consecutive numerals
 *   01 = <directory> 
 *   02 = <directory>
 *   [Macros]
 *   # macroname = <command line arguments>
 *   # e.g: -xyz = --verify --tag --all -
 *
 * Dependencies and limitations
 *   This program requires .NET 8.0 runtime or later, and is compiled as a x64 Windows binary
 *   IniParser.dll library, created by Windows Visual Studio NuGet package ini-parser
 *   https://github.com/rickyah/ini-parser
 *
 * External Windows programs called from this script (must be in path). The installation script will
 * create a directory "c:\Program Files\Audio Tools" to install these programs.
 *   flac            Freeware Lossless Audic Codec
 *   fdkaac          Frauhoefer AAC encoder
 *   id3             Tagging program for mp3 files
 *   lame            "LAME Ain't an MP3 Encoder" Freeware MP3 encoder (.mp3)
 *   md5sums         Creates/verifies md5 checksums (this program can output md5 in unix format)
 *   MediaInfo       Multipurpose utility to get metadata for audio files, CLI version only
 *   metaflac        Multipurpose metadata editing utility for flac files
 *   NeroEncAac      Nero AAC encoder (not developed any longer)
 *   NeroTagAac      Tagging program for AAC encoded audio files
 *   oggenc2         Ogg Vorbis audio encoder (.ogg)
 *   opusenc         Opus audio encoder (.opus)
 *   qaac64          Freeware implementation of Apple's Advanced Audio Codec, x64 version  
 *                   Requires CoreBoxAudio.dll (installed using AppleApplicationSupport.msi)
 *                   compresses wav file to aac (.m4a) and ALAC (.alac) formats
 *   shntool         Multi-purpose sound utility. Calculates CD sector boundaries (16 bit flacs only)
 *   sox             SOund eXchange utility to change file formats and calculate audio file bitrate
 *   vorbiscomment   Utility for modifying metadata of Vorbis encoded audio files (OGG format)
 *   
 * Setup scripts were created by using InnoSetup
 * 
 * Installation script (Audio Archive Toolbox x.x.x Setup.exe):
 *   Copies program files, dlls, and external tools (above) to Windows
 *   installs the following required programs which have separate installation scripts:
 *     Apple Application Support 64bit
 *     Microsoft .Net Framework 64bit
 *     Sound Exchange utility (sox) 32bit
 *   Adds aatb and sox program locations to the Windows environment path
 * 
 * Uninstallation script (uninst000.exe):
 *   Removes all required programs and external tools (above)
 *   Does not remove the following programs, as they may be used by other programs. These
 *   may be uninstalled manually if desired:
 *     Apple Application Support 64bit
 *     Microsoft .Net Framework 64bit
 *     Sound Exchange utility (sox) 32bit
 *   Removes aatb and sox program locations from the Windows environment path
 */