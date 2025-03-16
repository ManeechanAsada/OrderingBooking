using Avantik.Web.Service.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.Entity.Agency;
using Avantik.Web.Service.COMHelper;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Exception.Booking;
using Avantik.Web.Service.Entity.Booking;
using Avantik.Web.Service.Entity;

using System.Data;

namespace Avantik.Web.Service.Model.COM
{
    public class PaymentService : RunComplus, IPaymentService
    {
        string _server = string.Empty;
        string _user = string.Empty;
        string _pass = string.Empty;
        string _domain = string.Empty;
        public PaymentService(string server, string user, string pass, string domain)
            : base(user, pass, domain)
        {
            _server = server;
            _user = user;
            _pass = pass;
            _domain = domain;
        }

        public bool SavePayment(IList<Payment> payments, IList<PaymentAllocation> paymentAllocations)
        {
           
            return true;

        }

        public bool PaymentCreditCard(Entity.Booking.BookingHeader header,
                                      IList<Entity.Booking.Payment> payments,
                                      IList<Entity.PaymentAllocation> paymentAllocations)
        {
           
            return true;
        }

        private Entity.Voucher ValidateVoucher(Entity.Voucher voucher, IList<Entity.Booking.Mapping> Mappings, IList<Entity.Booking.Fee> Fees)
        {
          

            return null;
        }

        // for cob 
        public bool PaymentVoucher(decimal totalOutStanding,IList<Entity.Booking.Mapping> Mappings, IList<Entity.Booking.Fee> Fees,
                                   IList<Entity.Booking.Payment> payments,
                                   IList<Entity.PaymentAllocation> paymentAllocations, string agencyCode,string userId)
        {
          

            return true;
        }
        //

        public bool PaymentAgencyAccount(decimal totalOutStanding, 
                                         string paymentType, 
                                         string agencyCode, 
                                         string userId,
                                         IList<Entity.Booking.Payment> payments,
                                         IList<Entity.PaymentAllocation> paymentAllocations)
        {

           

            return true;
        }

        public IList<Entity.Booking.Fee> GetPaymentTypeFee(
                                        string originRcd,
                                        string destinationRcd,
                                        string formOfPayment,
                                        string formOfPaymentSubtype,
                                        string currencyRcd,
                                        string agency,
                                        DateTime feeDate)
        {
         
            return null;

        }
    }
}
