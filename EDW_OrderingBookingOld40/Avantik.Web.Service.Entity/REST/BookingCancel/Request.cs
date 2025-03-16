using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avantik.Web.Service.BookingCancel
{
    public class BookingCancelRequest
    {
        public string AgencyCode { get; set; }
        public string UserLogon { get; set; }
        public string Password { get; set; }
        public Guid booking_id { get; set; }
        public Guid booking_segment_id { get; set; }
        public bool IsVoidAllFees { get; set; }
    }
}