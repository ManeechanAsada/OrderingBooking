using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Model.Contract;
using Avantik.Web.Service.Infrastructure;
using Avantik.Web.Service.COMHelper;

namespace Avantik.Web.Service.Model.COM.Flight
{
    public class AvailabilityService : RunComplus, IAvailabilityService
    {
        string _server = string.Empty;
        public AvailabilityService(string server, string user, string pass, string domain)
            : base(user, pass, domain)
        {
            _server = server;
        }
        public Entity.Flight.Availabilities GetAvailability(string otherPassengerType, 
                                                            string boardingClass, 
                                                            string bookingClass, 
                                                            string dayTimeIndicator,
                                                            string returnDayTimeIndicator,
                                                            string agencyCode, 
                                                            string currencyCode, 
                                                            string transitPoint, 
                                                            string promotionCode, 
                                                            AvailabilityFareTypes fareType, 
                                                            string languageCode, 
                                                            string ipAddress, 
                                                            string originRcd, 
                                                            string destinationRcd, 
                                                            string odOriginRcd, 
                                                            string odDestinationRcd, 
                                                            DateTime fromDate, 
                                                            DateTime toDate, 
                                                            DateTime returnFromDate, 
                                                            DateTime returnToDate, 
                                                            DateTime bookingDate, 
                                                            decimal maxAmount, 
                                                            byte adult, 
                                                            byte child, 
                                                            byte infant, 
                                                            byte other, 
                                                            Guid flightId, 
                                                            Guid fareId, 
                                                            short nonStopOnly, 
                                                            short includeDeparted, 
                                                            short includeCancelled, 
                                                            short includeWaitlisted, 
                                                            short includeSoldOut, 
                                                            short includeFares, 
                                                            short refundable,
                                                            short returnRefundable,
                                                            short groupFares, 
                                                            short iTFaresOnly, 
                                                            short staffFares, 
                                                            bool applyFareLogic, 
                                                            bool showClose, 
                                                            short unknownTransit, 
                                                            bool mapWithFares, 
                                                            bool noVat, 
                                                            string transactionReference)
        {
          

            return null;
        }
    }
}
