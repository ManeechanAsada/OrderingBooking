using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using Avantik.Web.Service;

namespace Avantik.Web.Service.Message.Booking
{
    [DataContract]
    public class BookingCancelRequest
    {
        [DataMember]
        public string BookingId { set; get; }
        [DataMember]
        public string BookingSegmentId { set; get; }
        [DataMember]
        public string UserId { set; get; }

        [DataMember]
        public bool IsVoidAllFees { set; get; }

    }
}
