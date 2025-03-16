using Avantik.Web.Service.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.Entity.Agency;
using Avantik.Web.Service.COMHelper;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Entity;
using Avantik.Web.Service.Entity.Booking;


namespace Avantik.Web.Service.Model.COM
{
    public class FeeService : RunComplus, IFeeService
    {
        string _server = string.Empty;
        public FeeService(string server, string user, string pass, string domain)
            :base(user,pass,domain)
        {
            _server = server;
        }

        public IList<Entity.Fee> GetFee(string strFeeRcd,
                          string strCurrencyCode,
                          string strAgencyCode,
                          string strClass,
                          string strFareBasis,
                          string strOrigin,
                          string strDestination,
                          string strFlightNumber,
                          DateTime dtDate,
                          string strLanguage,
                          bool bNovat)
        {
           
            IList<Entity.Fee> fees = new List<Entity.Fee>();
          
            return fees;
        }

        public IList<Entity.Booking.Fee> CalculateFeesBookingCreate(string AgencyCode,
                                                        string currency,
                                                        Booking booking,
                                                        string strLanguage) 
        {
           
            return booking.Fees;
        }


        public IList<Entity.Booking.Fee> CalculateFeesBookingChange(string AgencyCode,
                                                    string currency,
                                                    Booking booking,
                                                    string strLanguage)
        {
           

            return booking.Fees;
        }

        public IList<Entity.Booking.Fee> CalculateFeesSeatAssignment(string AgencyCode,
                                                        string currency,
                                                        Booking booking,
                                                        string strLanguage,
                                                        bool bNovat)
        {
          
         
            IList<Entity.Booking.Fee> fees = new List<Entity.Booking.Fee>();
            return fees;
        }

        public IList<Entity.Booking.Fee> CalculateFeesNameChange(string AgencyCode,
                                                    string currency,
                                                    Booking booking,
                                                    string strLanguage,
                                                    string strUserId)
        {
          

        

            return null;
        }

        //ssr
        public Entity.Booking.Booking CalculateFeesSpecialService(string AgencyCode,
                                                        string currency,
                                                        Booking booking,
                                                        string strLanguage,
                                                        bool bNovat)
        {
         
            return null;
        }

        public List<ServiceFee> GetSegmentFee(string agencyCode,
                                       string currencyCode,
                                       string languageCode,
                                       int numberOfPassenger,
                                       int numberOfInfant,
                                       IList<PassengerService> services,
                                       IList<SegmentService> segmentService,
                                       bool SpecialService, 
                                       bool bNovat)
        {
           

            return null;
        }


        public IList<Entity.Booking.Fee> GetBaggageFee(
            IList<Entity.Booking.Mapping> mapping, 
            Guid bookingSegmentId, 
            Guid passengerId, 
            string agencyCode, 
            string languageCode, 
            int maxUnits, 
            IList<Entity.Booking.Fee> fees, 
            bool bNovat)
        {
          
            return null;
        }
    }
}
