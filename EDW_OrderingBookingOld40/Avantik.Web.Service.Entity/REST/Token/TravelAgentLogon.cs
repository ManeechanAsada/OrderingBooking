using Avantik.Web.Service.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Avantik.Web.Service.Entity.REST.Token
{
    public class TravelAgentLogonRequest
    {
        public string AgencyCode { get; set; }
        public string AgentLogon { get; set; }
        public string AgentPassword { get; set; }
    }

    public class TravelAgentLogonResponse : ResponseBase
    {
        public Agency.Agent AgentResponse { get; set; }

    }
}
