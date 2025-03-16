using System;
using System.Collections.Generic;
using System.Linq;

using Avantik.Web.Service.Model.Contract;

using Avantik.Web.Service.COMHelper;
using entity = Avantik.Web.Service.Entity.Booking;
using Avantik.Web.Service.Helpers;
using Avantik.Web.Service.Entity.Booking;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using Avantik.Web.Service.Model.COM.Extension.REST;
using System.Collections;
using System.Text;
using Avantik.Web.Service.Model.COM.Extension;

namespace Avantik.Web.Service.Model.COM
{
    public class BookingModelService 
    {
        string _server = string.Empty;
        string _user = string.Empty;
        string _pass = string.Empty;
        string _domain = string.Empty;

        bool result = false;
        public BookingModelService(string server, string user, string pass, string domain)
        {
            _server = server;
            _user = user;
            _pass = pass;
            _domain = domain;
        }


        public bool SaveBooking(entity.Booking booking,
                                bool createTickets,
                                bool readBooking,
                                bool readOnly,
                                bool bSetLock,
                                bool bCheckSeatAssignment,
                                bool bCheckSessionTimeOut)
        {
            string strBookingId = string.Empty;
            string strOther = string.Empty;
            string strUserId = string.Empty;
            string strAgencyCode = string.Empty;
            string strCurrency = string.Empty;

            short iAdult = 0;
            short iChild = 0;
            short iInfant = 0;
            short iOther = 0;


            return result;
        }


        public entity.Booking ReadBooking(string bookingId,
                                   string bookingReference,
                                   double bookingNumber,
                                   bool bReadonly,
                                   bool bSeatLock,
                                   string userId,
                                   bool bReadHeader,
                                   bool bReadSegment,
                                   bool bReadPassenger,
                                   bool bReadRemark,
                                   bool bReadPayment,
                                   bool bReadMapping,
                                   bool bReadService,
                                   bool bReadTax,
                                   bool bReadFee,
                                   bool bReadOd,
                                   string releaseBookingId,
                                   string CompleteRemarkId)

        {
            entity.Booking booking = new entity.Booking();

            string baseURL = ConfigHelper.ToString("RESTURL");
            string apiURL = baseURL + "api/Booking/ReadBooking";

            try
            {
                if (!string.IsNullOrEmpty(bookingId))
                {
                    var BookingRESTRequest = new Avantik.Web.Service.Entity.Booking.REST.BookingRead.BookingReadRequest
                    {
                        booking_id = new Guid(bookingId),
                        bReadHeader = true,
                        bReadPassenger = true,
                        bReadSegment = true,
                        bReadMapping = true,
                        bReadPayment = true,
                        bReadTax = true,
                        bReadFee = true,
                        bReadRemark = true,
                        bReadOd = true,
                        bReadService = true
                    };

                    var jsonContent = JsonConvert.SerializeObject(BookingRESTRequest);
                    var content = System.Text.Encoding.UTF8.GetBytes(jsonContent);

                    var requestUri = new Uri(apiURL);

                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);

                    var userlogon = string.Format("{0}:{1}", "DLXAPI", "dlxapi");
                    byte[] bytes = Encoding.UTF8.GetBytes(userlogon);
                    string base64String = Convert.ToBase64String(bytes);
                    httpWebRequest.Headers["Authorization"] = "Basic " + base64String;


                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = content.Length;

                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(content, 0, content.Length);
                    }
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                var responseContent = reader.ReadToEnd();
                                Avantik.Web.Service.Entity.Booking.REST.BookingRead.BookingRead bookingRead = JsonConvert.DeserializeObject<Avantik.Web.Service.Entity.Booking.REST.BookingRead.BookingRead>(responseContent);

                                booking.Header = bookingRead.Header.ToBookingMessage();
                                booking.Segments = bookingRead.Segments.ToBookingMessage();
                                booking.Passengers = bookingRead.Passengers.ToBookingMessage();
                                booking.Mappings = bookingRead.Mappings.ToBookingMessage();
                                booking.Fees = bookingRead.Fees.ToBookingMessage();
                                booking.Services = bookingRead.Services.ToBookingMessage();
                                booking.Payments = bookingRead.Payments.ToBookingMessage();
                                booking.Remarks = bookingRead.Remarks.ToBookingMessage();
                                booking.Taxs = bookingRead.Taxs.ToBookingMessage();
                                booking.Quotes = bookingRead.Quotes.ToBookingMessage();
                            }

                        }
                    }

                }
                else
                {

                }
            }
            catch (System.Exception ex)
            {
            }


            return booking;
        }

        public bool CancelSegment(string bookingId, 
                                  string bookingSegmentId,
                                  string userId,
                                  string agencyCode,
                                  bool bWaveFee,
                                  bool bVoid)
        {
            bool cancelSuccess = false;
            bool IsVoidAllFee = false;
            string baseURL = ConfigHelper.ToString("RESTURL");
            string apiURL = baseURL + "api/Booking/CancelBookingSegment";

            try
            {
                if (!String.IsNullOrEmpty(bookingId) && !String.IsNullOrEmpty(userId))
                {
                    var BookingRESTRequest = new Avantik.Web.Service.Entity.Booking.REST.BookingCancel.BookingSegmentCancelRequest
                    {
                        UserId = new Guid(userId),
                        AgencyCode = agencyCode,
                        booking_id = new Guid(bookingId),
                        booking_segment_id = new Guid(bookingSegmentId),
                        IsVoidAllFees = IsVoidAllFee,

                    };

                    var jsonContent = JsonConvert.SerializeObject(BookingRESTRequest);
                    var content = System.Text.Encoding.UTF8.GetBytes(jsonContent);

                    var requestUri = new Uri(apiURL);

                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);

                    var userlogon = string.Format("{0}:{1}", "DLXAPI", "dlxapi");
                    byte[] bytes = Encoding.UTF8.GetBytes(userlogon);
                    string base64String = Convert.ToBase64String(bytes);
                    httpWebRequest.Headers["Authorization"] = "Basic " + base64String;

                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = content.Length;

                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(content, 0, content.Length);
                    }
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            cancelSuccess = true;
                        }
                    }

                }
                else
                {

                }

            }
            catch (System.Exception ex)
            {
            }

            return cancelSuccess;
        }


        

    }
}
