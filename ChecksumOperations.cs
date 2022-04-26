using System;
using System.IO;
using System.Linq;

namespace AATB
{
    public partial class AATB_Main
    {
        static void CreateMD5ChecksumFile(string MD5FilePath, FileInfo[] FileList, bool WriteLogMessage)
        {
            /* Create new md5 file (overwrite any existing files)
             * Inputs:
             *   MD5FilePath  Pathname of MD5 checksum file
             *   FileList     List of files to be checksummed
             * Calls external programs:
             *   md5sums      utility to create md5 checksums
             *     -e exits immediately after completing (batch mode)
             *     -u switch creates unix md5 checksum format:
             *       <32char md5 checksum> *filename
             * Outputs:
             *   MD5 checksums are written to MD5 checksum file
             */
            string
                MD5Checksum,
                ExternalProgram = "md5sums.exe",
                ExternalArguments;

            if (WriteLogMessage)
                Log.WriteLine("    Creating MD5 checksum file");

            if (FileList != null)
            {
                if (CreateFile(MD5FilePath))
                {
                    // calculate md5sum for each file in list and write to md5 data file
                    foreach (FileInfo fi in FileList)
                    {
                        ExternalArguments = "-e"
                                          + " -u"
                                          + SPACE + DBLQ + fi.FullName + DBLQ;
                        // run external program
                        MD5Checksum = RunProcess(ExternalProgram, ExternalArguments);
                        File.AppendAllText(MD5FilePath, MD5Checksum);
                    }
                }
            }
            else
                Log.WriteLine("    No files found in this directory to create MD5 checksum");
        } // end CreateMD5ChecksumFile

        static void VerifyMD5ChecksumFile(string MD5FilePath, FileInfo[] MD5FileList, FileInfo[] FileList)
        {
            /* Creates a new MD5 checksum file, then compares it with the original file
             * Inputs:
             *   MD5FilePath  Pathname of MD5 checksum file
             *   MD5FileList  List of MD5 checksum files in current directory
             *                only the first one is valid. If multiple entries, none are verified
             *   FileList     List of MD5 checksum files in current directory, non-zero length
             * Outputs:
             *   Errors are written to log and stdout, but not returned
             */
            int
                MD5FileCount;
            string
                NewMD5FilePath,
                ExistingMD5FilePath;

            MD5FileCount = MD5FileList.Count();
            if (MD5FileCount == 1)
            {
                Log.Write("    Verifying MD5 checksum file..");

                // check existing MD5 filename is correct, otherwise rename it
                ExistingMD5FilePath = MD5FileList[0].FullName;
                if (ExistingMD5FilePath != MD5FilePath)
                    MoveFile(ExistingMD5FilePath, MD5FilePath);

                // remove extraneous comments written to MD5 file by non-standard utilities
                CleanChecksumFile(MD5FilePath);

                // Create new MD5 filepath
                NewMD5FilePath = MD5FilePath + PERIOD + NEW;

                // create new MD5 checksum, output to MD5FilePath
                CreateMD5ChecksumFile(NewMD5FilePath, FileList, NoLogMessage);
                //if files are identical, remove the new file and rename the original file
                if (FilesAreEquivalent(MD5FilePath, NewMD5FilePath))
                {
                    DeleteFile(NewMD5FilePath);
                    Log.WriteLine("  OK");
                }
                else
                    Log.WriteLine("\n    Cannot verify MD5 checksums. The new checksum file is:"
                                + "\n      " + NewMD5FilePath);
            }
            else
                Log.WriteLine("    Multiple MD5 checksum files exist, and are not verified");
        } // end VerifyMD5ChecksumFile

