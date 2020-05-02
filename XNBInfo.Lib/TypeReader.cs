namespace XNBInfo.Lib
{
    public class TypeReader
    {
        public string AssemblyName { get; }
        public int Version { get; }

        internal TypeReader( string name, int ver )
        {
            AssemblyName = name;
            Version = ver;
        }

        public override string ToString()
        {
            return $"{AssemblyName} (reader version {Version})";
        }
    }
}
