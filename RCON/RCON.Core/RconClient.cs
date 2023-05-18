using NLog;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

namespace RCON.Core
{
    public delegate T RconFormmater<T>(string command, int ID, List<Packet> result);

    public class RconClient
    {
        private TcpClient _client;
        private Pipe _pipe;
        private List<Command> _commands;
        private int _currentCommand;

        private Logger _logger;

        public RconClient()
        {
            _client = new TcpClient();
            _pipe = new Pipe();
            _logger = LogManager.GetCurrentClassLogger();
            _commands = new List<Command>();
        }

        ~RconClient()
        {
            LogManager.Shutdown();
        }

        public async Task ConnectAsync(string ip, int port)
        {
            try
            {
                await _client.ConnectAsync(ip, port);

                var fillTask = FillPipeAsync(_pipe.Writer);
                var readTask = ReadPipeAsync(_pipe.Reader);
                _ = Task.WhenAll(fillTask, readTask).ContinueWith(x =>
                {
                    _pipe.Reset();
                });
            }
            catch (Exception e)
            {
                _logger.Error("ConnectAsync() failed. {ip} {port} {ExceptionMessage}", ip, port, e.Message);
                throw;
            }
        }

        public void Close()
        {
            try
            {
                _client.Close();
            }
            catch (Exception e)
            {
                _logger.Error("Close() failed. {ExceptionMessage}", e.Message);
                throw;
            }
        }

        public async Task<List<Packet>> ExecCommandAsync(string command, int ID, int timeout = 5000)
        {
            var packet = new Packet();
            packet.Body = command;
            packet.ID = ID;
            packet.Type = PacketType.EXECCOMMAND;
            await SendPacketAsync(packet);
            return await GetResponse(ID, timeout);
        }

        public async Task<T> ExecCommandAsync<T>(string command, int ID, RconFormmater<T> formmater, int timeout = 5000)
        {
            var res = await ExecCommandAsync(command, ID, timeout);
            return formmater(command, ID, res);
        }

        // 100 is reserved ID for Auth
        public async Task<bool> AuthAsync(string password, int timeout = 5000)
        {
            var packet = new Packet();
            packet.ID = 100;
            packet.Type = PacketType.AUTH;
            packet.Body = password;
            await SendPacketAsync(packet);

            var res = await GetResponse(packet.ID, timeout);
            if (res == null)
                return false;
            return true;
        }

        private async Task<List<Packet>> GetResponse(int ID, int timeout)
        {
            int waited = 0;
            Command command;
            while (waited < timeout)
            {
                command = _commands.Find(x => x.ID == ID);
                if (command.Status == CommandStatus.RECEIVED)
                {
                    _commands.Remove(command);
                    return command.Response;
                }
                await Task.Delay(100);
                waited += 100;
            }

            command = _commands.Find(x => x.ID == ID);
            _commands.Remove(command);
            return null;
        }

        private async Task SendPacketAsync(Packet packet)
        {
            try
            {
                var ns = _client.GetStream();
                await ns.WriteAsync(packet.ToBytes());
                _commands.Add(new Command { ID = packet.ID });
            }
            catch (Exception e)
            {
                _logger.Error("SendPacketAsync() failed. {Packet} {ExceptionMessage}", packet, e.Message);
                throw;
            }
        }

        private async Task FillPipeAsync(PipeWriter writer)
        {

            while (true)
            {
                // 14 is minimum size for any packet
                var buffer = writer.GetMemory(14);

                var bytesReaded = await _client.GetStream().ReadAsync(buffer);
                if (bytesReaded == 0) // Connection closed
                    break;

                writer.Advance(bytesReaded);

                var flush = await writer.FlushAsync();
                if (flush.IsCompleted) // True if PipeReader no longer reading
                    break;
            }
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                if (buffer.Length < 4) // Packet is not completed
                {
                    if (result.IsCompleted)
                        break;

                    reader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var size = BitConverter.ToInt32(buffer.Slice(buffer.Start, 4).ToArray());
                if (buffer.Length == size + 4) // Checking whether the size of the package corresponds to the declared one
                {
                    var packetEnd = buffer.GetPosition(size + 4);
                    var packet = Packet.FromBytes(buffer.Slice(buffer.Start, packetEnd).ToArray());

                    if (_commands.Any(x => x.ID == packet.ID))
                    {
                        _currentCommand = packet.ID;
                        var index = _commands.FindIndex(x => x.ID == packet.ID);
                        _commands[index].Response.Add(packet);
                        _commands[index].Status = CommandStatus.RECEIVED;
                    }
                    else
                    {
                        _logger.Info("The package with an unknown ID has arrived. {Packet}", packet);
                    }

                    reader.AdvanceTo(packetEnd);
                }
                else
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (buffer.IsEmpty && result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync();
        }
    }
}
