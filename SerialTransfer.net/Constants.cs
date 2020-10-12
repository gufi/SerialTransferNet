namespace SerialTransfer.net
{
    public static class Constants
    {
        public static readonly byte STARTBYTE = 0x7E;
        public static readonly byte STOPBYE = 0x81;
        public static readonly int PREAMBLESIZE = 4;
        public static readonly int POSTAMBLESIZE = 2;
        public static readonly int MAXPACKETSIZE = 0xFE;
        public static readonly int NUMOVERHEAD = 6;
    }
}