# SerialTransferNet
## Serial Transfer protocol implementation under .net

Arduino based implementation [SerialTransfer](https://github.com/PowerBroker2/SerialTransfer)

This library is very much in a very aplha state and does only the very basic of functionality
### What works
- [x] Basic transfer and receipt of structured data
- [x] Id based messages via callback ``` SerialTransferNet.AddCallback(id,Action<Packet> action) ``` the packet can be unpacked using the static method ``` Packet.ReadPacket<T>(packet) ```
- [ ] Multiple packet sized transfers ( anything over max packet len such as files and long strings  )



### Example simplistic usage i have used for testing
```csharp

class Program
    {
        static async Task Main(string[] args)
        {

            using (var ser = new SerialTransferNet())
            {
                ser.Start("COM3",115200);
                var cts = new CancellationTokenSource();
                int i = 1;
#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    do
                    {
                        await ser.SendAsync((byte)i);
                        var data = await ser.ReadAsync<float>();
                        switch (i)
                        {
                            case 1:
                                System.Console.SetCursorPosition(0, 0);
                                System.Console.WriteLine($"Temp     : {data:F3} C {data*(9.0/5.0)+32:F3} F");
                                break;
                            case 2:
                                System.Console.WriteLine($"Pres     : {data:F3} hpa");
                                break;
                            case 3:
                                System.Console.WriteLine($"Humidity : {data:F3} %");
                                break;
                            case 4:
                                System.Console.WriteLine($"Alt      : {data:F3} meters");
                                break;
                        }
                        i += 1;
                        await Task.Delay(1);
                        if (i > 4) i = 1;
                    } while (!cts.IsCancellationRequested);
                });
                System.Console.ReadKey();
                cts.Cancel();
            }
        }
    }
```
