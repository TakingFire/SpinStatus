using System.Collections.Generic;
using SpinStatus.Model;
using Fleck;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace SpinStatus
{
    internal static class Server
    {
        private static WebSocketServer _server;
        private static readonly List<IWebSocketConnection> _instances = [];
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = [new StringEnumConverter(new CamelCaseNamingStrategy())]
        };

        public static void Start(int port)
        {
            if (_server != null) { return; }
            FleckLog.Level = LogLevel.Error;

            _server = new WebSocketServer($"ws://0.0.0.0:{port}");

            _server.Start(instance =>
            {
                instance.OnOpen = () =>
                {
                    _instances.Add(instance);

                    instance.Send(JsonConvert.SerializeObject(new Event
                    {
                        Type = EventType.Hello
                    }, _jsonSettings));
                };

                instance.OnClose = () => _instances.Remove(instance);
            });

            Plugin.Logger.LogInfo($"Server started on port {port}");
        }

        public static void Stop()
        {
            Plugin.Logger.LogInfo($"Server shutting down");

            if (_server == null) { return; }
            foreach (var instance in _instances)
            {
                instance.Close();
            }
            _server.Dispose();
            _server = null;
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
