using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BusDriver.UI;
using BusDriver.Utils;
using SimpleJSON;

namespace BusDriver.ValuesSource
{
    public class UdpValuesSource : AbstractValuesSource
    {
        private UdpClient _server;
        private IPEndPoint _receiveEndpoint;

        private UITextInput PortInput;
        private JSONStorableString PortText;

        private JSONStorableAction StartUdpAction;
        private JSONStorableAction StopUdpAction;

        public UdpValuesSource()
        {
            _receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            PortInput = builder.CreateTextInput("ValuesSource:Udp:Port", "Port", "8889", 50);
            PortText = PortInput.storable;

            StartUdpAction = UIManager.CreateAction("Start Udp", Start);
            StopUdpAction = UIManager.CreateAction("Stop Udp", Stop);
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(PortInput);
            base.DestroyUI(builder);

            UIManager.RemoveAction(StartUdpAction);
            UIManager.RemoveAction(StopUdpAction);
        }

        public override void RestoreConfig(JSONNode config)
        {
            base.RestoreConfig(config);
            config.Restore(PortText);
        }

        public override void StoreConfig(JSONNode config)
        {
            base.StoreConfig(config);
            config.Store(PortText);
        }

        protected override void Start()
        {
            if (_server != null)
                return;

            var port = 0;
            if (!int.TryParse(PortText.val, out port))
            {
                SuperController.LogMessage($"Failed to parse port number from: {PortText.val}");
                return;
            }

            try
            {
                _server = new UdpClient();
                _server.ExclusiveAddressUse = false;
                _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _server.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                _server.BeginReceive(Receive, _server);

                SuperController.LogMessage($"Upd started on port: {port}");
            }
            catch(Exception e)
            {
                SuperController.LogError(e.ToString());
                Stop();
            }
        }

        protected override void Stop()
        {
            if (_server == null)
                return;

            try { _server.Close(); }
            catch { }

            _server = null;
            SuperController.LogMessage("Upd stopped");
        }

        private void Receive(IAsyncResult ar)
        {
            var server = (UdpClient)ar.AsyncState;
            var bytes = server.EndReceive(ar, ref _receiveEndpoint);
            var data = Encoding.ASCII.GetString(bytes);

            ParseCommands(data);

            server.BeginReceive(Receive, server);
        }

        public override void Update()
        {
            if (_server == null)
                return;

            UpdateValues();
        }
    }
}
