using System.Text;

namespace RCON.Core
{
    public enum PacketType
    {
        AUTH = 3,
        AUTH_RESPONSE = 2,
        EXECCOMMAND = 2,
        RESPONSE_VALUE = 0
    }

    public struct Packet
    {
        public int Size { get; set; }

        public int ID { get; set; }

        public PacketType Type { get; set; }

        public string Body { get; set; }

        public Packet()
        {
            ID = 0;
            Type = PacketType.RESPONSE_VALUE;
            Body = "";
            Size = 0;
        }

        public byte[] ToBytes()
        {
            var body = Encoding.ASCII.GetBytes(Body + '\0');
            var buffer = new byte[12 + body.Length + 1];
            var result = buffer.AsSpan();
            body.CopyTo(result[12..]);
            BitConverter.GetBytes(body.Length + 9).CopyTo(result[0..4]);
            BitConverter.GetBytes(ID).CopyTo(result[4..8]);
            BitConverter.GetBytes((int)Type).CopyTo(result[8..12]);
            return result.ToArray();
        }

        public static Packet FromBytes(byte[] buffer)
        {
            return new Packet()
            {
                Size = BitConverter.ToInt32(buffer[..4]),
                ID = BitConverter.ToInt32(buffer[4..8]),
                Type = (PacketType)BitConverter.ToInt32(buffer[8..12]),
                Body = Encoding.ASCII.GetString(buffer[12..])
            };
        }

        public override string ToString()
        {
            return $"Size: {Size}\n" +
                   $"ID:   {ID}\n" +
                   $"Type: {Enum.GetName(Type)}\n" +
                   $"Body: {Body}\n\n";
        }
    }
}
