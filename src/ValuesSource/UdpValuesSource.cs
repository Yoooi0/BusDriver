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
        private readonly byte[] _readBuffer;
        private UdpClient _server;
        private EndPoint _receiveEndpoint;

        private UITextInput PortInput;
        private JSONStorableString PortText;

        private JSONStorableAction StartUdpAction;
        private JSONStorableAction StopUdpAction;

        public UdpValuesSource()
        {
            _readBuffer = new byte[1024];
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
                _server.Client.Blocking = false;
                _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _server.Client.Bind(new IPEndPoint(IPAddress.Loopback, port));
            }
            catch(Exception e)
            {
                SuperController.LogError(e.ToString());
            }

            SuperController.LogMessage($"Upd started on port: {port}");
        }

        protected override void Stop()
        {
            if (_server == null)
                return;

            _server.Close();
            _server = null;

            SuperController.LogMessage("Upd stopped");
        }

        private string ReceiveLatest()
        {
            var received = -1;
            while (true)
            {
                try
                {
                    received = _server.Client.ReceiveFrom(_readBuffer, ref _receiveEndpoint);
                    if (received <= 0)
                        break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                        break;
                }
            }

            if(received <= 0)
                return null;

            return Encoding.ASCII.GetString(_readBuffer, 0, received);
        }

        public override void Update()
        {
            if (_server == null)
                return;

            try
            {
                var data = ReceiveLatest();
                UpdateValues(data);
            }
            catch (SocketException e)
            {
                SuperController.LogError(e.ToString());
            }
        }
    }
}
