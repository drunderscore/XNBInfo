using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.IO;
using System.Linq;

namespace XNBInfo.Lib
{
    public class XNBContent
    {
        private static readonly char[] XNB_MAGIC = new char[] { 'X', 'N', 'B' };
        private const string DECOMPRESS_STREAM_NAME = "Microsoft.Xna.Framework.Content.DecompressStream";
        private const string COMPRESSOR_NAME = "Microsoft.Xna.Framework.Content.Pipeline.Interop.Compressor";
        private const string COMPRESSOR_COMPRESS_NAME = "Compress";
        private const string COMPRESSOR_FLUSH_OUTPUT_NAME = "FlushOutput";
        private const int HEADER_SIZE = 3 + 1 + 1 + 1 + 4;
        private const int COMPRESSED_HEADER_SIZE = HEADER_SIZE + 4;

        /// <summary>
        /// The platform this content was compiled for.
        /// </summary>
        public Platform TargetPlatform { get; }
        /// <summary>
        /// The version of XNB this file is formatted in.
        /// </summary>
        public Version FormatVersion { get; }
        /// <summary>
        /// The flags of this content.
        /// </summary>
        public Flags Flags { get; }
        /// <summary>
        /// The size of the content.
        /// </summary>
        public uint Size { get; }

        /// <summary>
        /// Returns whether this content is compressed.
        /// </summary>
        public bool Compressed
        {
            get
            {
                return Flags.HasFlag( Flags.Compressed );
            }
        }

        /// <summary>
        /// Returns whether this content is HiDef or Reach.
        /// </summary>
        public bool HiDef
        {
            get
            {
                return Flags.HasFlag( Flags.HiDef );
            }
        }

        public TypeReader[] Readers { get; }

        public int? TypeID { get; }

        public TypeReader PrimaryAsset
        {
            get
            {
                if ( !TypeID.HasValue )
                    return null;

                return Readers[TypeID.Value];
            }
        }

        public int SharedResourcesCount { get; }

        /// <summary>
        /// Contains the file data. The data held here will always be the uncompressed data.
        /// </summary>
        public byte[] Data { get; }

        public XNBContent( string path )
        {
            if ( path == null )
                throw new ArgumentNullException( "path" );

            if ( !File.Exists( path ) )
                throw new ArgumentException( "The path specified does not exist." );

            XNBBinaryReader reader = null;

            try
            {
                reader = new XNBBinaryReader( File.OpenRead( path ) );
                if ( !reader.ReadChars( 3 ).SequenceEqual( XNB_MAGIC ) )
                    throw new XNBException( "The file specified is not an XNB file." );

                TargetPlatform = (Platform)reader.ReadByte();
                FormatVersion = (Version)reader.ReadByte();
                Flags = (Flags)reader.ReadByte();
                Size = reader.ReadUInt32();

                if ( Compressed )
                {
                    var todoSize = (int)( Size - COMPRESSED_HEADER_SIZE );
                    var decompressedSize = (int)reader.ReadUInt32();
                    var compressed = reader.ReadBytes( todoSize );
                    reader.Dispose();

                    // This is so complicated only because of having to save the data to an array...
                    using ( var compressedStream = new MemoryStream( compressed ) )
                    {
                        var uncompressedStream = DecompressStream( compressedStream, todoSize, decompressedSize );
                        if ( uncompressedStream == null )
                            throw new XNBException( "Unable to decompress the data. Are you missing XNA Game Studio or the XNA Redistributable?" );
                        var uncompressedData = new MemoryStream();

                        uncompressedStream.CopyTo( uncompressedData );
                        uncompressedData.Position = 0;
                        Data = uncompressedData.ToArray();
                        reader = new XNBBinaryReader( uncompressedData );
                    }
                }
                else
                {
                    Data = reader.ReadBytes( (int)( Size - HEADER_SIZE ) );
                    reader.Dispose();
                    reader = new XNBBinaryReader( new MemoryStream( Data ) );
                }

                var readerCnt = reader.Read7BitEncodedInt();
                Readers = new TypeReader[readerCnt];
                for ( int i = 0; i < readerCnt; i++ )
                    Readers[i] = reader.ReadTypeReader();

                SharedResourcesCount = reader.Read7BitEncodedInt();
                var id = reader.Read7BitEncodedInt();
                if ( id != 0 )
                    TypeID = id - 1;
            }
            finally
            {
                reader?.Dispose();
            }
        }

        /// <summary>
        /// Re-writes the parsed content to the supplied stream. This will intentionally save the decompressed version if originally compressed.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="compress">Should the output data be compressed?</param>
        public void Write( Stream stream, bool compress = false )
        {
            using ( var writer = new BinaryWriter( stream ) )
            {
                writer.Write( XNB_MAGIC );
                writer.Write( (byte)TargetPlatform );
                writer.Write( (byte)FormatVersion );
                if ( compress )
                {
                    writer.Write( (byte)( Flags | Flags.Compressed ) );
                    using ( var ms = new MemoryStream() )
                    {
                        if ( !Compress( ms, Data ) )
                            throw new XNBException( "Unable to decompress the data. Are you missing XNA Game Studio or the XNA Redistributable?" );
                        writer.Write( (uint)( COMPRESSED_HEADER_SIZE + ms.Length ) );
                        writer.Write( (uint)Data.Length );
                        writer.Write( ms.ToArray() );
                    }
                }
                else
                {
                    writer.Write( (byte)( Flags & ~Flags.Compressed ) );
                    writer.Write( (uint)( HEADER_SIZE + Data.Length ) );
                    writer.Write( Data );
                }
            }
        }

        private static Stream DecompressStream( Stream stream, int compressedTodo, int decompressedTodo )
        {
            var decompressStreamType = typeof( ContentReader ).Assembly.GetType( DECOMPRESS_STREAM_NAME );
            if ( decompressStreamType == null )
                return null;

            var con = decompressStreamType.GetConstructor( new Type[] { typeof( Stream ), typeof( int ), typeof( int ) } );

            if ( con == null )
                return null;

            var decompressStream = (Stream)con.Invoke( new object[] { stream, compressedTodo, decompressedTodo } );
            return decompressStream;
        }

        private static bool Compress( Stream stream, byte[] data )
        {
            var compressorType = typeof( ContentItem ).Assembly.GetType( COMPRESSOR_NAME );
            if ( compressorType == null )
                return false;

            var con = compressorType.GetConstructor( new Type[] { typeof( Stream ) } );
            var compress = compressorType.GetMethod( COMPRESSOR_COMPRESS_NAME );
            var flush = compressorType.GetMethod( COMPRESSOR_FLUSH_OUTPUT_NAME );

            if ( con == null || compress == null || flush == null )
                return false;

            var compressor = (IDisposable)con.Invoke( new object[] { stream } );
            compress.Invoke( compressor, new object[] { data, data.Length } );
            flush.Invoke( compressor, new object[] { } );
            compressor.Dispose();

            return true;
        }
    }
}
