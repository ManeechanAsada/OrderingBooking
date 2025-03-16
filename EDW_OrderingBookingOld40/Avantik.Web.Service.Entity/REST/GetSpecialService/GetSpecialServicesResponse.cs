using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avantik.Web.Service.Entity.REST.GetSpecialService
{
    public class GetSpecialServicesResponse : ResponseBase
    {
        public IList<ServiceFee> ServiceFees { get; set; }

    }
}
