using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SerialTransfer.net
{
    public class SerialTransferNet : IDisposable
    {
        private CancellationTokenSource cts;
        private TaskCompletionSource<Packet> _rxTask;
        private Dictionary<byte, Action<Packet>> _callbacks;
        public SerialTransferNet()
        {
            SourceMatchesCurrentSysemEndian = true;
            _callbacks = new Dictionary<byte, Action<Packet>>();
            _rxTask = new TaskCompletionSource<Packet>();
        }
        public bool SourceMatchesCurrentSysemEndian { get; set; }
        public SerialPort SerialPort { get; set; }

        public void Start(string portName, int baudrate)
        {
            if (cts != null && !cts.IsCancellationRequested) Stop();
            cts = new CancellationTokenSource();

            SerialPort = new SerialPort(portName, baudrate);
            SerialPort.Open();
            Task.Run(ConstructPacketAsync, cts.Token);
        }

        private async Task ConstructPacketAsync()
        {
            var packet = new Packet();
            while (!cts.IsCancellationRequested)
            {
                if (!SerialPort.IsOpen)
                {
                    try
                    {
                        SerialPort.Open();
                    }
                    catch (Exception e)
                    {
                        cts.Cancel();
                    }
                }
                
                var buffer = new byte[255];
                var readCount = await SerialPort.BaseStream.ReadAsync(buffer, 0, 255);
                if (readCount > 0)
                {
                    for(int i = 0; i < readCount; i++)
                    {
                        var b = buffer[i];
                        if (!SourceMatchesCurrentSysemEndian)
                            packet.Buffer.Add((byte) IPAddress.NetworkToHostOrder(b));
                        else
                            packet.Buffer.Add(b);

                        if (packet.StartByte != Packet.STARTBYTE)
                        {
                            packet.Buffer.Clear();
                            continue;
                        }

                        if (packet.EndByte == Packet.ENDBYTE && packet.Buffer.Count > 4 && packet.PassCRC)
                        {
                            packet.Unpack();
                            CompletePacketTask(packet);
                            packet = new Packet();
                            _rxTask = new TaskCompletionSource<Packet>();
                        }
                        else if (packet.Buffer.Count > 254)
                        {
                            CompletePacketTask(null);

                            // recovery, probably missed data or serial corruption
                            var lastStart = packet.Buffer.LastIndexOf(Packet.STARTBYTE);
                            if (lastStart <= 0)
                            {
                                packet.Buffer.Clear();
                            }
                            else
                            {
                                packet.Buffer = packet.Buffer.Skip(lastStart).ToList();
                            }

                        }
                    }
                }

                await Task.Delay(1);
            }
        }

        private void CompletePacketTask(Packet packet)
        {
            _rxTask.SetResult(packet);
            if (packet != null && _callbacks.TryGetValue(packet.PacketId, out var callback))
            {
                callback(packet);
            }
        }

        public Task SendAsync<T>(T item, byte packetId = 0) where T : struct
        {
            var data = Packet.Create(item, packetId).Buffer.ToArray();
            return SerialPort.BaseStream.WriteAsync(data, 0, data.Length);
        }


        public async Task<T> ReadAsync<T>() where T: struct
        {
            using(var timeoutcts = new CancellationTokenSource()){

                var data = await Task.WhenAny(_rxTask.Task, Task.Delay(1000,timeoutcts.Token));
                if (data == _rxTask.Task)
                {
                    timeoutcts.Cancel();
                    return ReadPacket<T>(await _rxTask.Task);
                }
                else return default;
            }
        }

        public static T ReadPacket<T>(Packet packet) where T:struct
        {
            if (packet == null) return default;
            T target;
            GCHandle handle = GCHandle.Alloc(packet.Body, GCHandleType.Pinned);
            try
            {
                target = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return target;
        }
        public void AddCallback(byte id, Action<Packet> act)
        {
            _callbacks[id] = act;
        }


        public void Stop()
        {
            if (cts.IsCancellationRequested) return;
            if (!_rxTask.TrySetCanceled()) _rxTask.SetException(new Exception("Unable to cancel SerialTransfer.net shutting down all processes forcibly"));
            cts.Cancel();
            SerialPort.Close();
        }
        public void Dispose()
        {
            Stop();
            SerialPort.Dispose();
        }
    }
}
