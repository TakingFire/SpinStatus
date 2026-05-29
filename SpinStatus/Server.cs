using System.Collections.Generic;
using SpinStatus.Model;
using Fleck;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace SpinStatus.Server
{
    internal class Socket(int port) : WebSocketServer($"ws://0.0.0.0:{port}")
    {
        private static readonly List<IWebSocketConnection> _instances = [];
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = [new StringEnumConverter(new CamelCaseNamingStrategy())]
        };

        public void Start()
        {
            FleckLog.Level = LogLevel.Error;
            base.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _instances.Add(socket);

                    socket.Send(JsonConvert.SerializeObject(new Event
                    {
                        Type = Model.EventType.Hello
                    }, _jsonSettings));
                };

                socket.OnClose = () => _instances.Remove(socket);
            });
        }

        public void Stop()
        {
            base.Dispose();
        }

        public static void SendMessage(Event evt)
        {
            string json = JsonConvert.SerializeObject(evt, _jsonSettings);
            foreach (var instance in _instances)
            {
                instance.Send(json);
            }
        }
    }
}
