﻿using System;
using System.IO;
using System.Linq;

namespace AATB
{
    public partial class AATB_Main
    {
        static void WalkDirectoryTree(DirectoryInfo CurrentDir)
        {
            /* Processes command functions on current directory, then calls itself
             *   recursively to walk all subdirectories
             * Directory information can be found in the DirectoryInfo class
             * Inputs: Command line
             * Outputs: Files are converted and written to subdirectories
             */
            DirectoryInfo[] SubDirs;
            int
                index;
            string
                WAVDirName,
                WAVDirPath,
                FLACDirName,
                FLACDirPath,
                CompAudioDirName,
                CompAudioDirPath,
                CompAudioFormat,
                CompAudioDirExtension,
                M3UFilePath,
                MD5FilePath,
                FFPFilePath,
                SHNReportPath,
                JoinedWAVFilename;

            // Populate filelists for each type of file in this directory
            FileInfo[]
                AllFilesList = CurrentDir.GetFiles(),
                WAVFileList = CurrentDir.GetFiles(ALLWAV),
                FLACFileList = CurrentDir.GetFiles(ALLFLAC),
                CompAudioFileList, // populated as needed
                MD5FileList = CurrentDir.GetFiles(ALLMD5),
                FFPFileList = CurrentDir.GetFiles(ALLFFP),
                SHNReportList = CurrentDir.GetFiles(ALLSHN),
                M3UFileList = CurrentDir.GetFiles(ALLM3U),
                ParentInfotextList = CurrentDir.Parent.GetFiles(ALLINFOTXT),
                ParentCuesheetList = CurrentDir.Parent.GetFiles(ALLINFOCUE);
            bool
                FLACFileFound,
                WAVFileBackupExists,
                WAVExists = WAVFileList.Any(),
                FLACExists = FLACFileList.Any(),
                MD5Exists = MD5FileList.Any(),
                FFPExists = FFPFileList.Any(),
                SHNExists = SHNReportList.Any(),
                M3UExists = M3UFileList.Any(),
                ParentInfotextExists = ParentInfotextList.Any(),
                ParentCuesheetExists = ParentCuesheetList.Any();
            
            // = = = = = = = = Initialization = = = = = = = =
            // Instantiate AATB_DirInfo class for current directory
            // Constructor will populate directory information
            AATB_DirInfo Dir = new (CurrentDir);

            // Get metadata information from directory names
            // Exclude starting directory
            if (Dir.Path != RootDir)
            {
                // use only the first info.txt file, others are ignored
                if (ParentInfotextExists)
                    Dir.ParentInfotextPath = ParentInfotextList[0].FullName;
                // use only the first cue file, others are ignored
                if (ParentCuesheetExists)
                    Dir.ParentCuesheetPath = ParentCuesheetList[0].FullName;
                // populate directory information
                GetDirInformation(Dir);
            }

            // = = = = = = = = Compress Audio section = = = = = = = =
            // Compress WAV audio files to various compressed formats
            // compressed audio files are written to the appropriate directory
            // Command: -c|--compress --compformat=<bitrate>|all
            if (CompressAudio )
            {
                if (Debug) Console.WriteLine("dbg: Compress Section");

                // only process WAV files in <bitrate> and raw audio directories
                // check audioformats and audiobitrates flags are set and WAV files exist
                if ((Dir.Type == WAVAUDIO || Dir.Type == RAWAUDIO)
                    && CheckFormatBitrate(ANYFORMAT, Dir.Name)
                    && WAVExists)
                {
                    // loop through all compressed audio formats in AudioFormats list
                    // ignore last entry in list = WAV
                    for (index=0; index <= AudioFormats.Length - 2; index++)
                    {
                        CompAudioFormat = AudioFormats[index];
                        CompAudioDirExtension = CompressedDirExtensions[index];

                        // check format and bitrate (directory name) flag is set
                        if (CheckFormatBitrate(CompAudioFormat, Dir.Name))
                        {
                            // populate directory metadata
                            GetDirMetadata(Dir);

                            // populate track metadata
                            GetTrackMetadata(Dir, WAVFileList);

                            // compressed audio formats
                            if (Dir.Type == WAVAUDIO)
                            {
                                // Create subdir if it does not exist
                                CompAudioDirName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + CompAudioDirExtension;
                                CompAudioDirPath = Dir.ParentPath + BACKSLASH + CompAudioDirName;
                                if ((Directory.Exists(CompAudioDirPath) && Overwrite)
                                    || (!Directory.Exists(CompAudioDirPath) && CreateDir(CompAudioDirPath)))
                                {
                                    // compress audio files
                                    Log.WriteLine("  Converting to " + CompAudioFormat.ToUpper() + ": " + CompAudioDirName);
                                    CompAudioFileList = CompressWAV(CompAudioFormat, Dir, CompAudioDirPath, WAVFileList);

                                    // create MD5 Checksum
                                    MD5FilePath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + MD5;
                                    CreateMD5ChecksumFile(MD5FilePath, CompAudioFileList, WriteLogMessage);

                                    // create other reports - FLAC only
                                    if (CompAudioFormat == FLAC)
                                    {
                                        // create FLAC Fingerprint (FFP) file
                                        if (CreateFFP)
                                        {
                                            FFPFilePath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + FFP;
                                            CreateFFPChecksumFile(FFPFilePath, CompAudioFileList, WriteLogMessage);
                                        }

                                        // create shntool length report (requires flac 1.3.2)
                                        if (CreateSHN)
                                        {
                                            SHNReportPath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + SHN;
                                            CreateSHNReport(SHNReportPath, CompAudioFileList);
                                        }
                                    }

                                    // create m3u Playlist
                                    if (CreateM3U)
                                    {
                                        M3UFilePath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + M3U;
                                        CreateM3UPlaylist(Dir, M3UFilePath, CompAudioFileList);
                                    }

                                    // copy info file, if it exists, to destination directory
                                    if (UseInfotext && ParentInfotextExists)
                                        CopyTextFile(Dir.ParentInfotextPath, CompAudioDirPath);
                                }
                                else
                                    Log.WriteLine("*** " + CompAudioFormat.ToUpper()
                                                + " directory already exists, use overwrite option to replace\n"
                                                + "    " + CompAudioDirName);
                            }

                            // raw audio
                            else if (Dir.Type == RAWAUDIO)
                            {
                                CompAudioDirName = Dir.Name;
                                CompAudioDirPath = Dir.Path;
                                if (!FLACExists || (FLACExists && Overwrite))
                                {
                                    Log.WriteLine("  Converting to FLAC: " + CompAudioDirName);
                                    // return list is discarded
                                    CompressWAV(FLAC, Dir, CompAudioDirPath, WAVFileList);
                                    // Note: md5, ffp, shtool report and m3u playlists are not used for raw files
                                }
                                else
                                    Log.WriteLine("*** FLAC files already exist, use overwrite option to replace");
                            }
                        }
                    }
                }
            } // end Compress Audio section

            // = = = = = = = = Verify Audio section = = = = = = = = 
            // (1) Verify metadata in compressed audio directories only
            //     a. MD5 checksums - all commpressed audio files
            //     b. FFP (FLAC Fingerprint) checksums - FLAC files
            //     c. SHN (shntool file length and CD sector boundary report) - FLAC files
            // (2) Create/Update ID3 tags (rewrites MD5 checksum files)
            // (3) Create/update M3U Playlists
            // Command: -v|--verify --compformat=<bitrate>|all
            else if (VerifyAudio)
            {
                if (Debug) Console.WriteLine("dbg: Verify Section");

                // compressed audio directory
                if (Dir.Type == COMPAUDIO
                    || Dir.Type == RAWAUDIO)
                {
                    if (Debug) Console.WriteLine("dbg: Dir compression Format: " + Dir.AudioCompressionFormat
                           + "  Dir bitrate: " + Dir.Bitrate);

                    // loop through all compressed audio formats in AudioFormats list
                    // ignore last entry in list = WAV
                    for (index=0; index <= AudioFormats.Length - 2; index++)
                    {
                        CompAudioFormat = AudioFormats[index];
                        if (Debug) Console.WriteLine("dbg: CompAudioFormat: " + CompAudioFormat);

                        // check format and bitrate (directory name) flag is set
                        // and directory compression format matches
                        if (CheckFormatBitrate(CompAudioFormat, Dir.Bitrate)
                            && Dir.AudioCompressionFormat == CompAudioFormat)
                        {
                            Log.WriteLine("  Verifying and updating " + CompAudioFormat.ToUpper() + " metadata");

                            // get list of compressed audio files
                            CompAudioFileList = CurrentDir.GetFiles("*." + CompAudioFormat);

                            if (CompAudioFileList != null)
                            {
                                // populate directory metadata
                                GetDirMetadata(Dir);

                                // populate track metadata
                                GetTrackMetadata(Dir, CompAudioFileList);

                                // Create ID3 tags
                                // Note: This also changes MD5 checksums, recreate them
                                MD5FilePath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + MD5;
                                if (CreateTags)
                                {
                                    TagCompressedAudio(CompAudioFormat, Dir, CompAudioFileList);
                                    CreateMD5ChecksumFile(MD5FilePath, CompAudioFileList, WriteLogMessage);
                                }
                                else
                                {
                                    // Verify MD5 Checksum file
                                    if (CreateMD5)
                                    {
                                        if (MD5Exists && !Overwrite)
                                            VerifyMD5ChecksumFile(MD5FilePath, MD5FileList, CompAudioFileList);
                                        else
                                            CreateMD5ChecksumFile(MD5FilePath, CompAudioFileList, WriteLogMessage);
                                    }
                                }

                                // Verify FLAC Fingerprint file - FLAC only
                                if (FLACExists && CreateFFP)
                                {
                                    FFPFilePath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + FFP;
                                    if (FFPExists && !Overwrite)
                                        VerifyFFPChecksumFile(FFPFilePath, FFPFileList, CompAudioFileList);
                                    else
                                        CreateFFPChecksumFile(FFPFilePath, CompAudioFileList, WriteLogMessage);
                                }

                                // Verify shntool report - FLAC only
                                if (FLACExists && CreateSHN)
                                {
                                    SHNReportPath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + SHN;
                                    if (SHNExists && !Overwrite)
                                        VerifySHNReport(SHNReportPath, SHNReportList, Dir.Bitrate);
                                    else
                                        CreateSHNReport(SHNReportPath, CompAudioFileList);
                                }

                                // Create M3U Playlist
                                if (CreateM3U)
                                {
                                    M3UFilePath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + M3U;
                                    if (!M3UExists || (M3UExists && Overwrite))
                                        CreateM3UPlaylist(Dir, M3UFilePath, CompAudioFileList);
                                    else
                                        Log.WriteLine("*** M3U playlist exists, use overwrite option to replace");
                                }

                                // Copy info.txt File from parent directory
                                if (UseInfotext && ParentInfotextExists)
                                    CopyTextFile(Dir.ParentInfotextPath, Dir.Path);
                            }
                            else
                                Log.WriteLine("*** No " + CompAudioFormat.ToUpper() + " format files found");
                        }
                    }
                }
            } // end Verify Audio section

            // = = = = = = = = Decompress Audio section = = = = = = = = 
            // Decompress FLAC lossless audio files to WAV format from raw audio and compressed audio
            // directories. Other lossless formats like ALAC are not supported for simplicity
            // Command: -d|--decompress --flac=<bitrate>|raw|all
            else if (DecompressAudio)
            {
                if (Debug) Console.WriteLine("dbg: Decompress Section");

                // compressed audio directories
                if (Dir.Type == COMPAUDIO
                    && Dir.AudioCompressionFormat == FLAC
                    && CheckFormatBitrate(FLAC, Dir.Bitrate))
                {
                    // FLACFileList is populated during initialization for each traversal of WalkDirectory
                    if (FLACFileList != null)
                    {
                        // WAV output directory is a parent subdir <Bitrate>
                        WAVDirName = Dir.Bitrate;
                        WAVDirPath = Dir.ParentPath + BACKSLASH + WAVDirName;
                        if ((Directory.Exists(WAVDirPath) && Overwrite)
                            || (!Directory.Exists(WAVDirPath) && CreateDir(WAVDirPath)))
                        {
                            Log.WriteLine("  Decompressing to WAV directory: " + WAVDirName);
                            DecompressToWAV(FLAC, WAVDirPath, FLACFileList);
                        }
                        else
                            Log.WriteLine("*** WAV directory already exists, use overwrite option to replace\n"
                                        + "    " + WAVDirPath);
                    }
                    else
                        Log.WriteLine("*** No FLAC format files found");
                }

                // raw audio directory
                else if (Dir.Type == RAWAUDIO
                        && CheckFormatBitrate(FLAC, Dir.Bitrate))
                {
                    // FLACFileList is populated during initialization for each traversal of WalkDirectory
                    if (FLACFileList != null)
                    {
                        // WAV output directory is the input directory
                        WAVDirName = Dir.Name;
                        WAVDirPath = Dir.Path;
                        if (!WAVExists || (WAVExists && Overwrite))
                        {
                            Log.WriteLine("  Decompressing to WAV directory: " + WAVDirName);
                            DecompressToWAV(FLAC, WAVDirPath, FLACFileList);
                        }
                        else
                            Log.WriteLine("*** WAV file(s) already exist, use overwrite option to replace");
                    }
                    else
                        Log.WriteLine("*** No FLAC format files found");
                }

            } // end Decompress Audio section

            // = = = = = = = = Delete Audio section = = = = = = = = 
            // (1) Delete unneeded wav files after compression from:
            //     a. RAW directory
            //     b. Uncompressed WAV directories <bitrate>
            //     Backup FLAC files are verified before deletion
            // (2) Delete extraneous temporary files by extension wildcard
            // (3) Mark empty directories for deletion. Directories cannot be deleted immedaitely
            //     as the program is calling WalkDirectoryTree recursively
            // Command: -x|--delete --wav=<bitrate>|raw|all
            else if (DeleteAudio)
            {
                if (Debug) Console.WriteLine("dbg: Delete Section");

                // populate directory metadata
                GetDirMetadata(Dir);

                // check appropriate format flag is set (WAV, <bitrate>|RAW)
                if (CheckFormatBitrate(WAV, Dir.Name))
                {
                    // delete wav files from <bitrate> directories
                    if (Dir.Type == WAVAUDIO)
                    {
                        // WAVFileList contains WAV filenames in this directory
                        // Find corresponding FLAC directory and convert to DirectoryInfo format
                        FLACDirName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + FLACF;
                        FLACDirPath = Dir.ParentPath + BACKSLASH + FLACDirName;
                        DirectoryInfo FLAC_DirInfoPath = new DirectoryInfo(FLACDirPath);
                        if (Directory.Exists(FLACDirPath))
                        {
                            // get all FLAC files in FLAC directory
                            FLACFileList = FLAC_DirInfoPath.GetFiles(ALLFLAC);
                            if (FLACFileList == null)
                                WAVFileBackupExists = false;
                            else
                                WAVFileBackupExists = true;
                            // match each wav file with corresponding flac file
                            foreach (FileInfo wav in WAVFileList)
                            {
                                FLACFileFound = false;
                                foreach (FileInfo flac in FLACFileList)
                                {
                                    if (BaseNamesAreEqual(wav.Name, flac.Name))
                                    {
                                        // found correspond FLAC file in FLAC directory
                                        FLACFileFound = true;
                                        break;
                                    }
                                }
                                if (!FLACFileFound)
                                {
                                    // set flag to prevent wav directory deletion
                                    Log.WriteLine("  FLAC Backup file not found for " + wav.Name +
                                                Environment.NewLine + "    WAV directory will be retained");
                                    WAVFileBackupExists = false;
                                }
                            }
                            if (WAVFileBackupExists)
                            {
                                Log.WriteLine("  Marking directory for deletion: " + Dir.Name);
                                DirsMarkedForDeletion.Add(Dir.Path);
                            }
                        }
                        else
                            Log.WriteLine("*** FLAC directory does not exist: " + FLACDirName);
                    }

                    // delete wav files from RAW directory
                    else if (Dir.Type == RAWAUDIO)
                    {
                        foreach (FileInfo wav in WAVFileList)
                        {
                            FLACFileFound = false;
                            // Check the corresponding FLAC file exists before deleting wav file
                            foreach (FileInfo flac in FLACFileList)
                            {
                                if (BaseNamesAreEqual(wav.Name, flac.Name))
                                {
                                    FLACFileFound = true;
                                    Log.WriteLine("  Deleting file: " + wav.Name);
                                    DeleteFile(wav.FullName);
                                    break;
                                }
                            }
                            if (!FLACFileFound)
                                Log.WriteLine("*** WAV file has no matching FLAC file, and has not been deleted: " + wav.Name);
                        }
                    }

                    // delete extraneous files created by sound editing process
                    // "AllFilesList" contains all files in the current directory
                    // "FilesToDelete" contain all file types (extensions) to be deleted
                    foreach (FileInfo fi in AllFilesList)
                    {
                        if (FilesToDelete.Contains(fi.Extension))
                        {
                            Log.WriteLine("  Deleting file " + fi.Name);
                            DeleteFile(fi.FullName);
                        }
                    }
                }

                // Mark miscellaneous directories in DirsToDelete list for deletion
                foreach (string dirtodelete in DirsToDelete)
                {
                    if (dirtodelete == Dir.Name)
                    {
                        Log.WriteLine("  Marking directory for deletion: " + Dir.Name);
                        DirsMarkedForDeletion.Add(Dir.Path);
                    }
                }
            } // end Delete Audio section

            // = = = = = = = =  Join WAV Files section = = = = = = = = 
            // Concatenate separate WAV files into one combined wav file
            // wav files must exist in the Dir.Bitrate directory specified by --wav=<bitrate>
            // command: -j|--join --wav=<bitrate>
            else if (JoinWAV)
            {
                if (Debug) Console.WriteLine("dbg: Join WAV Files Section");
                if (CheckFormatBitrate(WAV, Dir.Name)
                    && WAVExists)
                {
                    // populate directory metadata
                    GetDirMetadata(Dir);

                    JoinedWAVFilename = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + WAV;
                    ConcatentateWAVFiles(Dir, WAVFileList, JoinedWAVFilename);
                }

            } // end Join WAV Files section

            // = = = = = = = =  Convert Audio Bitrate section = = = = = = = = 
            // Convert wav files from one bitrate to another
            // Command: -z|--convert-bitrate=<bitrate to> --wav=<bitrate from>
            else if (ConvertAudioBitrate)
            {
                if (Debug) Console.WriteLine("dbg: Convert Bitrate Section");

                // Only convert wav files in <bitrate> directory with WAV flag set
                if (CheckFormatBitrate(WAV, Dir.Name)
                    && Dir.Name == ConversionFromBitrate
                    && WAVExists)
                {
                    // ConversionToBitrate is entered as the "Convert" command line argument
                    // convert all wav files in list to the desired bitrate
                    ConvertBitrate(Dir, WAVFileList, ConversionFromBitrate, ConversionToBitrate);
                }
            } // end Convert Audio Bitrate section

            // = = = = = = = =  Create Cuesheet section = = = = = = = = 
            // Create cuesheet from wav track metadata
            // command: -r|--create-cuesheet --wav=<bitrate>
            if (CreateCuesheet)
            {
                if (Debug) Console.WriteLine("dbg: Create Cuesheet Section");

                if (Dir.Type == WAVAUDIO
                    && CheckFormatBitrate(WAV, Dir.Name)
                    && WAVExists)
                {
                    // populate directory metadata
                    GetDirMetadata(Dir);

                    // populate track metadata
                    // this is obtained from parent infotext file or cuesheet, if specified
                    // otherwise from track filenames
                    GetTrackMetadata(Dir, WAVFileList);

                    // create cuesheet
                    // cuesheet name is calcuated, if it exists, overwrite to replace.
                    CreateCuesheetFile(Dir);

                }
            } // end Create Cuesheet section

            // = = = = = = = =  Recursion section = = = = = = = = 
            // Find all the subdirectories under this directory
            SubDirs = CurrentDir.GetDirectories();
            foreach (DirectoryInfo dirname in SubDirs)
            {
                // Recursive call to walk the directory tree for each subdirectory
                Log.WriteLine("Directory: " + dirname.Name);
                WalkDirectoryTree(dirname);
            }

        } // end WalkDirectoryTree
    }
}