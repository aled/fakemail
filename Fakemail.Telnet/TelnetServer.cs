using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Models;

namespace Fakemail.Telnet
{
    public class TelnetServer
    {
        private static IEngine _engine;

        public async Task StartAsync(IEngine engine, CancellationToken cancellationToken)
        {
            _engine = engine;
            _engine.AddSubscription((mailbox, summary) => CallbackManager.OnMessageReceived(mailbox, summary));

            var task = Task.Run(() => Listener.StartListening());

            cancellationToken.Register(() => Listener.StopListening());

            await task;
        }

        // store a Dictionary of mailbox => List<StateObject>
        class CallbackManager
        {
            private static ConcurrentDictionary<string, List<SocketState>> callbacks = new ConcurrentDictionary<string, List<SocketState>>();
            private static Timer KeepAliveTimer = new Timer(OnTick);
            private static int symbol = 0;

            static CallbackManager()
            {
                KeepAliveTimer.Change(0, Timeout.Infinite);
            }


            private static void OnTick(object timerState)
            {
                KeepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);

                byte[] symbols = new[] { (byte)'\\', (byte)'|', (byte)'/', (byte)'-' };

                foreach (var socketStateList in callbacks.Values)
                {
                    foreach (var socketState in socketStateList)
                    {
                        try
                        {
                            var sequence = new List<byte>();
                            
                            sequence.Add(0x1b); // esc
                            sequence.Add(0x5b); // [
                            sequence.Add(0x39); // 9
                            sequence.Add(0x39); // 9
                            sequence.Add(0x39); // 9
                            sequence.Add(0x44); // D
                            sequence.Add(symbols[symbol++]);

                            socketState.handlerSocket.BeginSend(sequence.ToArray(), 0, sequence.Count, SocketFlags.None, new AsyncCallback(Listener.SendCallback), socketState);
                        }
                        catch (Exception e)
                        {
                            socketState.CloseHandlerSocket();
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                symbol %= symbols.Length;
                KeepAliveTimer.Change(250, Timeout.Infinite);
            }

            public static void AddSubscription(SocketState state)
            {
                callbacks.AddOrUpdate(state.mailbox, new List<SocketState> { state }, (mailbox, socketStates) => { socketStates.Add(state); return socketStates; });

                // get 10 most recent emails
                Task.Run(async () =>
                {
                    var summaries = await _engine.GetMessageSummaries(state.mailbox, 0, 10);
                    foreach (var summary in summaries.OrderBy(x => x.ReceivedTimestamp))
                        SendSummary(state, summary);
                });       
            }

            public static void RemoveSubscription(SocketState o)
            {
                if (callbacks.TryGetValue(o.mailbox, out var stateObjects))
                {
                    if (stateObjects.Count == 0)
                        callbacks.TryRemove(o.mailbox, out _);
                    else
                        stateObjects.Remove(o);
                }
            }

            private static void SendSummary(SocketState state, MessageSummary summary)
            {
                try
                {
                    if (state.handlerSocket.Connected)
                    {
                        var summaryLine = $"{summary.From} | {summary.ReceivedTimestamp:yyyy-MM-dd HH:mm:ss} | {summary.Subject} | {summary.Body}";

                        if (!summaryLine.EndsWith("\n"))
                            summaryLine += "\n";
                        
                        Listener.Send(state, summaryLine);
                    }
                    else
                    {
                        RemoveSubscription(state);
                    }
                }
                catch (Exception e)
                {
                    RemoveSubscription(state);
                    Console.WriteLine(e.Message);
                }

            }

            public static void OnMessageReceived(string mailbox, MessageSummary summary)
            {
                if (callbacks.TryGetValue(mailbox, out var socketStates))
                {
                    foreach (var state in socketStates.ToArray())
                    {
                        SendSummary(state, summary);
                    }
                }
            }
        }

        public class SocketState
        {
            public static readonly int bufferSize = 1024;

            public Socket handlerSocket = null;
            public string mailbox = null;
            public byte[] buffer = new byte[bufferSize];
            public int pos = 0;

            public void CloseHandlerSocket()
            {
                if (handlerSocket != null)
                {
                    CallbackManager.RemoveSubscription(this);

                    try { 
                        handlerSocket.Shutdown(SocketShutdown.Both); 
                    } 
                    catch (Exception) { }

                    try
                    {
                        handlerSocket.Close();
                    }
                    catch (Exception) { }
                }
            }
        }

        public class Listener
        {
            public static ManualResetEvent connectEvent = new ManualResetEvent(false);
            static Socket listenSocket;

            public Listener()
            {
            }

            public static void StopListening()
            {
                try
                {
                    if (listenSocket != null)
                        listenSocket.Close();
                }
                catch  (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static void StartListening()
            {
                var ipAddress = IPAddress.Any;
                var localEndPoint = new IPEndPoint(ipAddress, 12041);

                listenSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
 
                try
                {
                    listenSocket.Bind(localEndPoint);
                    listenSocket.Listen(50);

                    while (true)
                    {
                        connectEvent.Reset();
                        listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenSocket);
                        connectEvent.WaitOne();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    StopListening();
                }
            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                connectEvent.Set();
                
                var state = new SocketState();
                try
                {
                    var listenSocket = (Socket)ar.AsyncState;
                    state.handlerSocket = listenSocket.EndAccept(ar);
                    SendInitialPrompt(state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    state?.CloseHandlerSocket();
                }
            }

            public static void SendInitialPrompt(SocketState state)
            {
                try
                {
                    Send(state, "Enter mailbox name, or press enter to create new mailbox: ");
                    state.pos = 0;
                    state.handlerSocket.BeginReceive(state.buffer, 0, SocketState.bufferSize, 0, new AsyncCallback(RecieveMailboxCallback), state);
                }
                catch (Exception e)
                {
                    state?.CloseHandlerSocket();
                    Console.WriteLine(e.Message);
                }
            }

            public static void RecieveMailboxCallback(IAsyncResult ar)
            {
                SocketState state = null;
                try
                {
                    state = (SocketState)ar.AsyncState;
                    var handlerSocket = state.handlerSocket;
                    int bytesRead = handlerSocket.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (state.buffer[state.pos] == '\n' || state.buffer[state.pos] == '\r')
                            {
                                state.mailbox = Encoding.ASCII.GetString(state.buffer, 0, state.pos);
                                break;
                            }
                            state.pos++;
                        }

                        if (state.mailbox != null)
                        {
                            if (state.mailbox == string.Empty)
                            {
                                // create new mailbox.
                                var result = _engine.CreateMailboxAsync().GetAwaiter().GetResult();

                                if (result.Success)
                                {
                                    state.mailbox = result.Mailbox;
                                    Send(state, $"Monitoring mailbox '{state.mailbox}'.\n");
                                    CallbackManager.AddSubscription(state);
                                    handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(CloseConnectionCallback), state);
                                    return;
                                }
                                else
                                {
                                    Send(state, "Error creating mailbox. Goodbye.\n");
                                    handlerSocket.Close();
                                    return;
                                }
                            }
                            else
                            {
                                if (_engine.MailboxExistsAsync(state.mailbox).GetAwaiter().GetResult())
                                {
                                    Send(state, $"Monitoring mailbox '{state.mailbox}'.\n");
                                    CallbackManager.AddSubscription(state);
                                    handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(CloseConnectionCallback), state);
                                    return;
                                }
                                else
                                {
                                    Send(state, $"Mailbox '{state.mailbox}' does not exist.\n");
                                    SendInitialPrompt(state);
                                    return;
                                }
                            }
                        }

                        handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(RecieveMailboxCallback), state);
                    }
                }
                catch (Exception e)
                {
                    state?.CloseHandlerSocket();
                    Console.WriteLine(e.Message);
                }
            }

