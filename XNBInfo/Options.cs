using CommandLine;

namespace XNBInfo
{
    internal class Options
    {
        [Option( 'i', "input", Required = true, HelpText = "Input file to parse." )]
        public string Input { get; private set; }

        [Option( 'o', "output", HelpText = "Output file to generate." )]
        public string Output { get; private set; }

        [Option( 'c', "compress", HelpText = "Compress the input file." )]
        public bool Compress { get; private set; }

        [Option( 'u', "uncompress", HelpText = "Uncompress the input file." )]
        public bool Uncompress { get; private set; }
    }
}