        static void CreateFFPChecksumFile(string FFPFilePath, FileInfo[] FLACFileList, bool WriteLogMessage)
        {
            /* Creates FLAC Fingerprint File containing checksums for all input FLAC files
             * Inputs:
             *   FFPFileName   Name of FFP checksum file
             *   FLACFileList  List of FLAC files in current directory
             * Calls external programs:
             *   metaflac      utility to modify metadata in FLAC files
             *     --show-md5sum  creates md5 checksum for each input file
             * Outputs:
             *   FFP checksum file
             */
            string
                FFPData,
                FFPChecksum,
                ExternalProgram = "metaflac.exe",
                ExternalArguments;

            if (WriteLogMessage)
                Log.WriteLine("    Creating FLAC Fingerprint (FFP) file");

            if (FLACFileList != null)
            {
                if (CreateFile(FFPFilePath))
                {
                    foreach (FileInfo fi in FLACFileList)
                    {
                        ExternalArguments = "--show-md5sum"
                                          + SPACE + DBLQ + fi.FullName + DBLQ;
                        // run external program
                        FFPChecksum = RunProcess(ExternalProgram, ExternalArguments);
                        FFPData = (fi.Name + COLON + FFPChecksum);
                        File.AppendAllText(FFPFilePath, FFPData);
                    }
                }
            }
            else
                Log.WriteLine("    No flac files found in this directory to create FFP checksum");
        } // end CreateFFPChecksumFile

        static void VerifyFFPChecksumFile(string FFPFilePath, FileInfo[] FFPFileList, FileInfo[] FLACFileList)
        {
            /* Creates a new FFP checksum file, then compares it with the original file
             * Inputs:
             *   FFPFileName  FFP filename
             *   FFPFileList  List of FFP checksum files in current directory
             *                only the first one is valid. If multiple entries, none are verified
             *   FLACFileList List of FLAC files to verify
             * Calls methods:
             *   CleanChecksumFile  removes extraneous data from the input file
             *   FilesAreEquivalent  returns boolean value for files are equivalent
             *   CreateFFPChecksumFile  creates anffp checksum from input file
             * Outputs:
             *   Errors are written to log and stdout, but not returned
             */
            int FFPFileCount;
            string
                NewFFPFilePath,
                ExistingFFPFilePath;

            FFPFileCount = FFPFileList.Count();
            if (FFPFileCount == 1)
            {
                Log.Write("    Verifying FLAC Fingerprint (FFP) file..");

                // check existing FFP filename is correct, otherwise rename it
                ExistingFFPFilePath = FFPFileList[0].FullName;
                if (ExistingFFPFilePath != FFPFilePath)
                    MoveFile(ExistingFFPFilePath, FFPFilePath);

                // remove extraneous comments written to FFP file by non-standard utilities
                CleanChecksumFile(FFPFilePath);

                // build new FFP filename
                NewFFPFilePath = FFPFilePath + PERIOD + NEW;

                // create new FFP file from FLAC files named FFPFilePath
                CreateFFPChecksumFile(NewFFPFilePath, FLACFileList, NoLogMessage);
                //if files are identical, remove the new file and rename the original file
                if (FilesAreEquivalent(FFPFilePath, NewFFPFilePath))
                {
                    DeleteFile(NewFFPFilePath);
                    Log.WriteLine("  OK");
                }
                else
                    Log.WriteLine("\n    Cannot verify FFP checksums. The new FFP checksum file is:"
                                + "\n      " + NewFFPFilePath);
            }
            else
                Log.WriteLine("    Multiple FFP checksum files exist, and are not verified");
        } // end VerifyFFPChecksumFile

        static void CleanChecksumFile(string InputFilePath)
        {
            /* Checks each line in input checksum text file for validity. If any invalid lines
             * are detected, then the original file is rewritten with clean data.
             * Inputs:
             *   Path of file to be checked
             * Outputs:
             *   If any invalid data is found, the input file is rewritten with "clean" data
             */
            int
                i = 0;
            string[]
                InputDataList,
                OutputDataList;
            bool
                CleanFile = true;

            if (File.Exists(InputFilePath))
            {
                // read input file data into data list
                InputDataList = File.ReadAllLines(InputFilePath);
                // initialize output data list
                OutputDataList = new string[InputDataList.Length];
                if (InputDataList != null)
                {
                    foreach (string li in InputDataList)
                    {
                        // valid lines have at least one period
                        if (li.Contains(PERIOD))
                            OutputDataList[i] = li;
                        else
                            CleanFile = false;
                        i++;
                    }
                    // if any lines are invalid, rewrite file
                    if (!CleanFile)
                    {
                        Log.Write("  Removing invalid data..");
                        if (CreateFile(InputFilePath))
                            File.WriteAllLines(InputFilePath, OutputDataList);
                    }
                }
            }
        } // end CleanChecksumFile
    }
}