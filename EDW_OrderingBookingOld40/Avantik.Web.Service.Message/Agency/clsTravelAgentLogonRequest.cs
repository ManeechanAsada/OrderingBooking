using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using Avantik.Web.Service;

namespace Avantik.Web.Service.Message
{
    [MessageContract]
    public class TravelAgentLogonRequest
    {
        [MessageBodyMember]
        public string AgencyCode { set; get; }
        [MessageBodyMember]
        public string AgentLogon { set; get; }
        [MessageBodyMember]
        public string AgentPassword { set; get; }


    }
    public class AgencyUserDetails
    {
        public string agency_code { get; set; }
        public string user_logon { get; set; }
        public string user_password { get; set; }
        public string user_account_id { get; set; }
        public byte api_flag { get; set; }
        public string currency_crd { get; set; }
        public string language_crd { get; set; }
    }
}
