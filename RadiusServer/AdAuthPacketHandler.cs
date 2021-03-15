using Flexinets.Radius.Core;
using log4net;
using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;

namespace RadiusServer
{
    public class AdAuthPacketHandler : IPacketHandler
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(AdAuthPacketHandler));
        public IRadiusPacket HandlePacket(IRadiusPacket packet)
        {
            var config = ConfigurationManager.AppSettings;
            var domain = config.Get("Domain");
            var groupName = config.Get("AdGroupName");
            _log.Info($"Recived request with packet code {packet.Code}");
            if(packet.Code == PacketCode.AccessRequest)
            {
                var userName = packet.GetAttribute<String>("User-Name");
                var userPassword = packet.GetAttribute<String>("User-Password");
                var userLogin = ValidateCredentials(domain, userName, userPassword, userName, userName);
                var userInGroup = IsUserInAdGroup(domain, userName, groupName, userName, userPassword);
                _log.Info($"User Login result ={userLogin} UserInGroup result = {userInGroup} for user {userName}");
                if(userInGroup && userLogin)
                {
                    var response = packet.CreateResponsePacket(PacketCode.AccessAccept);
                    response.AddAttribute("Acct-Interim-Interval", 60);
                    return response;
                }

                return packet.CreateResponsePacket(PacketCode.AccessReject);
            }

            _log.Info($"Cant handle request code {packet.Code}");

            throw new InvalidOperationException($"Can't handle other requests besides AccessRequests with code {PacketCode.AccessRequest}");
        }

        private bool ValidateCredentials(string domain, string username, string password, string servAccUser, string servAccPass)
        {
            var result = false;
            using (var context = new PrincipalContext(ContextType.Domain, domain, servAccUser, servAccPass))
            {
                result = context.ValidateCredentials(username, password);
            }

            return result;
        }

        private bool IsUserInAdGroup(string domain, string username, string adGroupName, string servAccUser, string servAccPass)
        {
            bool result = false;
            using (var context = new PrincipalContext(ContextType.Domain, domain, servAccUser, servAccPass))
            {
                var user = UserPrincipal.FindByIdentity(context, username);
                if (user != null)
                {
                    var group = GroupPrincipal.FindByIdentity(context, adGroupName);
                    if (group != null && user.IsMemberOf(group))
                        result = true;
                    group.Dispose();
                }
                user.Dispose();
            }
            return result;
        }

        public void Dispose()
        {
        }
    }
}
