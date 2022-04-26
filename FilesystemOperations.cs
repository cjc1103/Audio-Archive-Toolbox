using System;
using System.IO;
using System.Linq;

namespace AATB
{
    public partial class AATB_Main
    {
        static bool CreateDir(string DirPath)
        {
            /* Creates directory if it does not exist
             * Inputs:
             *   Directory path
             * Outputs:
             *   Directory is created
             */
            if (DirPath != null) try
            {
                Directory.CreateDirectory(DirPath);
                return true;
            }
            catch (Exception)
            {
                Log.WriteLine("*** Unable to create directory: " + DirPath);
            }
            return false;
        } // end CreateDir

        static bool DeleteDir(string DirPath, bool Force)
        {
            /* Deletes directory if it exists
             * Inputs:
             *   Directory path
             *   "Force" flag forces recursive deletion of all files and subdirs
             */
            if (DirPath != null) try
            {
                Directory.Delete(DirPath, Force);
                return true;
            }
            catch (Exception)
            {
                Log.WriteLine("*** Unable to delete directory: " + DirPath);
            }
            return false;
        } // end DeleteDir

        static bool CreateFile(string FilePath)
        {
            /* Creates a file in the desired location
             * Inputs:
             *   File path
             * Outputs:
             *   File is created
             */
            if (FilePath != null) try
            {
                DeleteFile(FilePath);
                var file = File.Create(FilePath);
                file.Close();
                return true;
            }
            catch (Exception)
            {
                Log.WriteLine("*** Unable to create file: " + FilePath);
            }
            return false;
        } // end CreateFile

        static void DeleteFile(string FilePath)
        {
            /* Deletes a file, if it exists and the user has access
             * Inputs:
             *   File path
             * Outputs:
             *   File is deleted
             */
            if (File.Exists(FilePath)) try
            {
                File.Delete(FilePath);
            }
            catch (Exception)
            {
                Log.WriteLine("*** Unable to delete file: " + FilePath);
            }
        } // end DeleteFile

        static void MoveFile(string OrigFilePath, string DestFilePath)
        {
            /* Moves a file to a new location
             * Inputs:
             *   File path
             * Outputs:
             *   File is moved to the destination file path, original file is deleted
             * Note: File.Move method requires deleting existing file to overwrite
             */
            if (File.Exists(OrigFilePath))
            {
                try
                {
                    if (File.Exists(DestFilePath) && !Overwrite)
                    {
                        Log.WriteLine("*** Destination file exists, use overwrite option to replace");
                        Log.WriteLine("    " + DestFilePath);
                    }
                    else if (!File.Exists(DestFilePath)
                            || File.Exists(DestFilePath) && Overwrite)
                    {
                        File.Delete(DestFilePath);
                        File.Move(OrigFilePath, DestFilePath);
                    }
                }
                catch (Exception)
                {
                    Log.WriteLine("*** Unable to move or rename file");
                    Log.WriteLine("    From: " + OrigFilePath);
                    Log.WriteLine("    To:   " + DestFilePath);
                }
            }
        } // end MoveFile

        static void CopyFile(string OrigFilePath, string DestFilePath)
        {
            /* Copies a file to a new location
             * Inputs:
             *   File path
             * Outputs:
             *   File is copied to the destination file path
             * Note: File.Move method requires deleting existing file to overwrite
             */
            if (File.Exists(OrigFilePath)
                && (OrigFilePath != DestFilePath))
            {
                try
                {
                    if (File.Exists(DestFilePath) && !Overwrite)
                    {
                        Log.WriteLine("*** Destination file exists, use overwrite option to replace");
                        Log.WriteLine("    " + DestFilePath);
                    }
                    else if (!File.Exists(DestFilePath)
                            || File.Exists(DestFilePath) && Overwrite)
                    {
                        File.Delete(DestFilePath);
                        File.Copy(OrigFilePath, DestFilePath);
                    }
                }
                catch (Exception)
                {
                    Log.WriteLine("*** Unable to copy file");
                    Log.WriteLine("    From: " + OrigFilePath);
                    Log.WriteLine("    To:   " + DestFilePath);
                }
            }
        } // end CopyFile

        static void CopyTextFile(string SourceFilePath, string DestDirPath)
        {
            /* Copy text file from source to destination
             * Inputs:
             *   Source file path
             *   Destination directory path
             * Outputs:
             *   CopyFile method writes file to the destination directory
             */
            string
                FilePath,
                SourceFileName,
                DestFilePath;

            // build destination file path
            (FilePath, SourceFileName) = SplitPath(SourceFilePath);
            DestFilePath = DestDirPath + BACKSLASH + SourceFileName;
            
            // CopyFile method will catch errors
            Log.WriteLine("    Copying information file " + SourceFileName);
            CopyFile(SourceFilePath, DestFilePath);

        } // end CopyTextFile

        static string[] ReadTextFile(string FilePath)
        {
            /* Opens a text file, if it exists, and reads the contents into a string array
             * Inputs:
             *   File path
             * Outputs:
             *   Text embedded in file
             */
            string[]
                Data = null;
            try
            {
                Data = File.ReadAllLines(FilePath);
            }
            catch (Exception)
            {
                Log.WriteLine("*** Unable to read file: " + FilePath);
            }
            return Data;
        } // end ReadTextFile

        static bool FilesAreEquivalent(string FilePath1, string FilePath2)
        {
            /* Checks if input files are equivalent
             * Inputs:
             *   File path 1, File path 2
             * Outputs:
             *   Boolean value representing equivalency
             */
            string[]
                Data1 = null,
                Data2 = null;

            // read data from both input files into lists
            // ReadTextFile method will catch errors
            if (File.Exists(FilePath1)
                && File.Exists(FilePath2))
            {
                Data1 = ReadTextFile(FilePath1);
                Data2 = ReadTextFile(FilePath2);
            }
            // compare both data lists and return true if they are identical
            return (Data1.SequenceEqual(Data2));
        } // end FilesAreEquivalent

        static bool BaseNamesAreEqual(string Filename1, string Filename2)
        {
            /* Checks if input filenames are equal
             * Inputs:
             *   File name 1, File name 2
             * Outputs:
             *   Boolean value representing equivalency
             */
            string
                BaseName1,
                BaseName2,
                Extension1,
                Extension2;

            // extract basename from each input string
            (BaseName1, Extension1) = SplitString(Filename1, PERIOD);
            (BaseName2, Extension2) = SplitString(Filename2, PERIOD);

            // compare both basenames and return true if they are equal
            return (BaseName1 == BaseName2);
        } // end BaseNamesAreEqual
    }
}