namespace SerialTransfer.net
{
    public enum PacketStatus
    {
        Continue = 3,
        NewData = 2,
        NoData = 1,
        CRCError = 0,
        PayloadError = -1,
        StopByteError = -2
    }
}