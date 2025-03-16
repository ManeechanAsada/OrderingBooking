using Avantik.Web.Service.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.COMHelper;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Entity;
using Avantik.Web.Service.Entity.Client;
using Avantik.Web.Service.Entity.Booking;

namespace Avantik.Web.Service.Model.COM
{
    public class ClientService : RunComplus, IClientService
    {
        string _server = string.Empty;
        public ClientService(string server, string user, string pass, string domain)
            :base(user,pass,domain)
        {
            _server = server;
        }
        public bool ClientSave(ClientProfile clientProfile)
        {
          
            return true;

        }

        public ClientProfile ClientRead(string clientProfileId)
        {
            ClientProfile ClientProfile = new Entity.Client.ClientProfile();
         
            return ClientProfile;

        }

        public bool EditClientProfile(ClientProfile clientProfile)
        {
          
            return true;

        }

    }
}
