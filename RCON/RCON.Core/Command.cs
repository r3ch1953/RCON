namespace RCON.Core
{
    internal enum CommandStatus
    {
        RECEIVED, // All packages have been received
        NOT_RECEIVED, // Package not received
    }

    internal class Command
    {
        public int ID { get; set; }

        public List<Packet> Response { get; set; }

        public CommandStatus Status { get; set; }

        public Command()
        {
            ID = 0;
            Response = new List<Packet>();
            Status = CommandStatus.NOT_RECEIVED;
        }
    }
}
