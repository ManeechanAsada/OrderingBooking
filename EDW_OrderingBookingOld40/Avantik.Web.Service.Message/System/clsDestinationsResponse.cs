﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Avantik.Web.Service.Message
{
    [MessageContract]
    public class DestinationsResponse : ResponseBase
    {
        [MessageBodyMember]
        public IList<RouteView> Routes { get; set; }
    }
}
