using System;

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

        static bool DeleteDir(string DirPath)
        {
            /* Deletes directory if it exists
             * Inputs:
             *   Directory path
             *   "Force" flag forces recursive deletion of all files and subdirs
             */
            bool Force = true;
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

        static bool MoveFile(string SourceFilePath, string TargetFilePath)
        {
            /* Moves a file to a new location
             * Inputs:
             *   File path
             * Outputs:
             *   File is moved to the destination file path
             * Note: File.Move method overload has boolean flag to overwrite
             *       Uses global flag "Overwrite"
             */
            if (File.Exists(SourceFilePath))
            {
                try
                {
                    if (File.Exists(TargetFilePath) && !Overwrite)
                    {
                        Log.WriteLine("*** Target file exists, use overwrite option to replace");
                    }
                    else
                    {
                        File.Move(SourceFilePath, TargetFilePath, Overwrite);
                        return(true);
                    }
                }
                catch (Exception)
                {
                    Log.WriteLine("*** Unable to move or rename file:" + SourceFilePath);
                }
            }
            else
                Log.WriteLine("*** Source file not found: " + SourceFilePath);

            return (false);
        } // end MoveFile

        static bool CopyFile(string SourceFilePath, string TargetFilePath)
        {
            /* Copies a file to a new location
             * Inputs:
             *   File path
             * Outputs:
             *   File is copied to the destination file path
             * Note: File.Copy method overload has boolean flag to overwrite
             *       Uses global flag "Overwrite"
             */
            if (File.Exists(SourceFilePath))
            {
                try
                {
                    if (File.Exists(TargetFilePath) && !Overwrite)
                    {
                        Log.WriteLine("*** Target file exists, use overwrite option to replace");
                    }
                    else
                    {
                        File.Copy(SourceFilePath, TargetFilePath, Overwrite);
                        return (true);
                    }
                }
                catch (Exception)
                {
                    Log.WriteLine("*** Unable to copy file: " + SourceFilePath);
                }
            }
            else
                Log.WriteLine("*** Source file not found: " + SourceFilePath);

            return (false);
        } // end CopyFile

        static void CopyTextFile(string SourceFilePath, string TargetDirPath)
        {
            /* Copy text file from Sourceinal to target file path
             * Inputs:
             *   Source file path
             *   Destination directory path
             * Outputs:
             *   CopyFile method writes file to the destination directory
             */
            string
                SourceFileName,
                TargetFilePath;

            // build destination file path
            SourceFileName = SplitFileName(SourceFilePath);
            TargetFilePath = TargetDirPath + BACKSLASH + SourceFileName;
            
            // CopyFile method will catch errors
            Log.WriteLine("    Copying information file: " + SourceFileName);
            CopyFile(SourceFilePath, TargetFilePath);

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

        static void RenameWAVFiles(AATB_DirInfo Dir, FileInfo[] FileList)
        {
            /* renames all files in File list to the names embedded in the infotext file
             * if no infotext file exists then return error
             * Input
             *   Dir Class
             *      InfotextPath    *Must* have a set list with one name for each file
             *      TitleList       Previously populated by GetTrackMetadatafromInfotext method
             *   Filelist               List of files
             * Outputs
             *   Files are renamed, extension remains the same
             */

            int 
                FileListCount,
                TitleListCount,
                TrackNumber;
            string
                Path, FileName,
                RootFileName, Extension,
                NewFileName, NewFilePath,
                TrackNumberStr;

            // WAVFile list must contain the same number of files as exists in Dir.TitleList
            FileListCount = FileList.Count();
            TitleListCount = Dir.TitleList.Count();
            if (FileListCount == TitleListCount)
            {
                TrackNumber = 0;
                foreach (FileInfo fi in FileList)
                {
                    // increment track number and convert to two place string
                    TrackNumber++;
                    TrackNumberStr = TrackNumber.ToString("00");
                    // split file name in root, extension
                    (Path, FileName) = SplitFilePath(fi.FullName);
                    (RootFileName, Extension) = SplitString(FileName, PERIOD);
                    //build new file name, number prefic has to added back
                    NewFileName = TrackNumberStr + SPACE + Dir.TitleList[TrackNumber - 1] + PERIOD + Extension;
                    NewFilePath = Path + BACKSLASH + NewFileName;
                    Log.WriteLine("  Renaming " + fi.Name + " ==> " + NewFileName);
                    // rename file
                    MoveFile(fi.FullName, NewFilePath);
                }
            }
            else
                Log.WriteLine("*** Track list (" + TitleListCount + ")"
                            + " not equal to WAV files (" + FileListCount + ")");

        } // end RenameWAVFiles
    }
}