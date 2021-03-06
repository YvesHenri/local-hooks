using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace APP
{
    public class HookServer
    {
        private readonly HookCallback callback;
        private readonly NamedPipeServerStream pipe;

        public string Name { get; }

        public HookServer(HookCallback hookCallback)
        {
            var name = Guid.NewGuid().ToString();

            Name = string.Format("\\\\.\\pipe\\{0}", name);
            callback = new HookCallback(hookCallback);
            pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        /// <summary>
        /// Starts the server in asynchronous mode.
        /// </summary>
        public void Start()
        {
            if (!pipe.IsConnected)
            {
                Console.WriteLine("Starting...");
                pipe.BeginWaitForConnection(new AsyncCallback(ConnectionCallback), null);
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            if (pipe.IsConnected)
            {
                try
                {
                    Console.WriteLine("Stopping...");
                    pipe.WaitForPipeDrain();
                }
                finally
                {
                    pipe.Close();
                    pipe.Dispose();
                    Console.WriteLine("Stopped");
                }
            }
        }

        /// <summary>
        /// Dispatches new connections and asynchronously begins reading requests.
        /// </summary>
        private void ConnectionCallback(IAsyncResult asyncResult)
        {
            lock (pipe)
            {
                pipe.EndWaitForConnection(asyncResult);

                if (pipe.IsConnected)
                {
                    var requestSize = Marshal.SizeOf(typeof(HookRequest));
                    var requestBuffer = new byte[requestSize];

                    Console.WriteLine("Connected!");

                    pipe.BeginRead(requestBuffer, 0, requestBuffer.Length, new AsyncCallback(RequestCallback), requestBuffer);
                }
            }
        }

        /// <summary>
        /// Dispatches incoming requests and asynchronously begins writing responses.
        /// </summary>
        private void RequestCallback(IAsyncResult asyncResult)
        {
            lock (pipe)
            {
                var requestActualSize = pipe.EndRead(asyncResult);
                var requestExpectedSize = Marshal.SizeOf(typeof(HookRequest));

                if (pipe.IsConnected)
                {
                    if (requestActualSize == requestExpectedSize)
                    {
                        Console.WriteLine("Request received.");

                        var requestBuffer = asyncResult.AsyncState as byte[];
                        var request = Marshaller.ByteArrayToStruct<HookRequest>(requestBuffer);
                        var response = callback.Invoke(request.Code, request.WParam, request.LParam);
                        var responseBuffer = Marshaller.IntPtrToByteArray(response);

                        pipe.Write(responseBuffer, 0, responseBuffer.Length);
                        pipe.Flush();
                    }

                    var nextRequestBuffer = new byte[requestExpectedSize];

                    pipe.BeginRead(nextRequestBuffer, 0, nextRequestBuffer.Length, new AsyncCallback(RequestCallback), nextRequestBuffer);
                }
            }
        }

        private static class Marshaller
        {
            public static T ByteArrayToStruct<T>(byte[] bytes) where T : struct
            {
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            public static byte[] IntPtrToByteArray(IntPtr pointer)
            {
                return BitConverter.GetBytes(pointer.ToInt32());
            }
        }
    }
}
