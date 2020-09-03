using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            private static ConcurrentDictionary<string, List<StateObject>> callbacks = new();
 
            public static void AddSubscription(StateObject o)
            {
                callbacks.AddOrUpdate(o.mailbox, new List<StateObject> { o }, (mailbox, stateObjects) => { stateObjects.Add(o); return stateObjects; });                
            }

            public static void RemoveSubscription(StateObject o)
            {
                callbacks.TryGetValue(o.mailbox, out var stateObjects);

                if (stateObjects.Count == 0)
                    callbacks.TryRemove(o.mailbox, out _);
                else
                    stateObjects.Remove(o);
            }

            public static void OnMessageReceived(string mailbox, MessageSummary summary)
            {
                if (callbacks.TryGetValue(mailbox, out var stateObjects))
                {
                    foreach (var o in stateObjects.ToArray())
                    {
                        try
                        {
                            if (o.handlerSocket.Connected)
                            {
                                var summaryLine = $"{summary.From} {summary.ReceivedTimestamp.ToString("yyyy-MM-dd HH:mm:ss")} {summary.Subject} {summary.Body}";
                                Listener.Send(o.handlerSocket, summaryLine);
                            }
                            else
                            {
                                RemoveSubscription(o);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }

        public class StateObject
        {
            public static readonly int bufferSize = 1024;

            public Socket handlerSocket = null;
            public string mailbox = null;
            public byte[] buffer = new byte[bufferSize];
            public int pos = 0;
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
                Socket handlerSocket = null;
                try
                {
                    var listenSocket = (Socket)ar.AsyncState;                    
                    handlerSocket = listenSocket.EndAccept(ar);

                    var state = new StateObject();
                    state.handlerSocket = handlerSocket;

                    SendInitialPrompt(state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    
                    handlerSocket.Shutdown(SocketShutdown.Both);
                    handlerSocket.Close();
                }
            }

            public static void SendInitialPrompt(StateObject state)
            {
                Send(state.handlerSocket, "Enter mailbox name, or press enter to create new mailbox: ");
                state.pos = 0;
                state.handlerSocket.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(RecieveMailboxCallback), state);
            }

            public static void RecieveMailboxCallback(IAsyncResult ar)
            {
                var state = (StateObject)ar.AsyncState;
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
                                Send(handlerSocket, $"Monitoring mailbox '{state.mailbox}'.\n");
                                handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(CloseConnectionCallback), state);
                                return;
                            }
                            else
                            {
                                Send(handlerSocket, "Error creating mailbox. Goodbye.\n");
                                handlerSocket.Close();
                                return;
                            }
                        }
                        else
                        {
                            if (_engine.MailboxExistsAsync(state.mailbox).GetAwaiter().GetResult())
                            {
                                Send(handlerSocket, $"Monitoring mailbox '{state.mailbox}'.\n");
                                CallbackManager.AddSubscription(state);
                                handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(CloseConnectionCallback), state);
                                return;
                            }
                            else
                            {
                                Send(handlerSocket, $"Mailbox '{state.mailbox}' does not exist.\n");
                                SendInitialPrompt(state);
                                return;
                            }
                        }
                    }
                    
                    handlerSocket.BeginReceive(state.buffer, state.pos, state.buffer.Length - state.pos, SocketFlags.None, new AsyncCallback(RecieveMailboxCallback), state);                    
                }
            }

            public static void CloseConnectionCallback(IAsyncResult ar)
            {
                var state = (StateObject)ar.AsyncState;
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

            public static void Send(Socket handler, string s)
            {
                var buf = Encoding.ASCII.GetBytes(s); 
                handler.BeginSend(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                Socket handlerSocket = null;
                try
                {
                    handlerSocket = (Socket)ar.AsyncState;
                    int bytesSent = handlerSocket.EndSend(ar);

                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                    if (handlerSocket != null)
                    {
                        handlerSocket.Shutdown(SocketShutdown.Both);
                        handlerSocket.Close();
                    }
                }
            }
        }
    }
}
