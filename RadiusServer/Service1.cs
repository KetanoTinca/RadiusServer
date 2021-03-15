using Flexinets.Net;
using Flexinets.Radius.Core;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.ServiceProcess;
using System.Net;
using System.Configuration;
using log4net;

namespace RadiusServer
{
    public partial class RadiusServerService : ServiceBase
    {
        private Flexinets.Radius.RadiusServer _authenticationServer;
        private readonly ILog _log = LogManager.GetLogger(typeof(RadiusServerService));
        public RadiusServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _log.Info("Starting service");
                var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "/Content/radius.dictionary";
                var dictionary = new RadiusDictionary(path, NullLogger<RadiusDictionary>.Instance);
                var radiusPacketParser = new RadiusPacketParser(NullLogger<RadiusPacketParser>.Instance,dictionary);
                var packetHandler = new AdAuthPacketHandler();
                var repository = new Flexinets.Radius.PacketHandlerRepository();
                var udpClientFactory = new UdpClientFactory();
                repository.AddPacketHandler(IPAddress.Any, packetHandler, "secret");

                var port = int.Parse(ConfigurationManager.AppSettings.Get("Port"));

                _authenticationServer = new Flexinets.Radius.RadiusServer(
                    udpClientFactory,
                    new IPEndPoint(IPAddress.Any, port),
                    radiusPacketParser,
                    Flexinets.Radius.RadiusServerType.Authentication,
                    repository,
                    NullLogger<Flexinets.Radius.RadiusServer>.Instance
                    );

                _authenticationServer.Start();

            }catch(Exception ex)
            {
                _log.Fatal("Failed to start service", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            _authenticationServer?.Stop();
            _authenticationServer?.Dispose();
        }
    }
}
