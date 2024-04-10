using IniParser;
using IniParser.Model;

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

            // Read data from ini file. If file does not exist, returns null
            if (File.Exists(ConfigurationFilePath))
                ConfigData = FileIniData.ReadFile(ConfigurationFilePath);

            return ConfigData;
        }

        static string[] ExpandCommandLine(IniData ConfigData, string[] InputCommandLineList)
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
                    // Get the SectionData from the section "macros"
                    // return the definition matching argument, if it exists
                    KeyDataCollection MacroKeys = ConfigData["macros"];

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
    }
}
