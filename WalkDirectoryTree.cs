using System;

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
                i;
            string
                SubDirPath,
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
                SHNReportList = CurrentDir.GetFiles(ALLSHNRPT),
                InfotextList = CurrentDir.GetFiles(ALLINFOTXT),
                CuesheetList = CurrentDir.GetFiles(ALLINFOCUE),
                ParentInfotextList = CurrentDir.Parent.GetFiles(ALLINFOTXT),
                ParentCuesheetList = CurrentDir.Parent.GetFiles(ALLINFOCUE);
            bool
                WAVExists = WAVFileList.Any(),
                FLACExists = FLACFileList.Any(),
                FLACFileFound,
                WAVFileBackupExists,
                DirInfoPopulated,
                DirTrackInfoPopulated;

            // = = = = = = = = Initialization = = = = = = = =
            // Instantiate AATB_DirInfo class for current directory
            // AATB_DirInfo constructor will populate directory information
            AATB_DirInfo Dir = new(CurrentDir);

            // initialize dir metadata flags
            DirInfoPopulated = false;
            DirTrackInfoPopulated = false;

            // populate directory metadata - exclude root directory
            if (Dir.Path != RootDir)
                GetDirInformation(Dir);

            // = = = = = = = = Compress Audio section = = = = = = = =
            // Compress WAV audio files to various compressed formats
            // Note: compressed audio files are written to the appropriate directory
            // Command: -c|--compress --<compformat>=<bitrate>|all
            //          where <compformat> is a valid compression format
            if (CompressAudio)
            {
                if (Debug) Console.WriteLine("dbg: Compress Section");

                // check audioformats and audiobitrates flags are set
                // and wav format files exist
                if (CheckFormatBitrate(ANYFORMAT, Dir.Name)
                    && WAVExists)
                {
                    // loop through all audio formats in CompressedAudioFormats list
                    for (i = 0; i <= AudioCompressionFormats.Length - 1; i++)
                    {
                        // these arrays are indentical except of the suffix "f" in CompressedDirExtensions
                        CompAudioFormat = AudioCompressionFormats[i];
                        CompAudioDirExtension = CompressedDirExtensions[i];

                        // check format and bitrate (directory name) flag is set
                        if (CheckFormatBitrate(CompAudioFormat, Dir.Name))
                        {
                            // populate directory metadata - once for each directory
                            if (!DirInfoPopulated)
                            {
                                GetDirTextFiles(Dir, ParentInfotextList, ParentCuesheetList);
                                GetDirMetadata(Dir);
                                DirInfoPopulated = true;
                            }

                            // tracked wav audio directory
                            if (Dir.Type == TRACKEDAUDIO)
                            {
                                // populate track metadata - once for each tracked audio directory
                                if (!DirTrackInfoPopulated)
                                {
                                    GetTrackMetadata(Dir, WAVFileList);
                                    DirTrackInfoPopulated = true;
                                }

                                // create or overwrite compressed audio subdirectory
                                CompAudioDirName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + CompAudioDirExtension;
                                CompAudioDirPath = Dir.ParentPath + BACKSLASH + CompAudioDirName;
                                if ((Directory.Exists(CompAudioDirPath) && Overwrite)
                                    || (!Directory.Exists(CompAudioDirPath) && CreateDir(CompAudioDirPath)))
                                {
                                    // compress audio files
                                    Log.WriteLine("  Converting to " + CompAudioFormat.ToUpper() + ": " + CompAudioDirName);
                                    CompAudioFileList = ConvertFromWAV(CompAudioFormat, Dir, CompAudioDirPath, WAVFileList);

                                    // create MD5 Checksum
                                    if (CreateMD5)
                                    {
                                        MD5FilePath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + MD5;
                                        CreateMD5ChecksumFile(MD5FilePath, CompAudioFileList, WriteLogMessage);
                                    }

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
                                        if (CreateSHNRPT)
                                        {
                                            SHNReportPath = CompAudioDirPath + BACKSLASH + CompAudioDirName + PERIOD + SHNRPT;
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
                                    if (UseInfotext)
                                        CopyTextFile(Dir.InfotextPath, CompAudioDirPath);
                                }
                                else
                                    Log.WriteLine("*** " + CompAudioFormat.ToUpper()
                                                + " directory already exists, use overwrite option to replace\n"
                                                + "    " + CompAudioDirName);
                            }

                            // raw audio directory
                            else if (Dir.Type == RAWAUDIO)
                            {
                                // create or overwrite raw audio files
                                CompAudioDirName = Dir.Name;
                                CompAudioDirPath = Dir.Path;
                                if (!FLACExists || (FLACExists && Overwrite))
                                {
                                    if (Debug) PrintFileList(WAV, WAVFileList);
                                    Log.WriteLine("  Converting to FLAC: " + CompAudioDirName);

                                    // compress raw audio files, return list is discarded
                                    ConvertFromWAV(FLAC, Dir, CompAudioDirPath, WAVFileList);

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
            //     c. SHNRPT (shntool file length and CD sector boundary report) - FLAC files
            // (2) Create/Update ID3 tags (rewrites MD5 checksum files)
            // (3) Create/update M3U Playlists
            // Command: -v|--verify  --<compformat>=<bitrate>|all
            //          where <compformat> is a valid compression format
            else if (VerifyAudio)
            {
                if (Debug) Console.WriteLine("dbg: Verify Section");

                // compressed audio directory
                if (Dir.Type == COMPRESSEDAUDIO)
                {
                    // populate directory info from text files
                    if (UseCurrentDirInfo)
                        GetDirTextFiles(Dir, InfotextList, CuesheetList);
                    else
                        GetDirTextFiles(Dir, ParentInfotextList, ParentCuesheetList);

                    // loop through all audio formats in AudioFormats list
                    // ignore last entry in list = WAV
                    for (i = 0; i <= AudioFormats.Length - 2; i++)
                    {
                        CompAudioFormat = AudioFormats[i];
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
                                        if (File.Exists(MD5FilePath) && !Overwrite)
                                            VerifyMD5ChecksumFile(MD5FilePath, MD5FileList, CompAudioFileList);
                                        else
                                            CreateMD5ChecksumFile(MD5FilePath, CompAudioFileList, WriteLogMessage);
                                    }
                                }

                                // Verify FLAC Fingerprint file - FLAC only
                                if (FLACExists && CreateFFP)
                                {
                                    FFPFilePath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + FFP;
                                    if (File.Exists(FFPFilePath) && !Overwrite)
                                        VerifyFFPChecksumFile(FFPFilePath, FFPFileList, CompAudioFileList);
                                    else
                                        CreateFFPChecksumFile(FFPFilePath, CompAudioFileList, WriteLogMessage);
                                }

                                // Verify shntool report - FLAC only
                                if (FLACExists && CreateSHNRPT)
                                {
                                    SHNReportPath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + SHNRPT;
                                    if (File.Exists(SHNReportPath) && !Overwrite)
                                        VerifySHNReport(SHNReportPath, SHNReportList, Dir.Bitrate);
                                    else
                                        CreateSHNReport(SHNReportPath, CompAudioFileList);
                                }

                                // Create M3U Playlist
                                if (CreateM3U)
                                {
                                    M3UFilePath = Dir.Path + BACKSLASH + Dir.Name + PERIOD + M3U;
                                    if (!File.Exists(M3UFilePath)
                                        || (File.Exists(M3UFilePath) && Overwrite))
                                        CreateM3UPlaylist(Dir, M3UFilePath, CompAudioFileList);
                                    else
                                        Log.WriteLine("*** M3U playlist exists, use overwrite option to replace");
                                }
                            }
                            else
                                Log.WriteLine("*** No " + CompAudioFormat.ToUpper() + " format files found");
                        }
                    }
                }
            } // end Verify Audio section

            // = = = = = = = = Convert Audio to WAV files section = = = = = = = = 
            // Convert other audio formats to WAV files
            // RAW flag is used to specify a specific format to convert to WAV
            // Command: -y|--convert-to-wav
            else if (ConvertAudio)
            {
                if (Debug) Console.WriteLine("dbg: Convert Audio Section");

                // loop through all audio formats in AudioConversionFormats list
                for (i = 0; i <= AudioConversionFormats.Length - 1; i++)
                {
                    CompAudioFormat = AudioConversionFormats[i];

                    // check RAW flag is set for this format = convert
                    // and current dir is tracked wav audio
                    if (CheckFormatBitrate(CompAudioFormat, RAW)
                        && Dir.Type == TRACKEDAUDIO)
                    {
                        // get list of compressed audio files
                        CompAudioFileList = CurrentDir.GetFiles("*." + CompAudioFormat);
                        if (CompAudioFileList != null)
                        {
                            // convert to WAV in current directory
                            WAVDirName = Dir.Name;
                            WAVDirPath = Dir.Path;
                            Log.WriteLine("  Decompressing to directory: " + WAVDirName);
                            ConvertToWAV(CompAudioFormat, WAVDirPath, CompAudioFileList);
                        }
                    }
                }
            } // end Convert Audio section

            // = = = = = = = = Decompress Audio section = = = = = = = =
            // Decompress FLAC lossless audio files to WAV format from raw audio and compressed audio
            // directories. Other lossless formats like ALAC are not supported for simplicity
            // Command: -d|--decompress --flac=<bitrate>|raw|all
            else if (DecompressAudio)
            {
                if (Debug) Console.WriteLine("dbg: Decompress Section");

                // loop through all audio formats in AudioDecompressionFormats list
                for (i = 0; i <= AudioDecompressionFormats.Length - 1; i++)
                {
                    CompAudioFormat = AudioDecompressionFormats[i];
                    // check appropriate flag is set for directory

                    if (CheckFormatBitrate(CompAudioFormat, Dir.Bitrate))
                    {
                        if (Debug) Console.WriteLine("dbg: Decompress format: {0}  Bitrate: {1}", CompAudioFormat, Dir.Bitrate);
                        // get list of compressed audio files
                        CompAudioFileList = CurrentDir.GetFiles("*." + CompAudioFormat);

                        if (CompAudioFileList != null)
                        {
                            if (Dir.Type == COMPRESSEDAUDIO
                                && !OutputToCurrentDir)
                            {
                                // WAV output directory is a parent subdir <Bitrate>
                                WAVDirName = Dir.Bitrate;
                                WAVDirPath = Dir.ParentPath + BACKSLASH + WAVDirName;
                                if ((Directory.Exists(WAVDirPath) && Overwrite)
                                    || (!Directory.Exists(WAVDirPath) && CreateDir(WAVDirPath)))
                                {
                                    Log.WriteLine("  Decompressing to directory: " + WAVDirName);
                                    ConvertToWAV(CompAudioFormat, WAVDirPath, CompAudioFileList);
                                }
                                else
                                    Log.WriteLine("*** WAV directory already exists, use overwrite option to replace\n"
                                                + "    " + WAVDirPath);
                            }
                            else if (Dir.Type == RAWAUDIO
                                 || (Dir.Type == COMPRESSEDAUDIO && OutputToCurrentDir))
                            {
                                // decompress to current directory
                                WAVDirName = Dir.Name;
                                WAVDirPath = Dir.Path;
                                Log.WriteLine("  Decompressing to directory: " + WAVDirName);
                                ConvertToWAV(CompAudioFormat, WAVDirPath, CompAudioFileList);
                            }
                        }
                        else
                            Log.WriteLine("*** No " + CompAudioFormat + "format files found");
                    }
                }
            } // end Decompress Audio section

            // = = = = = = = = Join WAV Files section = = = = = = = = 
            // Concatenate separate WAV files into one combined wav file
            // Note: WAV files must exist in WAVFileList
            // Note: the directory name Dir.Name = bitrate
            // Command: -j|--join --wav=<bitrate>
            else if (JoinWAV)
            {
                if (Debug) Console.WriteLine("dbg: Join WAV Files Section");
                if (CheckFormatBitrate(WAV, Dir.Name)
                    && WAVExists)
                {
                    // populate directory metadata
                    GetDirTextFiles(Dir, ParentInfotextList, ParentCuesheetList);
                    GetDirMetadata(Dir);

                    JoinedWAVFilename = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + WAV;
                    ConcatentateWAVFiles(Dir, WAVFileList, JoinedWAVFilename);
                }
            } // end Join WAV Files section

            // = = = = = = = = Rename WAV files section = = = = = = = = 
            // Rename all wav files in curent directory using track metadata from infotext file
            // Note: WAV files must exist in WAVFileList
            // Note: the directory name Dir.Name = bitrate, raw directory is not valid
            // Command: -r|--rename-wav-files --wav=<bitrate>
            if (RenameWAV)
            {
                if (Debug) Console.WriteLine("dbg: Rename WAV Files Section");

                if (CheckFormatBitrate(WAV, Dir.Name)
                    && Dir.Name != RAW
                    && WAVExists)
                {
                    // populate directory metadata
                    GetDirTextFiles(Dir, ParentInfotextList, ParentCuesheetList);
                    GetDirMetadata(Dir);

                    // populate track metadata
                    GetTrackMetadata(Dir, WAVFileList);

                    // rename files in WAVFileList
                    RenameWAVFiles(Dir, WAVFileList);
                }
            } // end Rename WAV Files section

            // = = = = = = = = Convert Audio Bitrate section = = = = = = = = 
            // Convert wav files from one bitrate to another
            // Command: -z|--convert-to-bitrate=<bitrate to> --wav=<bitrate from>
            else if (ConvertBitrate)
            {
                if (Debug) Console.WriteLine("dbg: Convert Bitrate Section");

                // Only convert wav files in <bitrate> directory with WAV flag set
                if (CheckFormatBitrate(WAV, Dir.Name)
                    && Dir.Name == ConvertFromBitrate
                    && WAVExists)
                {
                    // ConvertToBitrate is entered as the "Convert" command line argument
                    // convert all wav files in list to the desired bitrate
                    ConvertWAVBitrate(Dir, WAVFileList, ConvertFromBitrate, ConvertToBitrate);
                }
            } // end Convert Audio Bitrate section

            // = = = = = = = = Create Cuesheet section = = = = = = = = 
            // Create cuesheet from directory and wav track metadata
            // Command: -s|--create-cuesheet --wav=<bitrate>
            if (CreateCuesheet)
            {
                if (Debug) Console.WriteLine("dbg: Create Cuesheet Section");

                if (CheckFormatBitrate(WAV, Dir.Name)
                    && WAVExists)
                {
                    // populate directory metadata
                    GetDirTextFiles(Dir, ParentInfotextList, ParentCuesheetList);
                    GetDirMetadata(Dir);

                    // populate track metadata
                    GetTrackMetadata(Dir, WAVFileList);

                    // create cuesheet
                    CreateCuesheetFile(Dir);
                }
            } // end Create Cuesheet section

            // = = = = = = = = Delete Audio section = = = = = = = = 
            // (1) Delete unneeded wav files after compression from:
            //     a. RAW directory
            //     b. Uncompressed WAV directories <bitrate>
            //     Backup FLAC files are verified before deletion
            // (2) Delete extraneous temporary files by extension wildcard
            // (3) Mark empty directories for deletion. Directories cannot be deleted immediately
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
                    if (Dir.Type == TRACKEDAUDIO)
                    {
                        // WAVFileList contains WAV filenames in this directory
                        // Find corresponding FLAC directory and convert to DirectoryInfo format
                        FLACDirName = Dir.ParentBaseName + PERIOD + Dir.Bitrate + PERIOD + FLACF;
                        FLACDirPath = Dir.ParentPath + BACKSLASH + FLACDirName;
                        DirectoryInfo FLACDirInfoPath = new DirectoryInfo(FLACDirPath);
                        if (Directory.Exists(FLACDirPath))
                        {
                            // get all FLAC files in FLAC directory
                            FLACFileList = FLACDirInfoPath.GetFiles(ALLFLAC);
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
                                    Log.WriteLine("*** FLAC Backup file not found for " + wav.Name);
                                    WAVFileBackupExists = false;
                                }
                            }
                            if (WAVFileBackupExists)
                            {
                                // mark tracked audio directory for deletion
                                // these directories will be deleted at the end of program execution
                                DirsMarkedForDeletion.Add(Dir.Path);
                            }
                            else
                            {
                                Log.WriteLine("*** WAV directory will be retained");
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
                }

                // delete extraneous files created by sound editing process
                // "AllFilesList" contains all files in the current directory
                // "FilesToDelete" contain all file types (extensions) to be deleted
                foreach (FileInfo fi in AllFilesList)
                {
                    if (FilesToDelete.Contains(fi.Extension))
                    {
                        Log.WriteLine("  Deleting file: " + fi.Name);
                        DeleteFile(fi.FullName);
                    }
                }

                // add miscellaneous directories to "DirsToDelete" list for deletion
                // these directories will be deleted at the end of program execution
                if (DirsToDelete.Contains(Dir.Name))
                    DirsMarkedForDeletion.Add(Dir.Path);

            } // end Delete Audio section

            // = = = = = = = = Recursion = = = = = = = = 

            // Find all subdirectories of the current directory
            SubDirs = CurrentDir.GetDirectories();
            foreach (DirectoryInfo dirname in SubDirs)
            {
                // extract the subdirectory path
                SubDirPath = SplitDirPath(RootDir, dirname.FullName);
                // log entire subdirectory path below root dir
                Log.WriteLine("Directory: " + SubDirPath);
                // Walk the directory tree for each subdirectory
                try
                {
                    WalkDirectoryTree(dirname);
                }
                catch (Exception)
                {
                    // Log error System.IO.DirectoryNetFoundException
                    Log.WriteLine("*** Directory not found: " + dirname.Name);
                }
            }
        } // end WalkDirectoryTree
    }
}