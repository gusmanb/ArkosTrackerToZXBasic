using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkosTrackerToZXBasic
{
    public class Options
    {
        [Option('s', "song", Separator =',', Required = true, HelpText = "List of songs sepparated by semicolon. The song names must match the output file names.")]
        public string Songs { get; set; }
        [Option('p', "player", Required = true, HelpText = "Type of player (Akg or Akm).")]
        public PlayerTypes Player { get; set;}
        [Option('o', "output", Required = true, HelpText = "Output file name.")]
        public string OutputFile { get; set; }
        [Option('t', "type", Required = true, HelpText = "Output type (Basic or Asm).")]
        public OutputType OutputType { get; set; }
    }

    public enum OutputType
    {
        Basic,
        Asm
    }

    public enum PlayerTypes
    {
        Akg,
        Akm
    }
}
