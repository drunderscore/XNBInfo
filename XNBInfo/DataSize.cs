using System;

namespace XNBInfo
{
    internal struct DataSize
    {
        private static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private readonly long _value;

        private DataSize( long val )
        {
            _value = val;
        }

        public override string ToString()
        {
            return ToString( 2 );
        }

        // thanks to https://stackoverflow.com/a/14488941
        private static string SizeSuffix( long value, int decimalPlaces )
        {
            if ( decimalPlaces < 0 )
                throw new ArgumentOutOfRangeException( "decimalPlaces" );

            if ( value < 0 )
                return "-" + SizeSuffix( -value, decimalPlaces );

            if ( value == 0 )
                return string.Format( "{0:n" + decimalPlaces + "} bytes", 0 );

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log( value, 1024 );

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / ( 1L << ( mag * 10 ) );

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if ( Math.Round( adjustedSize, decimalPlaces ) >= 1000 )
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format( "{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag] );
        }

        public string ToString( int decimals )
        {
            return SizeSuffix( _value, decimals );
        }

        public static implicit operator long( DataSize s ) => s._value;
        public static implicit operator DataSize( long val ) => new DataSize( val );
    }
}
