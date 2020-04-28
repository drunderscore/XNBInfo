using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Linq;

namespace XNBInfo
{
    public class Program
    {
        public static readonly char[] XNB_MAGIC = new char[] { 'X', 'N', 'B' };
        public const string DECOMPRESS_STREAM_NAME = "Microsoft.Xna.Framework.Content.DecompressStream";

        public static Stream DecompressStream( Stream stream, int compressedTodo, int decompressedTodo )
        {
            Type decompressStreamType = null;
            foreach ( var t in typeof( ContentReader ).Assembly.GetTypes() )
            {
                if ( t.FullName == DECOMPRESS_STREAM_NAME )
                {
                    decompressStreamType = t;
                    break;
                }
            }

            if ( decompressStreamType == null )
                return null;

            var con = decompressStreamType.GetConstructor( new Type[] { typeof( Stream ), typeof( int ), typeof( int ) } );
            Stream decompressStream = (Stream)con.Invoke( new object[] { stream, compressedTodo, decompressedTodo } );
            return decompressStream;
        }

        public static int Main( string[] args )
        {
            if ( args.Length < 1 )
            {
                Console.Error.WriteLine( "Supply a path to an XNB file as arguments." );
                return 1;
            }

            string path = string.Join( " ", args );

            if ( !File.Exists( path ) )
            {
                Console.Error.WriteLine( "Input file does not exist." );
                return 1;
            }

            XNBBinaryReader reader = null;
            try
            {
                reader = new XNBBinaryReader( File.OpenRead( path ) );
                if ( !reader.ReadChars( 3 ).SequenceEqual( XNB_MAGIC ) )
                {
                    Console.Error.WriteLine( "Input file is not an XNB file (magic doesn't match)" );
                    return 1;
                }

                var plat = (Platform)reader.ReadByte();
                Console.WriteLine( $"Platform: {plat}" );
                var version = (Version)reader.ReadByte();
                Console.WriteLine( $"Version: {version}" );
                var flags = (Flags)reader.ReadByte();
                Console.WriteLine( $"Flags: {flags}" );
                var dataLen = (DataSize)reader.ReadUInt32();
                Console.WriteLine( $"Size: {dataLen}" );
                if ( dataLen != reader.BaseStream.Length )
                    Console.WriteLine( "Size doesn't match file?" );

                if ( flags.HasFlag( Flags.Compressed ) )
                {
                    var decompressedSize = (DataSize)reader.ReadUInt32();
                    Console.WriteLine( $"Decompressed size: {decompressedSize}" );
                    Stream decompressedStream;
                    if ( ( decompressedStream = DecompressStream( reader.BaseStream, (int)( dataLen - 3 - 1 - 1 - 1 - 4 - 4 ), (int)decompressedSize ) ) == null )
                    {
                        Console.Error.WriteLine( "Couldn't decompress stream." );
                        return 1;
                    }
                    reader = new XNBBinaryReader( decompressedStream );
                }

                var readerCnt = reader.Read7BitEncodedInt();
                Console.WriteLine( $"Type readers ({readerCnt}):" );
                var readers = new TypeReader[readerCnt];
                for ( int i = 0; i < readerCnt; i++ )
                {
                    readers[i] = reader.ReadTypeReader();
                    Console.WriteLine( readers[i] );
                }

                Console.WriteLine();

                var sharedResources = reader.Read7BitEncodedInt();
                var typeId = reader.Read7BitEncodedInt();
                if ( typeId == 0 )
                    Console.WriteLine( "The data is null." );
                else
                {
                    typeId -= 1;
                    if ( typeId > readers.Length )
                        Console.WriteLine( $"Primary asset has weird type id {typeId} (cannot find in read table)" );
                    else
                        Console.WriteLine( $"Primary asset: {readers[typeId]}" );
                }

                Console.WriteLine( $"Shared resources: {sharedResources}" );
            }
            finally
            {
                reader?.Dispose();
            }

            return 0;
        }
    }
}