            public static void CloseConnectionCallback(IAsyncResult ar)
            {
                SocketState state = null;
                try
                {
                    state = (SocketState)ar.AsyncState;
                    var handlerSocket = state.handlerSocket;
                    int bytesRead = handlerSocket.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        state.pos += bytesRead;

                        for (int i = 0; i < state.pos - 4; i++)
                        {
                            // Immediately close the connection if ctrl-c received from client
                            // Note this is very far from the official telnet protocol.
                            if (state.buffer[i] == 255 && state.buffer[i + 1] == 244 && state.buffer[i + 2] == 255 && state.buffer[i + 3] == 253)
                            {
                                Console.WriteLine("Received ctrl-c; closing socket");
                                handlerSocket.Close();
                                CallbackManager.RemoveSubscription(state);
                                return;
                            }
                        }
                        Console.WriteLine("Discarding received data: " + Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    }

                    state.pos = 0;
                    handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length, SocketFlags.None, new AsyncCallback(CloseConnectionCallback), state);
                }
                catch (Exception e)
                {
                    state?.CloseHandlerSocket();
                    Console.WriteLine(e.Message);
                }
            }

            public static void Send(SocketState state, string s)
            {
                var buf = Encoding.ASCII.GetBytes(s);
                state.handlerSocket.BeginSend(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(SendCallback), state);                
            }

            public static void SendCallback(IAsyncResult ar)
            {
                SocketState state = null;
                try
                {
                    state = (SocketState)ar.AsyncState;
                    int bytesSent = state.handlerSocket.EndSend(ar);

                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                }
                catch (Exception e)
                {
                    state?.CloseHandlerSocket();
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
