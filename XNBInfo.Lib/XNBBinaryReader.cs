using System.IO;
using System.Text;

namespace XNBInfo.Lib
{
    internal class XNBBinaryReader : BinaryReader
    {
        public XNBBinaryReader( Stream input ) : base( input ) { }
        public XNBBinaryReader( Stream input, Encoding encoding ) : base( input, encoding ) { }
        public XNBBinaryReader( Stream input, Encoding encoding, bool leaveOpen ) : base( input, encoding, leaveOpen ) { }

        public new int Read7BitEncodedInt()
        {
            return base.Read7BitEncodedInt();
        }

        public TypeReader ReadTypeReader()
        {
            return new TypeReader( ReadString(), ReadInt32() );
        }
    }
}
