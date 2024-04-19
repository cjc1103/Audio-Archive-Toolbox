using IniParser;
using IniParser.Model;
using System.Globalization;

namespace AATB
{
    public partial class AATB_Main
    {
        static IniData ReadConfiguration(string ConfigurationFilePath)
        {
            /* read configuration ini file
             * Inputs
             *   ConfigurationFilePath path to configuration file
             * Outputs
             *   ConfigData            data structure containing entire ini file
             */

            var FileIniData = new FileIniDataParser();
            IniData ConfigData = null;

            // set configuration options
            FileIniData.Parser.Configuration.SkipInvalidLines = true;
            // default comment string
            FileIniData.Parser.Configuration.CommentString = "#";

            try
            {
                // Read data from ini file. If file does not exist, returns null
                if (File.Exists(ConfigurationFilePath))
                    ConfigData = FileIniData.ReadFile(ConfigurationFilePath);
            }
            catch (Exception)
            {
                Log.WriteLine("*** Cannot read configuration ini file: " + ConfigurationFilePath);
            }

            return ConfigData;
        }

        static string[] ExpandCommandLineMacros(IniData ConfigData, string[] InputCommandLineList)
        {
            /* expands the command line to include macros defined in the configuration file
             *
             * Inputs
             *   ConfigData          parsed data from configuration ini file
             *   InputCommandLine    command line input as a list
             * Output
             *   ExpandedCommandLineList  command line list after macro substitution
             */

            string arg, opt;
            string ExpandedCommandLine = null;
            string[] ExpandedCommandLineList = null;
            bool KeyFound;

            // method is only valid for non empty input command line
            if (InputCommandLineList.Length > 0)
            {
                if (ConfigData != null)
                {
                    // Get the SectionData from the section "Macros"
                    KeyDataCollection MacroKeys = ConfigData["Macros"];

                    // iterate through all keys in collection
                    // if arg equals key name then return key value
                    foreach (string CommandSubstring in InputCommandLineList)
                    {
                        // Split each substring s into arguments and options, delimited by '='
                        // ignore opt here, as the macro value will be substituted if found
                        (arg, opt) = SplitString(EQUALS, CommandSubstring);
                        KeyFound = false;
                        foreach (KeyData key in MacroKeys)
                        {
                            // if key is found, expand command line with key value
                            if (key.KeyName == arg)
                            {
                                ExpandedCommandLine += key.Value + SPACE;
                                KeyFound = true;
                                break;
                            }
                        }
                        // if key is not found, concatenate substring to expanded command line
                        if (!KeyFound)
                            ExpandedCommandLine += CommandSubstring + SPACE;
                    }
                }
                else
                    // no configuaton data - ini file not found or empty
                    // output will be set to the input command line without substitutions
                    ExpandedCommandLineList = InputCommandLineList;
            }

            // convert expanded command line string to list
            // arguments are separated in command line by a space
            if (ExpandedCommandLine != null)
                ExpandedCommandLineList = ExpandedCommandLine.Split(SPACE, StringSplitOptions.RemoveEmptyEntries);

            return ExpandedCommandLineList;
        }

        static string GetKeyValueFromConfigData(IniData ConfigData, string InitialValue, string section, string name)
        {
            /* search previously read config file data (ConfigData)
             * Inputs
             *   ConfigData          parsed data from configuration ini file
             *   Config file format
             *      [key]
             *      data=value
             * Output
             *   Key value matching input argument key
             */
            string KeyValueFound = null;

            if (ConfigData != null)
            {
                // Get the SectionData
                KeyDataCollection SectionData = ConfigData[section];

                foreach (KeyData k in SectionData)
                {
                    // if key is found, expand command line with key value
                    if (k.KeyName == name)
                    {
                        KeyValueFound = k.Value;
                        break;
                    }
                }
            }

            // if key value not found, then return the initial value
            if (KeyValueFound == null)
                KeyValueFound = InitialValue;

            return KeyValueFound;
        }

        static List<string> GetListFromConfigData(IniData ConfigData, string section)
        {
            /* Retrieve list fron ConifgData under section key
             * Inputs:
             *   ConfigData
             *   key        section name
             *   name       key name
             * Ouputs:
             *   List containing data
             */ 
            List<string> DataList = new List<string>();

            if (ConfigData != null)
            {
                // Get the SectionData
                KeyDataCollection SectionData = ConfigData[section];

                // build list from all ley values in this section
                foreach (KeyData k in SectionData)
                    DataList.Add(k.Value);
            }

            return DataList;
        }

        static void GetIniData(IniData ConfigData)
        {
            /* Get data from configuration file
             * Data was previously read into the ConfigData data structure
             * Search for key data, if found update the item
             * Initial values of these global variables are set in the Program method
             */

            // [Settings]
            // InfoTextFileExtension = <extension>
            // CuesheetFileExtension = <extension>
            INFOTXT = GetKeyValueFromConfigData(ConfigData, INFOTXTdefault, "Settings", "InfotextFileExtension");
            ALLINFOTXT = "*." + INFOTXT;
            INFOCUE = GetKeyValueFromConfigData(ConfigData, INFOCUEdefault, "Settings", "CuesheetFileExtension");
            ALLINFOCUE = "*." + INFOCUE;

            // [FilesToDelete]
            // xx = <extension>
            FilesToDelete = GetListFromConfigData(ConfigData, "FilesToDelete");

            // [DirsToDelete]
            // xx = <directory>
            DirsToDelete = GetListFromConfigData(ConfigData, "DirsToDelete");
        
        }
    }
}
