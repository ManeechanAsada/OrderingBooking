using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using entity = Avantik.Web.Service.Entity.Booking;

namespace Avantik.Web.Service.Entity.Booking.REST
{
    public class BookingSaveRequest : BookingFlightRequest
    {

    }
    public class BookingFlightRequest
    {
        #region BookingSave
        public entity.Booking booking { get; set; }
        public bool createTickets { get; set; } = false;
        public bool readBooking { get; set; } = false;
        public bool readOnly { get; set; } = false;
        public bool bSetLock { get; set; } = false;
        public bool bCheckSeatAssignment { get; set; } = false;
        public bool bCheckSessionTimeOut { get; set; } = false;
        #endregion

        #region BookFlight
        public string AgencyCode { get; set; }
        public string Currency { get; set; }
        public IList<Flight> Flight { get; set; }
        public string BookingId { get; set; }
        public short Adults { get; set; }
        public short Children { get; set; }
        public short Infants { get; set; }
        public short Others { get; set; }
        public string StrOthers { get; set; }
        public string UserId { get; set; }
        public string StrIpAddress { get; set; }
        public string StrLanguageCode { get; set; }
        public bool BNoVat { get; set; }
        #endregion

    }
}
