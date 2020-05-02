using System;

namespace XNBInfo.Lib
{
    [Flags]
    public enum Flags
    {
        None,
        HiDef = 1,
        Compressed = 128
    }
}
