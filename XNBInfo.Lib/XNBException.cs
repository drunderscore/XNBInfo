using System;

namespace XNBInfo.Lib
{
    public class XNBException : Exception
    {
        internal XNBException() { }
        internal XNBException( string message ) : base( message ) { }
        internal XNBException( string message, Exception innerException ) : base( message, innerException ) { }
    }
}
