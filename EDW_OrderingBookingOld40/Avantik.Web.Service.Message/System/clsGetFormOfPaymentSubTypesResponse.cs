﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using Avantik.Web.Service;

namespace Avantik.Web.Service.Message
{
      [MessageContract]
    public class GetFormOfPaymentSubTypesResponse : ResponseBase
    {
          [MessageBodyMember]
          public Message.FormOfPayment.FormOfPayment FormOfPayment { get; set; }

    }
}
