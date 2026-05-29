using System.Collections.Generic;
using SpinStatus.Model;
using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace SpinStatus.Server
{
    public class ServerBehavior : WebSocketBehavior
    {
        private static readonly List<ServerBehavior> _instances = [];
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = [new StringEnumConverter(new CamelCaseNamingStrategy())]
        };

        protected override void OnOpen()
        {
            base.OnOpen();
            _instances.Add(this);

            var helloEvent = new Event
            {
                Type = Model.EventType.Hello
            };

            this.SendAsync(JsonConvert.SerializeObject(helloEvent, _jsonSettings), null);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _instances.Remove(this);
            base.OnClose(e);
        }

        internal static void SendMessage(Event evt)
        {
            string json = JsonConvert.SerializeObject(evt, _jsonSettings);
            foreach (var instance in _instances)
            {
                instance.SendAsync(json, null);
            }
        }
    }
}
