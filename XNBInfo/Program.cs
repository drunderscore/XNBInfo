using CommandLine;
using System;
using System.IO;
using XNBInfo.Lib;

namespace XNBInfo
{
    public class Program
    {
        public static int Main( string[] args )
        {
            Options opts = null;
            Parser.Default.ParseArguments<Options>( args ).WithParsed( o => opts = o );

            if ( opts == null )
                return 1;

            if ( !File.Exists( opts.Input ) )
            {
                Console.Error.WriteLine( "Input file does not exist." );
                return 1;
            }

            if ( opts.Compress && opts.Uncompress )
            {
                Console.Error.WriteLine( "Cannot both compress and uncompress a file." );
                return 1;
            }

            if ( ( opts.Compress || opts.Uncompress ) && string.IsNullOrWhiteSpace( opts.Output ) )
            {
                Console.Error.WriteLine( "Must specify an output to compress or uncompress." );
                return 1;
            }

            var xnb = new XNBContent( opts.Input );

            if ( xnb.Compressed )
            {
                if ( opts.Compress )
                {
                    Console.Error.WriteLine( "Cannot compress already compressed file." );
                    return 1;
                }
            }
            else
            {
                if ( opts.Uncompress )
                {
                    Console.Error.WriteLine( "Cannot uncompress already compressed file." );
                    return 1;
                }
            }

            if ( !opts.Compress && !opts.Uncompress )
            {
                Console.WriteLine( $"Platform: {xnb.TargetPlatform}" );
                Console.WriteLine( $"Version: {xnb.FormatVersion}" );
                Console.WriteLine( $"Flags: {xnb.Flags}" );
                Console.WriteLine( $"Size: {(DataSize)xnb.Size}" );
                Console.WriteLine( $"Readers: {xnb.TargetPlatform}" );
                foreach ( var reader in xnb.Readers )
                    Console.WriteLine( reader );

                Console.WriteLine();

                if ( xnb.PrimaryAsset == null )
                    Console.WriteLine( "Primay asset is null." );
                else
                    Console.WriteLine( $"Primary asset: {xnb.PrimaryAsset}" );
            }
            else
            {
                if ( opts.Compress )
                {
                    using ( var fs = File.OpenWrite( opts.Output ) )
                        xnb.Write( fs, true );
                }
                else if ( opts.Uncompress )
                {
                    using ( var fs = File.OpenWrite( opts.Output ) )
                        xnb.Write( fs, false );
                }
            }

            return 0;
        }
    }
}
