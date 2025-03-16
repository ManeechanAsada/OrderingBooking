using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.Message;
using Avantik.Web.Service.Contracts;
using Avantik.Web.Service.Helpers;
using Avantik.Web.Service.Entity;
using Avantik.Web.Service.Model;
using Avantik.Web.Service.Model.Contract;
using Avantik.Web.Service.Exception.Flight;
using Avantik.Web.Service.Message.Booking;
using Avantik.Web.Service.Model.Booking;
using Avantik.Web.Service.Extension;
using Avantik.Web.Service.Entity.Booking;
using Avantik.Web.Service.Entity.Agency;
using Avantik.Web.Service.Message.ManageBooking;
using Avantik.Web.Service.Exception.Booking;
using Avantik.Web.Service.Message.SeatMap;
using System.Web;
using System.Web.Routing;
using System.ServiceModel;
using Avantik.Web.Service.Message.OrderBooking;
using Avantik.Web.Service.Message.OrderBooking.Extension;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Collections;
using Avantik.Web.Service.Model.COM.Extension;
using Avantik.Web.Service.Model.COM.Extension.REST;

namespace Avantik.Web.Service
{
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class OrderingBookingService : IOrderingBookingService
    {
        public InitializeResponse ServiceInitialize(InitializeRequest request)
        {
            InitializeResponse response = new InitializeResponse();

            // valid
            request.Valid(response);

            if (response.Success)
            {
                AuthenticationService objService = new AuthenticationService();
                response = objService.ServiceInitialize(request);
            }

            return response;
        }
        public BookingOrderResponse ModifyBooking(OrderBookingRequest request)
        {
           // IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            ModifyBookingResponse modifyResponse = new ModifyBookingResponse();
            BookingModifyResponse res = new BookingModifyResponse();
            BookingOrderResponse response = new BookingOrderResponse();
            Booking bookingResponse = new Booking();
            Booking booking = new Booking();
            Guid passengerIdDefault = Guid.Empty;
            Guid segmentIdDefault = Guid.Empty;
            Guid bookingId = Guid.Empty;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;
            string ActionCode = string.Empty;
            string originRCD = string.Empty;
            string destinationRCD = string.Empty;
            //decimal totalOutStandingBalance = 0;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // test get log
                    if (request.Token.Contains("dev@bravo"))
                    {
                        //
                    }
                    else
                    {
                        // valid token
                        Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                        if (objAuthen.ResponseSuccess == true)
                        {
                            userId = objAuthen.UserId.ToString();
                            agencyCode = objAuthen.AgencyCode;
                            currencyRcd = objAuthen.CurrencyRcd;
                            languageRcd = objAuthen.LanguageRcd;

                          
                            if (request != null)
                            {
                                //Validate input parameter.
                                if (string.IsNullOrEmpty(request.BookingId))
                                {
                                    modifyResponse.Code = "";
                                    modifyResponse.Success = false;
                                    modifyResponse.Message = "Booking Id required.";
                                }
                                else
                                {
                                    
                                    bookingId = new Guid(request.BookingId);
                                    IList<Message.Booking.FlightSegment> BookingSegments = null;
                                    IList<Message.Booking.Mapping> Mappings = null;
                                    IList<Message.Booking.Fee> Fees = null;
                                    IList<Message.Booking.PassengerService> Services = null;
                                    IList<Message.Booking.Tax> Taxes = null;

                                    if (request.BookingSegments != null)
                                    {
                                        response.BookingSegments = SegmentValidation(request.BookingSegments);

                                        BookingSegments = request.BookingSegments.FillOrderToBookingMessage(bookingId, objAuthen.UserId);

                                    }

                                    if (request.Mappings != null)
                                    {
                                        response.Mappings = MappingValidation(request.Mappings);

                                        Mappings = request.Mappings.FillOrderToBookingMessage(bookingId, objAuthen.UserId, agencyCode);

                                    }

                                    if (request.Fees != null)
                                    {
                                        response.Fees = FeeValidation(request.Fees);
                                        Fees = request.Fees.FillOrderToBookingMessage(bookingId, objAuthen.UserId, agencyCode);
                                    }

                                    if (request.Services != null)
                                    {
                                        response.Services = ServiceValidation(request.Services);
                                        Services = request.Services.FillOrderToBookingMessage(objAuthen.UserId);
                                    }

                                    if (request.Taxes != null)
                                    {
                                        response.Taxes = TaxValitdation(request.Taxes);
                                        Taxes = request.Taxes.FillOrderToBookingMessage(objAuthen.UserId);
                                    }

                                    if (response.BookingSegments == null && response.Passengers == null && response.Mappings == null && response.Fees == null &&
                                        response.Taxes == null && response.Remarks == null && response.Services == null && response.Payments == null &&
                                        response.BookingHeader == null)
                                    {
                                        bool validSegment = false;

                                        if (request.BookingSegments != null && request.BookingSegments.Count > 0)
                                        {
                                            foreach (Message.OrderBooking.FlightSegment f in request.BookingSegments)
                                            {
                                                if (f.segment_status_rcd.ToUpper() == "XX" && f.exchanged_segment_id == Guid.Empty)
                                                {
                                                    validSegment = true;
                                                    break;
                                                }
                                            }
                                        }



                                        if (!validSegment)
                                        {
                                            bookingResponse = OrderingBooking(
                                                                                request.BookingId,
                                                                                BookingSegments.ToListEntity(),
                                                                                Mappings.ToListEntity(),
                                                                                Fees.ToListEntity(),
                                                                                Services.ToListEntity(),
                                                                                Taxes.ToListEntity(),
                                                                                userId,
                                                                                agencyCode,
                                                                                currencyRcd,
                                                                                languageRcd);
                                        }
                                        else
                                        {
                                            modifyResponse.Code = "";
                                            modifyResponse.Success = false;
                                            modifyResponse.Message = "Invalid ExchangedSegmentId in Segment status XX.";
                                        }

                                        if (bookingResponse != null && bookingResponse.Header != null && bookingResponse.Mappings != null)
                                        {
                                            BookingReadRequest readRequest = new BookingReadRequest();
                                            readRequest.BookingId = request.BookingId;
                                            readRequest.Token = request.Token;

                                            if (Mappings != null && Mappings.Count > 0)
                                            {
                                                // call InsertBookingChangeQueueAPI
                                                bool IsInsertBookingChangeQueue = false;
                                                foreach (Message.Booking.Mapping m in Mappings)
                                                {
                                                    if (!string.IsNullOrEmpty(m.SeatNumber))
                                                    {
                                                        IsInsertBookingChangeQueue = true; break;
                                                    }
                                                }

                                                if (IsInsertBookingChangeQueue)
                                                {
                                                    InsertBookingChange(new Guid(userId), bookingId);
                                                }
                                            }

                                            //get response
                                            Avantik.Web.Service.Message.Booking.BookingReadResponse wsReadResponse = ReadBooking(readRequest);

                                            //reset modifyResponse
                                            modifyResponse.Success = true;

                                            if (wsReadResponse.Success == true)
                                            {
                                                //Set Booking Header inforamtion.
                                                response.BookingHeader = wsReadResponse.BookingResponse.Header.MapBookingHeaderResponse();

                                                response.BookingHeader.error_code = "000";
                                                response.BookingHeader.error_message = "SUCCESS";

                                                //Fill Flight segment information.
                                                response.BookingSegments = wsReadResponse.BookingResponse.FlightSegments.MapBookingSegmentsResponse();
                                                for (int i = 0; i < response.BookingSegments.Count; i++)
                                                {
                                                    response.BookingSegments[i].error_code = "000";
                                                    response.BookingSegments[i].error_message = "SUCCESS";
                                                }

                                                ////Fill passenger information.
                                                response.Passengers = wsReadResponse.BookingResponse.Passengers.MapPassengersResponse();
                                                for (int i = 0; i < response.Passengers.Count; i++)
                                                {
                                                    response.Passengers[i].error_code = "000";
                                                    response.Passengers[i].error_message = "SUCCESS";
                                                }

                                                ////Fill Mapping information.
                                                response.Mappings = wsReadResponse.BookingResponse.Mappings.MapMappingsResponse();
                                                for (int i = 0; i < response.Mappings.Count; i++)
                                                {
                                                    response.Mappings[i].error_code = "000";
                                                    response.Mappings[i].error_message = "SUCCESS";
                                                }

                                                ////Fill fee information.
                                                if (wsReadResponse.BookingResponse.Fees != null && wsReadResponse.BookingResponse.Fees.Count > 0)
                                                {
                                                    response.Fees = wsReadResponse.BookingResponse.Fees.MapFeesResponse();
                                                    for (int i = 0; i < response.Fees.Count; i++)
                                                    {
                                                        response.Fees[i].error_code = "000";
                                                        response.Fees[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill tax information.
                                                if (wsReadResponse.BookingResponse.Taxs != null && wsReadResponse.BookingResponse.Taxs.Count > 0)
                                                {
                                                    response.Taxes = wsReadResponse.BookingResponse.Taxs.MapTaxsResponse();
                                                    for (int i = 0; i < response.Taxes.Count; i++)
                                                    {
                                                        response.Taxes[i].error_code = "000";
                                                        response.Taxes[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill remark information.
                                                if (wsReadResponse.BookingResponse.Remarks != null && wsReadResponse.BookingResponse.Remarks.Count > 0)
                                                {
                                                    response.Remarks = wsReadResponse.BookingResponse.Remarks.MapRemarksResponse();
                                                    for (int i = 0; i < response.Remarks.Count; i++)
                                                    {
                                                        response.Remarks[i].error_code = "000";
                                                        response.Remarks[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill service information.
                                                if (request.Services != null && request.Services.Count > 0)
                                                {
                                                    response.Services = wsReadResponse.BookingResponse.Services.MapServicesResponse();
                                                    for (int i = 0; i < response.Services.Count; i++)
                                                    {
                                                        response.Services[i].error_code = "000";
                                                        response.Services[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill payment information
                                                if (wsReadResponse.BookingResponse.Payments != null && wsReadResponse.BookingResponse.Payments.Count > 0)
                                                {
                                                    response.Payments = wsReadResponse.BookingResponse.Payments.MapPaymentsResponse();
                                                    for (int i = 0; i < response.Payments.Count; i++)
                                                    {
                                                        response.Payments[i].error_code = "000";
                                                        response.Payments[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                response.Code = "000";
                                                response.Success = true;
                                                response.Message = "SUCCESS";
                                            }
                                            else
                                            {
                                                response.Code = "129";
                                                response.Success = false;
                                                response.Message = "Read booking failed.";
                                            }
                                        }

                                        if (modifyResponse.Success == false)
                                        {
                                            response.Code = modifyResponse.Code;
                                            response.Success = false;
                                            response.Message = modifyResponse.Message;
                                        }

                                    }                                  
                                }
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Invalid token.";
                            response.Code = "005";
                        }
                    } // test get log
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = "300";
                response.Message = mex.Message;
                response.Success = false;
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;

            }
            finally
            {
                // save log
               // SaveOrderingLog(request, response);
            }

            return response;
        }

        public BookingReadResponse ReadBooking(BookingReadRequest request)
        {
            BookingService objBookingService = new BookingService();
            BookingReadResponse response = new BookingReadResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        request.Token = request.Token;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingReference) && request.BookingId == "00000000-0000-0000-0000-000000000000")
                        {
                            response.Code = "B007";
                            response.Success = false;
                            response.Message = "Booking reference is required.";
                        }
                        else if (string.IsNullOrEmpty(request.BookingReference) && string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "B007";
                            response.Success = false;
                            response.Message = "Booking reference is required.";
                        }
                        else
                        {
                            //read booking
                            response = objBookingService.ReadBooking(request);

                            if (response != null && response.Success == true)
                            {
                                response.Code = "000";
                                response.Message = "Success";
                                response.Success = true;
                            }
                            else
                            {
                                response.Code = "B008";
                                response.Message = "Read Booking failed.";
                                response.Success = false;
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
            }

            return response;
        }


        public GetSeatMapResponse GetSeatMap(GetSeatMapRequest request)
        {
            BookingService objBookingService = new BookingService();
            GetSeatMapResponse response = new GetSeatMapResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;
            DataSet ds = new DataSet();

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        request.Token = request.Token;

                        // validate request
                        request.Valid(response, objAuthen);
                        // no need read booking
                        // call to booking service
                        if (response.Success)
                        {
                            //get seat map
                            // request = request.SeatMapRequest();
                            // response = objBookingService.GetSeatMap(request);

                            string connectionString = ConfigHelper.ToString("SQLConnectionString");
                            using (SqlConnection conn = new SqlConnection(connectionString))
                            {
                                //
                                SqlCommand sqlComm = new SqlCommand("get_seat_map_api_ordering", conn);
                                // SqlCommand sqlComm = new SqlCommand("get_seat_map_api", conn);
                                sqlComm.Parameters.AddWithValue("@origin", request.OriginRcd);
                                sqlComm.Parameters.AddWithValue("@destination", request.DestinationRcd);

                                if (string.IsNullOrEmpty(request.FlightId.ToString()))
                                    sqlComm.Parameters.AddWithValue("@flightid", null);
                                else
                                    sqlComm.Parameters.AddWithValue("@flightid", request.FlightId.ToString());

                                if (string.IsNullOrEmpty(request.BoardingClass))
                                    sqlComm.Parameters.AddWithValue("@boardingClass", "Y");
                                else
                                    sqlComm.Parameters.AddWithValue("@boardingClass", request.BoardingClass);


                                sqlComm.CommandType = CommandType.StoredProcedure;

                                SqlDataAdapter da = new SqlDataAdapter();
                                da.SelectCommand = sqlComm;

                                da.Fill(ds);

                            }

                            List<SeatMap> seatMaps = new List<SeatMap>();
                            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow dtRow in ds.Tables[0].Rows)
                                {
                                    // On all tables' columns
                                    SeatMap seatMap = new SeatMap();
                                    foreach (DataColumn dc in ds.Tables[0].Columns)
                                    {
                                        seatMap.AircraftConfigurationCode = dtRow["aircraft_configuration_code"].ToString();
                                        //seatMap.AirlineRcd = dtRow["airline_rcd"].ToString();
                                        seatMap.AisleFlag = dtRow["aisle_flag"] != null ? Int16.Parse(dtRow["aisle_flag"].ToString()) : 0;
                                        seatMap.BassinetFlag = Int16.Parse(dtRow["bassinet_flag"].ToString());
                                        seatMap.BlockB2bFlag = Int16.Parse(dtRow["block_b2b_flag"].ToString());
                                        seatMap.BlockB2cFlag = Int16.Parse(dtRow["block_b2c_flag"].ToString());
                                        seatMap.BlockedFlag = Int16.Parse(dtRow["blocked_flag"].ToString());
                                        seatMap.BoardingClassRcd = dtRow["boarding_class_rcd"].ToString();
                                        //seatMap.BookingClassRcd = dtRow["booking_class_rcd"].ToString();
                                        // seatMap.DepartureDate = dtRow["departure_date"].ToString();
                                        seatMap.DestinationRcd = dtRow["destination_rcd"].ToString();
                                        seatMap.EmergencyExitFlag = Int16.Parse(dtRow["emergency_exit_flag"].ToString());
                                        // seatMap.EticketFlag = Int16.Parse(dtRow["eticket_flag"].ToString());
                                        //seatMap.FairId = dtRow["fair_id"].ToString();
                                        seatMap.FlightCheckInStatusRcd = dtRow["flight_check_in_status_rcd"].ToString();
                                        // seatMap.FlightConnectionId =   dtRow["fair_id"].ToString();
                                        seatMap.FlightId = new Guid(dtRow["flight_id"].ToString());
                                        // seatMap.FlightNumber =  Int16.Parse(dtRow["blocked_flag"].ToString());
                                        seatMap.FreeSeatingFlag = Int16.Parse(dtRow["free_seating_flag"].ToString());
                                        //seatMap.HanddicappedFlag = Int16.Parse(dtRow["handdicapped_flag"].ToString());
                                        seatMap.InfantFlag = Int16.Parse(dtRow["infant_flag"].ToString());
                                        seatMap.LayoutColumn = Int16.Parse(dtRow["layout_column"].ToString());
                                        seatMap.LayoutRow = Int16.Parse(dtRow["layout_row"].ToString());
                                        seatMap.LocationTypeRcd = dtRow["location_type_rcd"].ToString();
                                        seatMap.LowComfortFlag = Int16.Parse(dtRow["low_comfort_flag"].ToString());
                                        seatMap.NoChildFlag = Int16.Parse(dtRow["no_child_flag"].ToString());
                                        seatMap.NoInfantFlag = Int16.Parse(dtRow["no_infant_flag"].ToString());
                                        seatMap.NumberOfBays = Int16.Parse(dtRow["number_of_bays"].ToString());
                                        seatMap.NumberOfColumns = dtRow["number_of_columns"] != null && !string.IsNullOrEmpty(dtRow["number_of_columns"].ToString()) ? Int16.Parse(dtRow["number_of_columns"].ToString()) : 0;
                                        seatMap.NumberOfRows = dtRow["number_of_rows"] != null && !string.IsNullOrEmpty(dtRow["number_of_rows"].ToString()) ? Int16.Parse(dtRow["number_of_rows"].ToString()) : 0;
                                        // seatMap.OdDestinationRcd = dtRow["od_destination_rcd"].ToString();
                                        //seatMap.OdOriginRcd  = dtRow["od_destination_rcd"].ToString();passenger_count
                                        seatMap.OriginRcd = dtRow["origin_rcd"].ToString();
                                        seatMap.PassengerCount = dtRow["passenger_count"] != null && !string.IsNullOrEmpty(dtRow["passenger_count"].ToString()) ? Int16.Parse(dtRow["passenger_count"].ToString()) : 0;
                                        seatMap.SeatColumn = dtRow["seat_column"].ToString();
                                        seatMap.FeeRcd = dtRow["fee_rcd"].ToString();
                                        seatMap.SeatRow = dtRow["seat_row"] != null && !string.IsNullOrEmpty(dtRow["seat_row"].ToString()) ? Int16.Parse(dtRow["seat_row"].ToString()) : 0;
                                        seatMap.StretcherFlag = Int16.Parse(dtRow["stretcher_flag"].ToString());
                                        //  seatMap.TransitPoints = dtRow["transit_points"].ToString();
                                        //seatMap.TransitpointsName = dtRow["transit_points"].ToString(); 
                                        seatMap.UnAccompaniedMinorsFlag = Int16.Parse(dtRow["unaccompanied_minors_flag"].ToString());
                                        seatMap.WindowFlag = Int16.Parse(dtRow["window_flag"].ToString());
                                    }
                                    seatMaps.Add(seatMap);
                                }

                                response.Code = "000";
                                response.Message = "Success";
                                response.Success = true;
                                response.SeatMaps = seatMaps;
                            }
                            else
                            {
                                response.Code = "F006";
                                response.Message = "Failed.";
                                response.Success = false;

                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                // Logger.SaveLog("GetSeatMap", DateTime.Now, DateTime.Now, mex.Message, XMLHelper.Serialize(request, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
                // Logger.SaveLog("GetSeatMap", DateTime.Now, DateTime.Now, ex.Message, XMLHelper.Serialize(request, false));
            }

            return response;
        }


        public GetFeeResponse GetFeeDefinition(GetFeeDefinitionRequest request)
        {
            GetFeeResponse response = new GetFeeResponse();
            IList<Message.Fee.Fee> fees = new List<Message.Fee.Fee>();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        currencyRcd = request.Currency;
                        languageRcd = objAuthen.LanguageRcd;

                        //valid
                        request.Valid(response);

                        if (response.Success)
                        {
                            // map data
                            //GetFeeRequest getFeeRequest = new GetFeeRequest();
                            //getFeeRequest = request.GetFeeDefinitionMapRequest(agencyCode, currencyRcd, languageRcd);
                            //BookingService objBookingService = new BookingService();
                            //response = objBookingService.GetFee(getFeeRequest);
                            string baseURL = ConfigHelper.ToString("RESTURL");
                            string apiURL = baseURL + "api/Setting/GetFeeDefinition";

                            try
                            {
                                    var BookingRESTRequest = new Avantik.Web.Service.Entity.REST.GetFeeDefinition.GetFeeDefinitionRequest
                                    {
                                        AgencyCode = agencyCode,
                                        FeeRcd = request.FeeRcd,
                                        BookingClass = request.BookingClass,
                                        FareCode = request.FareCode,
                                        Origin = request.Origin,
                                        Destination = request.Destination,
                                        LanguageRcd = languageRcd,
                                        Currency = currencyRcd
     
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
                                                Avantik.Web.Service.Entity.REST.GetFeeDefinition.GetFeeDefinitionResponse getFeeDefinitionResponse = JsonConvert.DeserializeObject<Avantik.Web.Service.Entity.REST.GetFeeDefinition.GetFeeDefinitionResponse>(responseContent);
                                                Message.Fee.Fee f = new Message.Fee.Fee();

                                            f = MapRESTMessageFeeToOrderingFee(getFeeDefinitionResponse.Fees[0]);


                                            fees.Add(f);

                                            response.Fees = fees;
                                            response.Success = true;





                                        }

                                        }
                                    }
       
                            }
                            catch (System.Exception ex)
                            {
                                response.Message = " error";
                                response.Success = false;
                               // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(BookingReadRequest), Request));
                            }



                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetFeeDefinition", DateTime.Now, DateTime.Now, mex.Message, "");
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                //Logger.SaveLog("GetFeeDefinition", DateTime.Now, DateTime.Now, ex.Message, "");

            }

            return response;
        }


        public GetSpecialServicesResponse GetSpecialServices(GetSpecialServicesRequest request)
        {
            GetSpecialServicesResponse response = new GetSpecialServicesResponse();
            IList<Entity.SegmentService> segmentServicesList = null;
            IList<Entity.Booking.PassengerService> passengerServices = null;

            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;
            int numberOfPassenger = 0;
            int numberOfInfant = 0;
            bool bNoVat = false;
            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else
                        {
                            // valid
                            request.Valid(response, objAuthen);
                            if (response.Success)
                            {
                                BookingReadRequest bookingReadRequest = new BookingReadRequest();
                                bookingReadRequest.BookingId = request.BookingId;
                                bookingReadRequest.Token = request.Token;

                                //read booking
                                BookingReadResponse readResponse = ReadBooking(bookingReadRequest);

                                if (readResponse != null && readResponse.Success == true)
                                {
                                    //set user id  and agency code from ...
                                    SelectedAgency objAgency = SelectedAgencyForAPI(objAuthen, readResponse.BookingResponse.Header);

                                    // prepare value
                                    userId = objAgency.UserId;
                                    agencyCode = objAgency.AgencyCode;
                                    currencyRcd = objAgency.CurrencyRcd;
                                    languageRcd = objAgency.LanguageRcd;
                                    numberOfPassenger = readResponse.BookingResponse.Header.NumberOfAdults + readResponse.BookingResponse.Header.NumberOfChildren;
                                    numberOfInfant = readResponse.BookingResponse.Header.NumberOfInfants;
                                    bNoVat = Convert.ToBoolean(readResponse.BookingResponse.Header.NoVatFlag == 0 ? false : true);


                                    Avantik.Web.Service.Entity.REST.GetSpecialService.GetServicesRequest rq = CreateGetServicesRequest(agencyCode, currencyRcd,readResponse.BookingResponse.FlightSegments);
                                    Avantik.Web.Service.Entity.REST.GetSpecialService.GetSpecialServicesResponse rs = new Entity.REST.GetSpecialService.GetSpecialServicesResponse();

                                    IList<Message.Fee.ServiceFee> ServiceFees = new List<Message.Fee.ServiceFee>();

                                    string baseURL = ConfigHelper.ToString("RESTURL");
                                    string apiURL = baseURL + "api/Setting/GetSpecialServices";

                                    try
                                    {
                                       
                                        var jsonContent = JsonConvert.SerializeObject(rq);
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
                                                    rs = JsonConvert.DeserializeObject<Avantik.Web.Service.Entity.REST.GetSpecialService.GetSpecialServicesResponse>(responseContent);

                                                    foreach(Avantik.Web.Service.Entity.ServiceFee f in rs.ServiceFees)
                                                    {
                                                        Message.Fee.ServiceFee sf = new Message.Fee.ServiceFee();
                                                        sf.AirlineRcd = f.AirlineRcd;
                                                        sf.BookingClassRcd = f.BookingClassRcd;
                                                        sf.CurrencyRcd = f.CurrencyRcd;
                                                        sf.CutOffTime = f.CutOffTime;
                                                        sf.DepartureDate = f.DepartureDate;
                                                        sf.DestinationRcd = f.DestinationRcd;
                                                        sf.DisplayName = f.DisplayName;
                                                        sf.FareCode = f.FareCode;
                                                        sf.FeeAmount = f.FeeAmount;

                                                        ServiceFees.Add(sf);
                                                    }

                                                    response.ServiceFees = ServiceFees;
                                                    response.Success = true;


                                                }

                                            }
                                        }

                                    }
                                    catch (System.Exception ex)
                                    {
                                        response.Message = " error";
                                        response.Success = false;
                                        // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(BookingReadRequest), Request));
                                    }



                                    //prepare Passenger Service get from system map with ssr code
                                    //segmentServicesList = GetSegmentServices(readResponse);
                                    //passengerServices = GetPassengerServices(request.ServiceCode);

                                    //   process get ssr from segmentfee
                                    //if (passengerServices != null && passengerServices.Count > 0)
                                    //{
                                    // get fee ssr
                                    /*
                                    IList<Entity.ServiceFee> ServiceFees = GetSSRFromSegmentFee(agencyCode, currencyRcd, languageRcd, numberOfPassenger, numberOfInfant, bNoVat, segmentServicesList, passengerServices);

                                    if (ServiceFees != null)
                                    {
                                        if (ServiceFees.Count > 0)
                                        {
                                            // map to response
                                            response.ServiceFees = ServiceFees.ToFeeMessage();
                                            response.Code = "000";
                                            response.Message = "Success";
                                            response.Success = true;
                                        }
                                        else
                                        {
                                            response.Code = "V011";
                                            response.Message = "Get SSR fee not found.";
                                            response.Success = false;
                                        }
                                    }
                                    else
                                    {
                                        response.Code = "V011";
                                        response.Message = "Get SSR failed.";
                                        response.Success = false;
                                    }
                                    */
                                    //}
                                    //else
                                    //{
                                    //    response.Code = "V012";
                                    //    response.Success = false;
                                    //    response.Message = "The SSR code is invalid.";

                                    //}
                                }
                                else
                                {
                                    response.Code = "P007";
                                    response.Message = "Booking not found.";
                                    response.Success = false;
                                }

                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetSpecialServices", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.ServiceCode, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
                // Logger.SaveLog("GetSpecialServices", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.ServiceCode, false));
            }
            return response;
        }

        public Avantik.Web.Service.Entity.REST.GetSpecialService.GetServicesRequest CreateGetServicesRequest(
         string agencyCode, string currency,
         IList<Message.Booking.FlightSegment> bookingSegments
         )
        {
            return new Avantik.Web.Service.Entity.REST.GetSpecialService.GetServicesRequest
            {
                AgencyCode = agencyCode,
                BookingSegments = AssignBookingSegmentValues(bookingSegments),
                Currency = currency
            };
        }

        public IList<Avantik.Web.Service.Entity.REST.GetSpecialService.BookingSegment> AssignBookingSegmentValues(
            IList<Message.Booking.FlightSegment> bookingSegments)
        {
            return bookingSegments.Select(segment => new Avantik.Web.Service.Entity.REST.GetSpecialService.BookingSegment
            {
                departure_date = segment.DepartureDate,
                airline_rcd = segment.AirlineRcd,
                flight_number = segment.FlightNumber,
                origin_rcd = segment.OriginRcd,
                destination_rcd = segment.DestinationRcd,
                booking_class_rcd = segment.BookingClassRcd
            }).ToList();
        }


        private DataSet CallDirectlyDatabase(string storedProcedureName, string parameterName, object parameterValue)
        {
            // Connection string should be retrieved from a configuration file or environment variable.
            string strSQLConnectionString = ConfigHelper.ToString("SQLConnectionString");

            using (SqlConnection connection = new SqlConnection(strSQLConnectionString))
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add the parameter to the command
                    command.Parameters.AddWithValue(parameterName, parameterValue);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataSet ds = new DataSet();
                        try
                        {
                            // Open the connection
                            connection.Open();

                            // Fill the DataSet with the result of the stored procedure
                            adapter.Fill(ds);
                        }
                        catch (System.Exception ex)
                        {
                            // Log the exception (you can replace this with your own logging mechanism)
                            Console.WriteLine("Error executing stored procedure: " + ex.Message);
                        }

                        return ds;
                    }
                }
            }
        }

        private IList<Entity.SpecialService> GetSpecialServiceList(string language)
        {
            IList<Entity.SpecialService> objSSRList = new  List<Entity.SpecialService>();

            language = string.IsNullOrEmpty(language) ? "EN" : language;

            try
            {

                //call sp
                var dsSSR = CallDirectlyDatabase("get_special_service", "@language", language);

                if (dsSSR != null && dsSSR.Tables[0].Rows.Count > 0)
                {
                    objSSRList = new List<Entity.SpecialService>();
                    foreach (DataRow dr in dsSSR.Tables[0].Rows)
                    {
                        Entity.SpecialService ssr = new Entity.SpecialService();

                        ssr.SpecialServiceRcd = dr["special_service_rcd"] != DBNull.Value ? ADODataHelpers.DBToString(dr, "special_service_rcd") : string.Empty;
                        ssr.DisplayName = dr["display_name"] != DBNull.Value ? ADODataHelpers.DBToString(dr, "display_name") : string.Empty;
                        ssr.TextAllowedFlag = dr["text_allowed_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "text_allowed_flag") : (byte)0;
                        ssr.ManifestFlag = dr["manifest_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "manifest_flag") : (byte)0;
                        ssr.TextRequiredFlag = dr["text_required_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "text_required_flag") : (byte)0;
                        ssr.ServiceOnRequestFlag = dr["service_on_request_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "service_on_request_flag") : (byte)0;
                        ssr.IncludePassengerNameFlag = dr["include_passenger_name_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_passenger_name_flag") : (byte)0;
                        ssr.IncludeFlightSegmentFlag = dr["include_flight_segment_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_flight_segment_flag") : (byte)0;
                        ssr.IncludeActionCodeFlag = dr["include_action_code_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_action_code_flag") : (byte)0;
                        ssr.IncludeNumberOfServiceFlag = dr["include_number_of_service_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_number_of_service_flag") : (byte)0;
                        ssr.IncludeCateringFlag = dr["include_catering_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_catering_flag") : (byte)0;
                        ssr.IncludePassengerAssistanceFlag = dr["include_passenger_assistance_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "include_passenger_assistance_flag") : (byte)0;
                        ssr.ServiceSupportedFlag = dr["service_supported_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "service_supported_flag") : (byte)0;
                        ssr.SendInterlineReplyFlag = dr["send_interline_reply_flag"] != DBNull.Value ? ADODataHelpers.DBToByte(dr, "send_interline_reply_flag") : (byte)0;
                        ssr.CutOffTime = dr["cut_off_time"] != DBNull.Value ? ADODataHelpers.DBToInt32(dr, "cut_off_time") : 0;
                        ssr.StatusCode = dr["status_code"] != DBNull.Value ? ADODataHelpers.DBToString(dr, "status_code") : string.Empty;
                       // ssr.PassengerSegmentServiceId = dr["passenger_segment_service_id"] != DBNull.Value ? ADODataHelpers.DBToGuid(dr, "passenger_segment_service_id") : Guid.Empty;
                      //  ssr.PassengerId = dr["passenger_id"] != DBNull.Value ? ADODataHelpers.DBToGuid(dr, "passenger_id") : Guid.Empty;
                      //  ssr.BookingSegmentId = dr["booking_segment_id"] != DBNull.Value ? ADODataHelpers.DBToGuid(dr, "booking_segment_id") : Guid.Empty;
                     //  ssr.ServiceText = dr["service_text"] != DBNull.Value ? ADODataHelpers.DBToString(dr, "service_text") : string.Empty;
                       // ssr.SpecialServiceStatusRcd = dr["special_service_status_rcd"] != DBNull.Value ? ADODataHelpers.DBToString(dr, "special_service_status_rcd") : string.Empty;

                        objSSRList.Add(ssr);
                    }

                }
            }
            catch(System.Exception ex)
            {
            }

            return objSSRList;
        }
        private IList<Entity.Booking.PassengerService> GetPassengerServices(string ServiceCode)
        {
            ISystemModelService objSystemService = SystemServiceFactory.CreateInstance();

            IList<Entity.Booking.PassengerService> passengerServices = new List<Entity.Booking.PassengerService>();

            // read from cache
            IList<Entity.SpecialService> ssr = GetSpecialServiceList(string.Empty);

            string[] ssrCode = ServiceCode.Split(',');

            Entity.Booking.PassengerService ps;

            if (!string.IsNullOrEmpty(ssrCode[0]))
            {
                for (int i = 0; i < ssrCode.Length; i++)
                {
                    for (int j = 0; j < ssr.Count; j++)
                    {
                        if (ssrCode[i].Trim().ToUpper().Equals(ssr[j].SpecialServiceRcd.Trim().ToUpper()))
                        {
                            ps = new Entity.Booking.PassengerService();

                            ps.SpecialServiceRcd = ssr[j].SpecialServiceRcd.ToUpper();
                            ps.DisplayName = ssr[j].DisplayName;
                            ps.ServiceText = ssr[j].ServiceText;
                            ps.ServiceOnRequestFlag = ssr[j].ServiceOnRequestFlag;
                            ps.CutOffTime = ssr[j].CutOffTime;
                            passengerServices.Add(ps);
                            break;
                        }
                    }
                }
            }

            return passengerServices;
        }

        private IList<Entity.SegmentService> GetSegmentServices(BookingReadResponse readRes)
        {
            IList<Entity.SegmentService> segmentServicesList = null;

            IList<Entity.Booking.FlightSegment> fs = readRes.BookingResponse.FlightSegments.ToListEntity();
            segmentServicesList = new List<Entity.SegmentService>();
            for (int i = 0; i < fs.Count; i++)
            {
                Entity.SegmentService segmentService = new Entity.SegmentService();
                segmentService.BookingSegmentId = fs[i].BookingSegmentId;
                segmentService.OriginRcd = fs[i].OriginRcd;
                segmentService.DestinationRcd = fs[i].DestinationRcd;
                segmentService.OdOriginRcd = fs[i].OdOriginRcd;
                segmentService.OdDestinationRcd = fs[i].OdDestinationRcd;
                segmentService.BookingClassRcd = fs[i].BookingClassRcd;
                segmentService.AirlineRcd = fs[i].AirlineRcd;
                segmentService.FlightNumber = fs[i].FlightNumber;
                segmentService.DepartureDate = fs[i].DepartureDate;
                segmentServicesList.Add(segmentService);
            }

            return segmentServicesList;
        }
        private SelectedAgency SelectedAgencyForAPI(Authentication objAuthen, Message.Booking.BookingHeader header)
        {
            SelectedAgency obj = new SelectedAgency();
            string selectedAgency = ConfigHelper.ToString("selectedAgency");

            switch (selectedAgency)
            {
                // currency always come from booking header
                case "BookingAgency":
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = header.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = header.LanguageRcd;
                    break;
                case "LoginAgency":
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = objAuthen.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = objAuthen.LanguageRcd;
                    break;
                default:
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = objAuthen.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = objAuthen.LanguageRcd;
                    break;
            }

            return obj;
        }

        public Message.Fee.Fee MapRESTMessageFeeToOrderingFee(Avantik.Web.Service.Entity.REST.GetFeeDefinition.MessageFee messageFee)
        {
            return new Message.Fee.Fee
            {
                AgencyCode = messageFee.AgencyCode,
                Comment = messageFee.Comment,
                CurrencyRcd = messageFee.CurrencyRcd,
                DestinationRcd = "",//messageFee.DestinationRcd,
                DisplayName = messageFee.DisplayName,
                FeeAmount = messageFee.FeeAmount,
                FeeAmountIncl = messageFee.FeeAmountIncl,
                FeeCategoryRcd = messageFee.FeeCategoryRcd,
                FeeId = messageFee.FeeId,
                FeeRcd = messageFee.FeeRcd,
                ManualFeeFlag = messageFee.ManualFeeFlag,
                MinimumFeeAmountFlag = messageFee.MinimumFeeAmountFlag,
                OdDestinationRcd = "",//messageFee.OdDestinationRcd,
                OdFlag = messageFee.OdFlag,
                OdOriginRcd = "",//messageFee.OdOriginRcd,
                OriginRcd = "",//messageFee.OriginRcd,
                VatPercentage = messageFee.VatPercentage
            };
        }









        public Avantik.Web.Service.Entity.Booking.Booking ReadBooking(string bookingId,
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
            Avantik.Web.Service.Entity.Booking.Booking booking = new Avantik.Web.Service.Entity.Booking.Booking();

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




        public Avantik.Web.Service.Entity.Booking.Booking OrderingBooking(string strBookingId,
                                  IList<Avantik.Web.Service.Entity.Booking.FlightSegment> newSegments,
                                  IList<Avantik.Web.Service.Entity.Booking.Mapping> newMappings,
                                  IList<Avantik.Web.Service.Entity.Booking.Fee> fees,
                                  IList<Avantik.Web.Service.Entity.Booking.PassengerService> services,
                                  IList<Avantik.Web.Service.Entity.Booking.Tax> taxes,
                                  string strUserId,
                                  string agencyCode,
                                  string currencyRcd,
                                  string languageRcd)
        {
            Avantik.Web.Service.Entity.Booking.Booking originalBooking = new Booking();
            Avantik.Web.Service.Entity.Booking.Booking newBooking = new Booking();

            ArrayList onlyAddNewSegment = new ArrayList();
            var listSegmentIdMappingChangeFlight = new List<KeyValuePair<string, string>>();
            var SegmentIdMappingChangeFlight = new List<KeyValuePair<string, string>>();
            bool foundInfant = false;
            bool changedFlightFlag = false;
            bool addNewSegmentFlag = false;
            bool IsProcessSuccess = true;


            //read
            string bookingReference = "";
            double bookingNumber = 0;
            bool bReadonly = true;
            bool bSeatLock = false;
            bool bReadHeader = true;
            bool bReadSegment = true;
            bool bReadPassenger = true;
            bool bReadRemark = true;
            bool bReadPayment = true;
            bool bReadMapping = true;
            bool bReadService = true;
            bool bReadTax = true;
            bool bReadFee = true;
            bool bReadOd = true;
            string releaseBookingId = "";
            string completeRemarkId = "";

            // Call the ReadBooking method
            originalBooking = ReadBooking(
                strBookingId,
                bookingReference,
                bookingNumber,
                bReadonly,
                bSeatLock,
                strUserId,
                bReadHeader,
                bReadSegment,
                bReadPassenger,
                bReadRemark,
                bReadPayment,
                bReadMapping,
                bReadService,
                bReadTax,
                bReadFee,
                bReadOd,
                releaseBookingId,
                completeRemarkId
            );



            // fill tax mapping 
            if (taxes != null && taxes.Count > 0 && newMappings != null && newMappings.Count > 0)
            {
                newMappings = taxes.FillTaxMapping(newMappings);
            }

            //if chnge flight: cancel flight
            if (newSegments != null && newSegments.Count > 0 && originalBooking != null)
            {
                //determine change or add
                foreach (Avantik.Web.Service.Entity.Booking.FlightSegment f in newSegments)
                {
                    //case change flight
                    if (f.SegmentStatusRcd.ToUpper() == "XX" && f.ExchangedSegmentId != Guid.Empty)
                    {
                        // key =old seg  value new seg
                        SegmentIdMappingChangeFlight.Add(new KeyValuePair<string, string>(f.BookingSegmentId.ToString(), f.ExchangedSegmentId.ToString()));

                        changedFlightFlag = true;
                    }
                    //case add new segment
                    else if (f.SegmentStatusRcd.ToUpper() == "NN" && f.ExchangedSegmentId == Guid.Empty)
                    {
                        onlyAddNewSegment.Add(f);
                        addNewSegmentFlag = true;
                    }
                }

                // case change flight need to remove new segment from onlyAddNewSegment
                for (int i = 0; i < onlyAddNewSegment.Count; i++)
                {
                    foreach (var element in SegmentIdMappingChangeFlight)
                    {
                        Avantik.Web.Service.Entity.Booking.FlightSegment ff = new Avantik.Web.Service.Entity.Booking.FlightSegment();
                        ff = (Avantik.Web.Service.Entity.Booking.FlightSegment)onlyAddNewSegment[i];
                       
                        if (ff.BookingSegmentId == new Guid(element.Value))  //value = new segment
                        {
                            onlyAddNewSegment.Remove(ff);
                        }
                    }
                }

                //if (alNewSegment != null && alNewSegment.Count > 0)
                //{
                //    addSegmentFlag = true;
                //}

                // ***** for change flight ***
                if (changedFlightFlag && SegmentIdMappingChangeFlight != null && SegmentIdMappingChangeFlight.Count > 0)
                {
                    //add flight first
                    BookingFlightRequest flightAddRequest = new BookingFlightRequest();
                    IList<Avantik.Web.Service.Message.Booking.Flight> flights = new List<Avantik.Web.Service.Message.Booking.Flight>();
                    foreach (var newFlight in newSegments)
                    {
                        if (newFlight.SegmentStatusRcd == "NN")
                        {
                            Avantik.Web.Service.Message.Booking.Flight flight = new Avantik.Web.Service.Message.Booking.Flight();
                            flight.AirlineRcd = newFlight.AirlineRcd;
                            flight.BoardingClassRcd = newFlight.BoardingClassRcd;
                            flight.BookingClassRcd = newFlight.BookingClassRcd;
                            flight.DepartureDate = newFlight.DepartureDate;
                            flight.DestinationRcd = newFlight.DestinationRcd;
                            flight.EticketFlag = newFlight.EticketFlag;

                            flight.FlightConnectionId = newFlight.FlightConnectionId;
                            flight.FlightId = newFlight.FlightId;
                            flight.OdDestinationRcd = newFlight.OdDestinationRcd;
                            flight.OdOriginRcd = newFlight.OdOriginRcd;
                            flight.OriginRcd = newFlight.OriginRcd;

                            flight.NumberOfUnits = newFlight.NumberOfUnits;
                            flight.AirlineRcd = newFlight.AirlineRcd;
                            flight.FlightNumber = newFlight.FlightNumber;
                            flights.Add(flight);

                        }
                    }

                    // process flight add
                    flightAddRequest.Adults = Convert.ToInt16(originalBooking.Header.NumberOfAdults);
                    flightAddRequest.AgencyCode = agencyCode;
                    flightAddRequest.BNoVat = false;
                    flightAddRequest.BookingId = strBookingId;
                    flightAddRequest.Children = Convert.ToInt16(originalBooking.Header.NumberOfChildren);
                    flightAddRequest.Infants = Convert.ToInt16(originalBooking.Header.NumberOfInfants);
                    flightAddRequest.Others = 0;
                    flightAddRequest.Currency = currencyRcd;
                    flightAddRequest.UserId = strUserId;
                    flightAddRequest.Flight = flights;

                    BookingFlightResponse flightAddResponse = new BookingFlightResponse();

                    flightAddResponse = BookFlight(flightAddRequest);


                    // flight add success  ==> need to process cancellation
                    if (flightAddResponse.Success)
                    {
                        IsProcessSuccess = UpdatedSegmentAndMapping(originalBooking, SegmentIdMappingChangeFlight, newSegments, newMappings,
                            strBookingId, strUserId, agencyCode);
                    }


                    // if change flight in same root: move SSR also
                    if (IsProcessSuccess && services != null && services.Count > 0)
                    {
                        services = moveSSR(SegmentIdMappingChangeFlight, originalBooking.Services, newSegments, services);
                    }
                }
                // ***** for change flight ***


                // add SSR
                if (services != null && services.Count > 0)
                {
                    foreach (Avantik.Web.Service.Entity.Booking.PassengerService ss in services)
                    {
                        if (originalBooking.FoundPassengerInfant(ss.PassengerId))
                        {
                            foundInfant = true;
                        }
                    }
                }

                //for add new segment
                if (addNewSegmentFlag)
                {
                    // for agg new segment
                    AddNewSegmentMapping(originalBooking, onlyAddNewSegment, newMappings, agencyCode, strUserId);
                }

              
            } //end change

            // update seat in old RS mapping
            if (IsProcessSuccess && newMappings != null && newMappings.Count > 0)
            {
                UpdatedSeatToMapping(newMappings, originalBooking.Mappings, strUserId);
            }

            if (changedFlightFlag)
            {
                //add done
                // oldbooking.Mappings.ToRecordset(ref rsMapping);
                //oldbooking.Segments.ToRecordset(ref rsSegment, true);

            }

            if (addNewSegmentFlag)
            {
                // mappings.ToRecordset(ref rsMapping);
              //  originalBooking.Mappings = newMappings;
                //  oldbooking.Segments.ToRecordset(ref rsSegment);
              //  originalBooking.Segments = newSegments;
            }

            //Add fee to RS
            if (fees != null && fees.Count > 0)
            {
                // map fee to rs
                //fees.ToRecordset(ref rsFees);
                foreach(Avantik.Web.Service.Entity.Booking.Fee f in fees)
                {
                   // f.FeeId = f.BookingFeeId;
                    f.BookingFeeId = f.BookingFeeId;
                    f.CreateBy = new Guid(strUserId);
                    f.UpdateBy = new Guid(strUserId);

                    originalBooking.Fees.Add(f);
                }               
            }

            //add Service
            if (services != null && services.Count > 0)
            {
                foreach (Avantik.Web.Service.Entity.Booking.PassengerService ps in services)
                {
                    ps.PassengerSegmentServiceId = Guid.NewGuid();
                    ps.CreateBy = new Guid(strUserId);
                    ps.UpdateBy = new Guid(strUserId);
                    originalBooking.Services.Add(ps);
                }
            }

            //add taxs
            if (taxes != null && taxes.Count > 0)
            {
                foreach (Avantik.Web.Service.Entity.Booking.Tax t in taxes)
                {
                    t.TaxId = Guid.NewGuid();
                    t.CreateBy = new Guid(strUserId);
                    t.UpdateBy = new Guid(strUserId);
                    originalBooking.Taxs.Add(t);
                }
            }

            UpdatedRSOldMapping(originalBooking, SegmentIdMappingChangeFlight, newSegments, newMappings,
                                  strBookingId, strUserId, agencyCode);

            //prepare request
            // Initialize the Booking and Flight objects
            Avantik.Web.Service.Entity.Booking.Booking restBooking = new Avantik.Web.Service.Entity.Booking.Booking();
            List<Avantik.Web.Service.Entity.Booking.Flight> restFlights = new List<Avantik.Web.Service.Entity.Booking.Flight>();


            restBooking.Header = originalBooking.Header;
            restBooking.Segments = originalBooking.Segments;
            restBooking.Passengers = originalBooking.Passengers;
            restBooking.Mappings = originalBooking.Mappings;
            restBooking.Payments = null;
            restBooking.Fees = originalBooking.Fees;
            restBooking.Services = originalBooking.Services;
            restBooking.Taxs = originalBooking.Taxs;
            restBooking.Remarks = null;
            restBooking.Quotes = null;

            foreach(var x in restBooking.Segments)
            {
                if (x.SegmentStatusRcd == "NN")
                    x.SegmentStatusRcd = "HK";
            }


            // Initialize and populate the BookingSaveRequest object
            var bookingSaveRequest = new Avantik.Web.Service.Entity.Booking.REST.BookingSaveRequest
            {
                booking = restBooking,
                createTickets = true,
                readBooking = false,
                readOnly = false,
                bSetLock = false,
                bCheckSeatAssignment = true,
                bCheckSessionTimeOut = true,
                AgencyCode = agencyCode,
                Currency = currencyRcd,
                Flight = restFlights,
                BookingId = strBookingId.ToString(),
                Adults = 1,
                Children = 0,
                Infants = 0,
                Others = 0,
                StrOthers = null,
                UserId = strUserId.ToString(),
                StrIpAddress = string.Empty,
                StrLanguageCode = languageRcd,
                BNoVat = false
            };



            // call api
            string baseURL = ConfigHelper.ToString("RESTURL");
            string apiURL = baseURL + "api/Booking/SaveBooking";

            var jsonContent = JsonConvert.SerializeObject(bookingSaveRequest);
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


            try
            {
                using (System.IO.Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(content, 0, content.Length);
                }
                using (HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        // Call the ReadBooking method
                        newBooking = ReadBooking(
                            strBookingId,
                            bookingReference,
                            bookingNumber,
                            bReadonly,
                            bSeatLock,
                            strUserId,
                            bReadHeader,
                            bReadSegment,
                            bReadPassenger,
                            bReadRemark,
                            bReadPayment,
                            bReadMapping,
                            bReadService,
                            bReadTax,
                            bReadFee,
                            bReadOd,
                            releaseBookingId,
                            completeRemarkId
                        );

                    }
                }

            }
            catch (System.Exception ex)
            {


            }

            return newBooking;
        }


        public BookingFlightResponse BookFlight(BookingFlightRequest Request)
        {
        //    IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            BookingFlightResponse response = new BookingFlightResponse();
            Booking booking = new Booking();

            IList<Entity.Booking.Flight> flights = null;

            try
            {
                //map from message to entity
                if (Request != null)
                {
                    flights = Request.Flight.ToListEntity();
                    response.Success = true;
                }
                else
                {
                    response.Message = "Map message to entity fail";
                    response.Success = false;
                }

                if (response.Success)
                {

                    // Initialize and populate the BookingSaveRequest object
                    var bookingFlightAddRequest = new Avantik.Web.Service.Entity.Booking.REST.FlightAdd.BookingFlightAddRequest
                    {
                        AgencyCode = Request.AgencyCode,
                        Currency = Request.Currency,
                        Flight = flights,
                        BookingId = Request.BookingId.ToString(),
                        Adults = Request.Adults,
                        Children = Request.Children,
                        Infants = Request.Infants,
                        Others = Request.Others,
                        StrOthers = null,
                        UserId = Request.UserId.ToString(),
                        StrIpAddress = Request.StrIpAddress,
                        StrLanguageCode = Request.StrLanguageCode,
                        BNoVat = Request.BNoVat
                    };

                    //call REST API
                    string baseURL = ConfigHelper.ToString("RESTURL");
                    //string baseURL = "https://localhost:7021/";//ConfigHelper.ToString("RESTURL");
                    string apiURL = baseURL + "api/Booking/BookFlight";

                    var jsonContent = JsonConvert.SerializeObject(bookingFlightAddRequest);
                    var content = System.Text.Encoding.UTF8.GetBytes(jsonContent);

                    var requestUri = new Uri(apiURL);

                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);

                    var userlogon = string.Format("{0}:{1}", "B2C", "B2C111");
                    byte[] bytes = Encoding.UTF8.GetBytes(userlogon);
                    string base64String = Convert.ToBase64String(bytes);
                    httpWebRequest.Headers["Authorization"] = "Basic " + base64String;


                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = content.Length;


                    try
                    {
                        using (System.IO.Stream requestStream = httpWebRequest.GetRequestStream())
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
                                    Avantik.Web.Service.Entity.Booking.REST.BookingRead.BookingRead  bookingRead = JsonConvert.DeserializeObject < Avantik.Web.Service.Entity.Booking.REST.BookingRead.BookingRead> (responseContent);

                                    booking.Header = bookingRead.Header.ToBookingMessage();
                                    booking.Segments = bookingRead.Segments.ToBookingMessage();
                                    booking.Passengers = bookingRead.Passengers.ToBookingMessage();
                                    booking.Mappings = bookingRead.Mappings.ToBookingMessage();


                                    if (booking.FindUSSegment() == true)
                                    {
                                        response.Message = "Selected flight is full";
                                        response.Success = false;
                                    }
                                    else
                                    {
                                        response.Message = "Success";
                                        response.Success = true;
                                    }
                                }

                            }
                        }

                    }
                    catch (System.Exception ex)
                    {

                    }

                }

            }
            catch (BookingException ex)
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            catch (System.Exception ex)
            {
                response.Message = ex.Message;
                response.Success = false;
                Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(BookingFlightRequest), Request));
            }
            return response;
        }

        private bool UpdatedRSOldMapping(Avantik.Web.Service.Entity.Booking.Booking oldBooking, List<KeyValuePair<string, string>> SegmentIdMappingChangeFlight,
                        IList<Avantik.Web.Service.Entity.Booking.FlightSegment> segments,
                        IList<Avantik.Web.Service.Entity.Booking.Mapping> mappings,
                        string strBookingId, string strUserId, string agencyCode
                        )
        {
            bool IsProcessSuccess = false;
            foreach (var element in SegmentIdMappingChangeFlight)
            {
                foreach (Avantik.Web.Service.Entity.Booking.FlightSegment f in segments)
                {
                    foreach (Avantik.Web.Service.Entity.Booking.Mapping newMapping in mappings)
                    {
                        foreach (Avantik.Web.Service.Entity.Booking.Mapping oldMapping in oldBooking.Mappings)
                        {
                            //if NN                             new seg = value                              value = new mapping                                              key = old mapping
                            if (f.SegmentStatusRcd == "NN" && f.BookingSegmentId == new Guid(element.Value) && new Guid(element.Value) == newMapping.BookingSegmentId && new Guid(element.Key) == oldMapping.BookingSegmentId && newMapping.PassengerId == oldMapping.PassengerId)
                            {
                                if (oldMapping.PassengerStatusRcd == "XX" )
                                {
                                    oldMapping.ETicketStatus = "E";
                                    oldMapping.ExchangedSegmentId = f.BookingSegmentId;
                                }


                            }

                        }
                    }
                }
            }

            return IsProcessSuccess;

        }

        private void UpdatedSeatToMapping(IList<Avantik.Web.Service.Entity.Booking.Mapping> mappings, IList<Avantik.Web.Service.Entity.Booking.Mapping> oldMapping,string strUserId)
        {
            foreach (Avantik.Web.Service.Entity.Booking.Mapping m in mappings)
            {
                foreach (Avantik.Web.Service.Entity.Booking.Mapping oldM in oldMapping)
                {
                    if (!string.IsNullOrEmpty(m.SeatNumber) && oldM.PassengerId == m.PassengerId && oldM.BookingSegmentId == m.BookingSegmentId)
                    {
                        oldM.SeatNumber = m.SeatNumber;

                        if (m.SeatNumber.Length > 1)
                        {
                            oldM.SeatColumn = m.SeatNumber.Substring(m.SeatNumber.Length - 1, 1);
                            oldM.SeatRow = Convert.ToInt16(m.SeatNumber.Substring(0, m.SeatNumber.Length - 1));
                            oldM.UpdateDateTime = DateTime.Now;
                            oldM.UpdateBy = new Guid(strUserId);
                        }
                    }

                }
            }

        }


        private void AddNewSegmentMapping(Booking originalBooking, ArrayList alNewSegment, IList<Avantik.Web.Service.Entity.Booking.Mapping> mappings, string agencyCode,string strUserId)
        {
            List<Avantik.Web.Service.Entity.Booking.Mapping> tmpNewMappings = new List<Avantik.Web.Service.Entity.Booking.Mapping>();

            for (int i = 0; i < alNewSegment.Count; i++)
            {
                Avantik.Web.Service.Entity.Booking.FlightSegment newSegment = new Avantik.Web.Service.Entity.Booking.FlightSegment();
                newSegment = (Avantik.Web.Service.Entity.Booking.FlightSegment)alNewSegment[i];

                if (newSegment.SegmentStatusRcd.ToUpper() == "NN")
                {
                    //newSegment.BookingId = originalBooking.Header.BookingId;
                    //newSegment.CreateBy = new Guid("4F938EDF-DB50-472D-BAE3-601BA130945C");
                    //newSegment.UpdateBy = new Guid("4F938EDF-DB50-472D-BAE3-601BA130945C");
                    //newSegment.CreateDateTime = DateTime.Now;
                    //newSegment.UpdateDateTime = DateTime.Now;
                    //newSegment.SegmentStatusRcd = "HK";

                    //// add NN to booking 
                    //originalBooking.Segments.Add(newSegment);

                    // add mapping
                    foreach (Avantik.Web.Service.Entity.Booking.Mapping newAddMapping in mappings)
                    {
                        if (newSegment.SegmentStatusRcd.ToUpper() == "NN" && newSegment.BookingSegmentId == newAddMapping.BookingSegmentId)
                        {
                            Avantik.Web.Service.Entity.Booking.Mapping newTempMM = new Avantik.Web.Service.Entity.Booking.Mapping();
                            foreach (Avantik.Web.Service.Entity.Booking.Mapping oldMapping in originalBooking.Mappings)
                            {
                                if (newAddMapping.PassengerId == oldMapping.PassengerId && newAddMapping.BookingSegmentId == newSegment.BookingSegmentId)
                                {
                                    newTempMM.BookingId = originalBooking.Header.BookingId;
                                    newTempMM.BookingSegmentId = newSegment.BookingSegmentId;
                                    newTempMM.PassengerId = oldMapping.PassengerId;
                                    newTempMM.Firstname = oldMapping.Firstname;
                                    newTempMM.Lastname = oldMapping.Lastname;
                                    newTempMM.PassengerTypeRcd = oldMapping.PassengerTypeRcd;
                                    newTempMM.PassengerStatusRcd = "OK";
                                    newTempMM.TitleRcd = oldMapping.TitleRcd;
                                    newTempMM.GenderTypeRcd = oldMapping.GenderTypeRcd;
                                    newTempMM.AirlineRcd = newSegment.AirlineRcd;
                                    newTempMM.FlightNumber = newSegment.FlightNumber;
                                    newTempMM.FlightId = newSegment.FlightId;
                                    newTempMM.FlightConnectionId = newSegment.FlightConnectionId;
                                    newTempMM.DepartureDate = newSegment.DepartureDate;
                                    newTempMM.DepartureTime = newSegment.DepartureTime;
                                    newTempMM.ArrivalDate = newSegment.ArrivalDate;
                                    newTempMM.ArrivalTime = newSegment.ArrivalTime;
                                    newTempMM.JourneyTime = newSegment.JourneyTime;
                                    newTempMM.OriginRcd = newSegment.OriginRcd;
                                    newTempMM.DestinationRcd = newSegment.DestinationRcd;
                                    newTempMM.OdOriginRcd = newSegment.OdOriginRcd;
                                    newTempMM.OdDestinationRcd = newSegment.OdDestinationRcd;
                                    newTempMM.BookingClassRcd = newSegment.BookingClassRcd;
                                    newTempMM.BoardingClassRcd = newSegment.BoardingClassRcd;
                                    newTempMM.AgencyCode = agencyCode;

                                    //EDWIBENEW
                                    newTempMM.CurrencyRcd = newAddMapping.CurrencyRcd;
                                    newTempMM.FareCode = newAddMapping.FareCode;
                                    newTempMM.EticketFlag = newAddMapping.EticketFlag;

                                    newTempMM.NetTotal = newAddMapping.NetTotal;

                                    newTempMM.FareAmount = newAddMapping.FareAmount;
                                    newTempMM.FareAmountIncl = newAddMapping.FareAmountIncl;
                                    newTempMM.AcctFareAmount = newAddMapping.AcctFareAmount;
                                    newTempMM.AcctFareAmountIncl = newAddMapping.AcctFareAmountIncl;

                                    newTempMM.YqAmount = newAddMapping.YqAmount;
                                    newTempMM.YqAmountIncl = newAddMapping.YqAmountIncl;
                                    newTempMM.AcctYqAmount = newAddMapping.AcctYqAmount;
                                    newTempMM.AcctYqAmountIncl = newAddMapping.AcctYqAmountIncl;

                                    newTempMM.TaxAmount = newAddMapping.TaxAmount;
                                    newTempMM.TaxAmountIncl = newAddMapping.TaxAmountIncl;
                                    newTempMM.AcctTaxAmount = newAddMapping.AcctTaxAmount;
                                    newTempMM.AcctTaxAmountIncl = newAddMapping.AcctTaxAmountIncl;

                                    newTempMM.PublicFareAmount = newAddMapping.PublicFareAmount;
                                    newTempMM.PublicFareAmountIncl = newAddMapping.PublicFareAmountIncl;

                                    newTempMM.SeatNumber = newAddMapping.SeatNumber;
                                    newTempMM.EndorsementText = newAddMapping.EndorsementText;
                                    newTempMM.RestrictionText = newAddMapping.RestrictionText;

                                    newTempMM.RefundCharge = newAddMapping.RefundCharge;
                                    newTempMM.RefundableFlag = newAddMapping.RefundableFlag;

                                    newTempMM.ExcludePricingFlag = newAddMapping.ExcludePricingFlag;

                                    newTempMM.CreateBy = new Guid(strUserId);
                                    newTempMM.UpdateBy = new Guid(strUserId);

                                    newTempMM.CreateDateTime = DateTime.Now;
                                    newTempMM.UpdateDateTime = DateTime.Now;

                                }

                            }

                            //add m
                            tmpNewMappings.Add(newTempMM);

                        }
                    }

                    //add new Segment
                    newSegment.BookingId = originalBooking.Header.BookingId;
                   // newSegment.CreateBy = new Guid("B3D77119-8EAD-4D2D-A5D0-10F3D7424732");
                  //  newSegment.UpdateBy = new Guid("B3D77119-8EAD-4D2D-A5D0-10F3D7424732");
                    newSegment.CreateDateTime = DateTime.Now;
                    newSegment.UpdateDateTime = DateTime.Now;
                    newSegment.SegmentStatusRcd = "HK";

                    newSegment.CreateBy = new Guid(strUserId);
                    newSegment.UpdateBy = new Guid(strUserId);

                    // add NN to booking 
                    originalBooking.Segments.Add(newSegment);

                }

            }

            foreach (var xm in tmpNewMappings)
            {
                originalBooking.Mappings.Add(xm);
            }

        }


        private IList<Avantik.Web.Service.Entity.Booking.PassengerService> moveSSR(List<KeyValuePair<string, string>> objSegmentIdMapping, IList<Avantik.Web.Service.Entity.Booking.PassengerService> oldservices, IList<Avantik.Web.Service.Entity.Booking.FlightSegment> segments, IList<Avantik.Web.Service.Entity.Booking.PassengerService> services)
        {
            if (services == null)
                services = new List<Avantik.Web.Service.Entity.Booking.PassengerService>();

            foreach (Avantik.Web.Service.Entity.Booking.FlightSegment oldF in segments)
            {
                foreach (Avantik.Web.Service.Entity.Booking.FlightSegment newF in segments)
                {
                    foreach (var element in objSegmentIdMapping)
                    {
                        if (oldF.SegmentStatusRcd.ToUpper() == "XX" && newF.SegmentStatusRcd.ToUpper() == "NN" && oldF.BookingSegmentId == new Guid(element.Key) && newF.BookingSegmentId == new Guid(element.Value) && oldF.OriginRcd == newF.OriginRcd && oldF.DestinationRcd == newF.DestinationRcd)
                        {
                            // action move SSR
                            foreach (var ss in oldservices)
                            {
                                if (ss.BookingSegmentId == oldF.BookingSegmentId)
                                {
                                    Avantik.Web.Service.Entity.Booking.PassengerService ps = new Avantik.Web.Service.Entity.Booking.PassengerService();
                                    ps.BookingSegmentId = newF.BookingSegmentId;
                                    ps.PassengerId = ss.PassengerId;
                                    ps.ServiceText = ss.ServiceText;
                                    ps.SpecialServiceRcd = ss.SpecialServiceRcd;
                                    ps.SpecialServiceStatusRcd = ss.SpecialServiceStatusRcd;
                                    ps.NumberOfUnits = ss.NumberOfUnits;
                                    ps.PassengerSegmentServiceId = Guid.NewGuid();
                                    services.Add(ps);
                                }

                            }
                        }
                    }
                }
            }

            return services;
        }

        private bool UpdatedSegmentAndMapping(Booking oldBooking,
                        List<KeyValuePair<string, string>> SegmentIdMappingChangeFlight,
                        IList<Avantik.Web.Service.Entity.Booking.FlightSegment> newSegments,
                        IList<Avantik.Web.Service.Entity.Booking.Mapping> newMappings,
                        string strBookingId, string strUserId, string agencyCode
                        )
        {
            bool IsProcessSuccess = false;
            foreach (var element in SegmentIdMappingChangeFlight)
            {
                foreach (Avantik.Web.Service.Entity.Booking.FlightSegment newSeg in newSegments)
                {
                    foreach (Avantik.Web.Service.Entity.Booking.Mapping newMapping in newMappings)
                    {
                        foreach (Avantik.Web.Service.Entity.Booking.Mapping oldMapping in oldBooking.Mappings)
                        {
                            //if NN                             new seg = value                              value = new mapping                                              key = old mapping
                            if (newSeg.SegmentStatusRcd == "NN" && newSeg.BookingSegmentId == new Guid(element.Value) && new Guid(element.Value) == newMapping.BookingSegmentId && new Guid(element.Key) == oldMapping.BookingSegmentId && newMapping.PassengerId == oldMapping.PassengerId)
                            {
                                // process cancel segment
                                // IsProcessSuccess = CancelSegment(strBookingId, element.Key.ToUpper(),"", 0, strUserId, agencyCode, false, false);// element.Key.ToUpper()) ;// segmentid
                                IsProcessSuccess = CancelSegment(strBookingId, element.Key.ToUpper(), strUserId, agencyCode, false, false);

                                // update old segment, mapping
                                if (IsProcessSuccess)
                                {
                                    foreach (Avantik.Web.Service.Entity.Booking.FlightSegment oldSeg in oldBooking.Segments)
                                    {
                                        if (oldSeg.BookingSegmentId.ToString().Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            oldSeg.SegmentStatusRcd = "XX";
                                        }
                                    }

                                    foreach (Avantik.Web.Service.Entity.Booking.Mapping oldM in oldBooking.Mappings)
                                    {
                                        if (oldM.BookingSegmentId.ToString().Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            oldM.PassengerStatusRcd = "XX";
                                        }
                                    }

                                    //change : add new NN
                                    //assign booking id to new segment
                                    newSeg.BookingId = oldBooking.Header.BookingId;
                                    newSeg.CreateBy = new Guid(strUserId);
                                    newSeg.UpdateBy = new Guid(strUserId);
                                    newSeg.CreateDateTime = DateTime.Now;
                                    newSeg.UpdateDateTime = DateTime.Now;

                                    // assign new seg id to ExchangedSegmentId in mapping
                                    oldMapping.ExchangedSegmentId = new Guid(element.Value);
                                    oldMapping.ExchangedDateTime = DateTime.Now;

                                    // from old mapping
                                    newMapping.Firstname = oldMapping.Firstname;
                                    newMapping.Lastname = oldMapping.Lastname;
                                    newMapping.PassengerTypeRcd = oldMapping.PassengerTypeRcd;
                                    newMapping.TitleRcd = oldMapping.TitleRcd;
                                    newMapping.GenderTypeRcd = oldMapping.GenderTypeRcd;

                                    newMapping.PassengerStatusRcd = "OK";
                                    newMapping.AgencyCode = agencyCode;

                                    // from new segment
                                    newMapping.AirlineRcd = newSeg.AirlineRcd;
                                    newMapping.FlightNumber = newSeg.FlightNumber;
                                    newMapping.FlightId = newSeg.FlightId;
                                    newMapping.FlightConnectionId = newSeg.FlightConnectionId;
                                    newMapping.DepartureDate = newSeg.DepartureDate;
                                    newMapping.DepartureTime = newSeg.DepartureTime;
                                    newMapping.ArrivalDate = newSeg.ArrivalDate;
                                    newMapping.ArrivalTime = newSeg.ArrivalTime;
                                    newMapping.JourneyTime = newSeg.JourneyTime;
                                    newMapping.OriginRcd = newSeg.OriginRcd;
                                    newMapping.DestinationRcd = newSeg.DestinationRcd;
                                    newMapping.OdOriginRcd = newSeg.OdOriginRcd;
                                    newMapping.OdDestinationRcd = newSeg.OdDestinationRcd;
                                    newMapping.BookingClassRcd = newSeg.BookingClassRcd;
                                    newMapping.BoardingClassRcd = newSeg.BoardingClassRcd;                                    
                                    newMapping.FareId = newSeg.FareId;

                                    // from new mapping
                                    newMapping.CreateBy = new Guid(strUserId);
                                    newMapping.UpdateBy = new Guid(strUserId);
                                    newMapping.CreateDateTime = DateTime.Now;
                                    newMapping.UpdateDateTime = DateTime.Now;
                                }
                                
                                // add new seg: NN
                                if (newSeg.SegmentStatusRcd == "NN")
                                {
                                   // f.SegmentStatusRcd = "HK";
                                    oldBooking.Segments.Add(newSeg);
                                }
                                    
                            }

                        }
                        // add New Mapping to booking
                      //  if (oldBooking.Mappings == null)
                         //   oldBooking.Mappings = new List<Avantik.Web.Service.Entity.Booking.Mapping>();

                        if (oldBooking.Mappings == null && newMapping.Firstname != null)
                            oldBooking.Mappings.Add(newMapping);

                    }
                }
            }
       
            return IsProcessSuccess;

        }


        public bool CancelSegment(string bookingId,
                            string bookingSegmentId,
                            string userId,
                            string agencyCode,
                            bool bWaveFee,
                            bool bVoid)
        {
            if (string.IsNullOrEmpty(bookingId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(bookingSegmentId))
                throw new ArgumentException("Booking ID, Booking Segment ID, and User ID cannot be null or empty.");

            string restAgency = ConfigHelper.ToString("restAgency");
            string restPassword = ConfigHelper.ToString("restPassword");

            var apiUrl = $"{ConfigHelper.ToString("RESTURL")}api/Booking/CancelBookingSegment";
            var cancelSuccess = false;

            var bookingRequest = new Avantik.Web.Service.Entity.Booking.REST.BookingCancel.BookingSegmentCancelRequest
            {
                UserId = new Guid(userId),
                AgencyCode = agencyCode,
                booking_id = new Guid(bookingId),
                booking_segment_id = new Guid(bookingSegmentId),
                IsVoidAllFees = bVoid
            };

            var jsonContent = JsonConvert.SerializeObject(bookingRequest);
            var content = Encoding.UTF8.GetBytes(jsonContent);

            var requestUri = new Uri(apiUrl);

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{restAgency}:{restPassword}"));

                httpWebRequest.Headers["Authorization"] = $"Basic {credentials}";
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.ContentLength = content.Length;

                using (var requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(content, 0, content.Length);
                }

                using (var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    cancelSuccess = httpResponse.StatusCode == HttpStatusCode.OK;
                }
            }

            catch (System.Exception ex)
            {
                // Log the general exception
                // e.g., Log.Error(ex, "Unexpected error occurred during booking segment cancellation.");
            }

            return cancelSuccess;
        }

        public SegmentCancelResponse CancelSegment(SegmentCancelRequest request)
        {
            //IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            SegmentCancelResponse response = new SegmentCancelResponse();
            string bookingReference = string.Empty;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            bool bWaveFee = false;
            bool bVoid = false;
            bool result = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        if (!String.IsNullOrEmpty(request.BookingId) && !String.IsNullOrEmpty(request.SegmentId))
                        {
                            userId = objAuthen.UserId.ToString();
                            agencyCode = objAuthen.AgencyCode;

                            if (DataType.IsGuid(request.SegmentId))
                            {
                                result = CancelSegment(request.BookingId,
                                                                          request.SegmentId,
                                                                          userId,
                                                                          agencyCode,
                                                                          bWaveFee,
                                                                          bVoid);

                                if (result)
                                {
                                    response.Code = "000";
                                    response.Message = "Cancel Segment Success.";
                                    response.Success = true;
                                }
                                else
                                {
                                    response.Code = "296";
                                    response.Message = "Cancel Segment Failed.";
                                    response.Success = false;
                                }
                            }
                            else
                            {
                                response.Code = "C003";
                                response.Message = "Segment ID is Invalid.";
                                response.Success = false;
                            }
                        }
                        else
                        {
                            response.Code = "B009";
                            response.Message = "BookingId and SegmentId are required.";
                            response.Success = false;
                        }
                    }
                    else
                    {
                        response.Success = objAuthen.ResponseSuccess;
                        response.Message = objAuthen.ResponseMessage;
                        response.Code = objAuthen.ResponseCode;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("CancelSegment", DateTime.Now, DateTime.Now, mex.Message, request.BookingId );
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                // Logger.SaveLog("CancelSegment", DateTime.Now, DateTime.Now, ex.Message, request.BookingId);
            }

            return response;
        }

        // validate from client request
        private IList<FlightSegmentResponse> SegmentValidation(IList<Message.OrderBooking.FlightSegment> flightSegment)
        {
            IList<FlightSegmentResponse> objFlightSegmentsResponse = null;
            FlightSegmentResponse segmentResponse = null;
            List<string> listStrName = new List<string>();
            string strName = string.Empty;
            bool bError = false;
            for (int i = 0; i < flightSegment.Count; i++)
            {
                //check flight duplicate
                if (flightSegment[i].segment_status_rcd != null && flightSegment[i].segment_status_rcd.ToUpper() == "NN")
                {
                    strName = "";
                    strName = flightSegment[i].origin_rcd + flightSegment[i].destination_rcd + flightSegment[i].departure_date + flightSegment[i].booking_class_rcd + flightSegment[i].flight_number;
                    listStrName.Add(strName);
                    if (listStrName.Distinct().Count() != listStrName.Count())
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "321";
                        segmentResponse.error_message = "Duplicate flight segment.";
                    }
                    else if (flightSegment[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Booking segment Id required.";
                    }
                    else if (flightSegment[i].flight_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Flight Id required.";
                    }
                    else if (flightSegment[i].origin_rcd == null || flightSegment[i].origin_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "100";
                        segmentResponse.error_message = "Invalid place of Departure Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].origin_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid city/airport code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].origin_rcd) == false)
                    {
                        if (flightSegment[i].origin_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid city/airport code.";
                        }
                    }
                    else if (flightSegment[i].destination_rcd == null || flightSegment[i].destination_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "101";
                        segmentResponse.error_message = "Invalid place of Destination Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].destination_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid city/airport code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].destination_rcd) == false)
                    {
                        if (flightSegment[i].destination_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid city/airport code.";
                        }
                    }
                    else if (flightSegment[i].departure_date == null || DateTime.Now > flightSegment[i].departure_date)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "102";
                        segmentResponse.error_message = "Invalid/Missing Departure Date.";
                    }
                    else if (flightSegment[i].airline_rcd == null || flightSegment[i].airline_rcd.ToString() == string.Empty)
                    {
                        if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].airline_rcd) == false)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "107";
                            segmentResponse.error_message = "Invalid Airline Designator/Vendor Supplier.";
                        }
                        else if (flightSegment[i].airline_rcd.Length != 2)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "107";
                            segmentResponse.error_message = "Invalid Airline Designator/Vendor Supplier.";
                        }
                    }
                    else if (flightSegment[i].flight_number == null || flightSegment[i].flight_number.Length == 0)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "114";
                        segmentResponse.error_message = "Invalid/Missing Flight Number.";
                    }
                    else if (string.IsNullOrEmpty(flightSegment[i].segment_status_rcd))
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Segment_status_rcd required.";
                    }
                    else if (flightSegment.Count > 8)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "132";
                        segmentResponse.error_message = "Exceeds Maximum Number of Segments.";
                    }
                }
                else if (flightSegment[i].segment_status_rcd == "XX")
                {
                    strName = "";
                    strName = flightSegment[i].booking_segment_id + flightSegment[i].origin_rcd + flightSegment[i].destination_rcd + flightSegment[i].departure_date + flightSegment[i].booking_class_rcd + flightSegment[i].flight_number;
                    listStrName.Add(strName);
                    if (listStrName.Distinct().Count() != listStrName.Count())
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "321";
                        segmentResponse.error_message = "Duplicate flight segment.";
                    }
                    else if (flightSegment[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Booking segment Id required.";
                    }
                    else if (flightSegment.Count > 8)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "132";
                        segmentResponse.error_message = "Exceeds Maximum Number of Segments.";
                    }
                    else if (string.IsNullOrEmpty(flightSegment[i].segment_status_rcd))
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Segment_status_rcd required.";
                    }
                    else if (flightSegment[i].od_origin_rcd == null || flightSegment[i].od_origin_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "100";
                        segmentResponse.error_message = "Invalid place of od_origin_rcd Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_origin_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid od_origin_rcd code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_origin_rcd) == false)
                    {
                        if (flightSegment[i].od_origin_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid od_origin_rcd code.";
                        }
                    }
                    else if (flightSegment[i].od_destination_rcd == null || flightSegment[i].od_destination_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "101";
                        segmentResponse.error_message = "Invalid place of od_destination_rcd Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_destination_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid od_destination_rcd code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_destination_rcd) == false)
                    {
                        if (flightSegment[i].od_destination_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid od_destination_rcd code.";
                        }
                    }


                }
                else
                {
                    bError = true;
                    segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                    segmentResponse.error_code = "320";
                    segmentResponse.error_message = "Invalid segment status.";
                }

                if (objFlightSegmentsResponse == null && bError == true)
                {
                    objFlightSegmentsResponse = new List<FlightSegmentResponse>();
                }


                if (segmentResponse != null)
                {
                    objFlightSegmentsResponse.Add(segmentResponse);
                }

                segmentResponse = null;
            }
            return objFlightSegmentsResponse;
        }

        private IList<MappingResponse> MappingValidation(IList<Message.OrderBooking.Mapping> mapping)
        {
            IList<MappingResponse> objMappingResponse = null;
            MappingResponse mappingResponse = null;
            bool bError = false;
            for (int i = 0; i < mapping.Count; i++)
            {
                if (mapping[i].booking_segment_id == Guid.Empty)
                {
                    bError = true;
                    mappingResponse = mapping[i].MapToOrderMappingResponse();
                    mappingResponse.error_code = "";
                    mappingResponse.error_message = "Booking Segment Id required.";
                }
                else if (mapping[i].passenger_id == Guid.Empty)
                {
                    bError = true;
                    mappingResponse = mapping[i].MapToOrderMappingResponse();
                    mappingResponse.error_code = "";
                    mappingResponse.error_message = "Passenger Id required.";
                }

                // End Validate

                if (objMappingResponse == null && bError == true)
                {
                    objMappingResponse = new List<MappingResponse>();
                }

                if (mappingResponse != null)
                {
                    objMappingResponse.Add(mappingResponse);
                }

                mappingResponse = null;
            }

            return objMappingResponse;
        }

        private IList<FeeResponse> FeeValidation(IList<Message.OrderBooking.Fee> fee)
        {
            IList<FeeResponse> objFeeResponse = null;
            FeeResponse feeResponse = null;
            bool bError = false;
            if (fee != null)
            {
                for (int i = 0; i < fee.Count; i++)
                {
                    if (fee[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "191";
                        feeResponse.error_message = "Passenger reference required.";
                    }
                    else if (fee[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "193";
                        feeResponse.error_message = "Segment reference required.";
                    }
                    else if (fee[i].fee_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "";
                        feeResponse.error_message = "Fee reference required.";
                    }
                    else if (string.IsNullOrEmpty(fee[i].fee_rcd))
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "";
                        feeResponse.error_message = "Fee_rcd required.";
                    }


                    if (objFeeResponse == null && bError == true)
                    {
                        objFeeResponse = new List<FeeResponse>();
                    }

                    if (feeResponse != null)
                    {
                        objFeeResponse.Add(feeResponse);
                    }

                    feeResponse = null;
                }
            }

            return objFeeResponse;
        }

        private IList<ServiceResponse> ServiceValidation(IList<Message.OrderBooking.Service> service)
        {
            IList<ServiceResponse> objServiceResponse = null;
            ServiceResponse serviceResponse = null;
            bool bError = false;
            if (service != null)
            {
                for (int i = 0; i < service.Count; i++)
                {
                    if (service[i].number_of_units > 9)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "167";
                        serviceResponse.error_message = "Invalid number of services specified in SSR.";
                    }
                    else if (service[i].service_text.Length > 300)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "180";
                        serviceResponse.error_message = "The SSR free text description length is in error.";
                    }
                    else if (service[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "191";
                        serviceResponse.error_message = "Passenger reference required.";
                    }
                    else if (service[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "193";
                        serviceResponse.error_message = "Segment reference required.";
                    }


                    if (objServiceResponse == null && bError == true)
                    {
                        objServiceResponse = new List<ServiceResponse>();
                    }

                    if (serviceResponse != null)
                    {
                        objServiceResponse.Add(serviceResponse);
                    }

                    serviceResponse = null;
                }
            }

            return objServiceResponse;
        }

        private IList<TaxResponse> TaxValitdation(IList<Message.OrderBooking.Tax> tax)
        {
            IList<TaxResponse> objTaxResponse = null;
            TaxResponse taxResponse = null;
            bool bError = false;
            if (tax != null)
            {
                for (int i = 0; i < tax.Count; i++)
                {
                    if (tax[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        taxResponse = tax[i].MapToOrderTaxResponse();
                        taxResponse.error_code = "191";
                        taxResponse.error_message = "Name reference required.";
                    }
                    else if (tax[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        taxResponse = tax[i].MapToOrderTaxResponse();
                        taxResponse.error_code = "193";
                        taxResponse.error_message = "Segment reference required.";
                    }

                    if (objTaxResponse == null && bError == true)
                    {
                        objTaxResponse = new List<TaxResponse>();
                    }

                    if (taxResponse != null)
                    {
                        objTaxResponse.Add(taxResponse);
                    }

                    taxResponse = null;
                }
            }
            return objTaxResponse;
        }

        private bool ValidServiceStatusInput(string serviceStatus)
        {
            if (string.IsNullOrEmpty(serviceStatus))
            {
                return false;
            }
            else if (serviceStatus == "SS")
            {
                return true;
            }
            else if (serviceStatus == "NN")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void InsertBookingChange(Guid userId, Guid bookingId)
        {
            string _connectionString = ConfigHelper.ToString("SQLConnectionString");
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("InsertBookingChangeQueueAPI", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters and their values
                    cmd.Parameters.Add("@strUserId", SqlDbType.UniqueIdentifier).Value = userId;
                    cmd.Parameters.Add("@strBookingId", SqlDbType.UniqueIdentifier).Value = bookingId;

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        // Console.WriteLine("Insert operation successful.");
                    }
                    catch (System.Exception ex)
                    {
                        // Handle exceptions
                        //  Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
            }
        }
        /*
        public BookingReadResponse ReadBooking(BookingReadRequest request)
        {
            BookingService objBookingService    = new BookingService();
            BookingReadResponse response        = new BookingReadResponse();
            string userId                       = string.Empty;
            string agencyCode                   = string.Empty;
            string currencyCode                 = string.Empty;
            string languageCode                 = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        request.Token = request.Token;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingReference) && request.BookingId == "00000000-0000-0000-0000-000000000000")
                        {
                            response.Code = "B007";
                            response.Success = false;
                            response.Message = "Booking reference is required.";
                        }
                        else if (string.IsNullOrEmpty(request.BookingReference) && string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "B007";
                            response.Success = false;
                            response.Message = "Booking reference is required.";
                        }
                        else
                        {
                            //read booking
                            response = objBookingService.ReadBooking(request);

                            if (response != null && response.Success == true)
                            {
                                response.Code = "000";
                                response.Message = "Success";
                                response.Success = true;
                            }
                            else
                            {
                                response.Code = "B008";
                                response.Message = "Read Booking failed.";
                                response.Success = false;
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("ReadBooking", DateTime.Now, DateTime.Now, mex.Message,XMLHelper.Serialize(request,false) );
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                //Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
                //Logger.SaveLog("ReadBooking", DateTime.Now, DateTime.Now, ex.Message, XMLHelper.Serialize(request, false));
            }

            return response;
        }
        
        public AvailabilityResponse GetAvailability(AvailabilityRequest request)
        {
            AvailabilityResponse response = new AvailabilityResponse();
            AvailabilityService obj = new AvailabilityService();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    
                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        request.Token = request.Token;

                        // validate request
                        request.Valid(response);

                        if (response.Success == true)
                        {
                            response = obj.GetAvailability(request);
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetAvailability", DateTime.Now, DateTime.Now, mex.Message, XMLHelper.Serialize(request,false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
                //Logger.SaveLog("GetAvailability", DateTime.Now, DateTime.Now, ex.Message, XMLHelper.Serialize(request, false));
            }

            return response;
        }

        public GetSeatMapResponse GetSeatMap(GetSeatMapRequest request)
        {
            BookingService objBookingService = new BookingService();
            GetSeatMapResponse response = new GetSeatMapResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;
            DataSet ds = new DataSet();

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    
                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        request.Token = request.Token;

                        // validate request
                        request.Valid(response, objAuthen);
                        // no need read booking
                        // call to booking service
                        if (response.Success)
                        {
                            //get seat map
                            // request = request.SeatMapRequest();
                            // response = objBookingService.GetSeatMap(request);

                            string connectionString = ConfigHelper.ToString("SQLConnectionString");
                            using (SqlConnection conn = new SqlConnection(connectionString))
                            {
                                //
                                SqlCommand sqlComm = new SqlCommand("get_seat_map_api_ordering", conn);
                               // SqlCommand sqlComm = new SqlCommand("get_seat_map_api", conn);
                                sqlComm.Parameters.AddWithValue("@origin", request.OriginRcd);
                                sqlComm.Parameters.AddWithValue("@destination", request.DestinationRcd);

                                if (string.IsNullOrEmpty(request.FlightId.ToString()))
                                    sqlComm.Parameters.AddWithValue("@flightid", null);
                                else
                                    sqlComm.Parameters.AddWithValue("@flightid", request.FlightId.ToString());

                                if (string.IsNullOrEmpty(request.BoardingClass))
                                    sqlComm.Parameters.AddWithValue("@boardingClass", "Y");
                                else
                                    sqlComm.Parameters.AddWithValue("@boardingClass", request.BoardingClass);


                                sqlComm.CommandType = CommandType.StoredProcedure;

                                SqlDataAdapter da = new SqlDataAdapter();
                                da.SelectCommand = sqlComm;

                                da.Fill(ds);

                            }

                            List<SeatMap> seatMaps = new List<SeatMap>();
                            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow dtRow in ds.Tables[0].Rows)
                                {
                                    // On all tables' columns
                                    SeatMap seatMap = new SeatMap();
                                    foreach (DataColumn dc in ds.Tables[0].Columns)
                                    {                                      
                                        seatMap.AircraftConfigurationCode = dtRow["aircraft_configuration_code"].ToString();
                                        //seatMap.AirlineRcd = dtRow["airline_rcd"].ToString();
                                        seatMap.AisleFlag = dtRow["aisle_flag"] != null ? Int16.Parse(dtRow["aisle_flag"].ToString()) : 0;
                                        seatMap.BassinetFlag = Int16.Parse(dtRow["bassinet_flag"].ToString());
                                        seatMap.BlockB2bFlag = Int16.Parse(dtRow["block_b2b_flag"].ToString());
                                        seatMap.BlockB2cFlag = Int16.Parse(dtRow["block_b2c_flag"].ToString());
                                        seatMap.BlockedFlag = Int16.Parse(dtRow["blocked_flag"].ToString());
                                        seatMap.BoardingClassRcd = dtRow["boarding_class_rcd"].ToString();
                                        //seatMap.BookingClassRcd = dtRow["booking_class_rcd"].ToString();
                                        // seatMap.DepartureDate = dtRow["departure_date"].ToString();
                                        seatMap.DestinationRcd = dtRow["destination_rcd"].ToString();
                                        seatMap.EmergencyExitFlag = Int16.Parse(dtRow["emergency_exit_flag"].ToString());
                                        // seatMap.EticketFlag = Int16.Parse(dtRow["eticket_flag"].ToString());
                                        //seatMap.FairId = dtRow["fair_id"].ToString();
                                        seatMap.FlightCheckInStatusRcd = dtRow["flight_check_in_status_rcd"].ToString();
                                        // seatMap.FlightConnectionId =   dtRow["fair_id"].ToString();
                                        seatMap.FlightId = new Guid(dtRow["flight_id"].ToString());
                                        // seatMap.FlightNumber =  Int16.Parse(dtRow["blocked_flag"].ToString());
                                        seatMap.FreeSeatingFlag= Int16.Parse(dtRow["free_seating_flag"].ToString());
                                        //seatMap.HanddicappedFlag = Int16.Parse(dtRow["handdicapped_flag"].ToString());
                                        seatMap.InfantFlag = Int16.Parse(dtRow["infant_flag"].ToString());
                                        seatMap.LayoutColumn = Int16.Parse(dtRow["layout_column"].ToString());
                                        seatMap.LayoutRow = Int16.Parse(dtRow["layout_row"].ToString());
                                        seatMap.LocationTypeRcd = dtRow["location_type_rcd"].ToString();
                                        seatMap.LowComfortFlag = Int16.Parse(dtRow["low_comfort_flag"].ToString());
                                        seatMap.NoChildFlag = Int16.Parse(dtRow["no_child_flag"].ToString());
                                        seatMap.NoInfantFlag = Int16.Parse(dtRow["no_infant_flag"].ToString()); 
                                        seatMap.NumberOfBays = Int16.Parse(dtRow["number_of_bays"].ToString());
                                        seatMap.NumberOfColumns = dtRow["number_of_columns"] != null && !string.IsNullOrEmpty(dtRow["number_of_columns"].ToString()) ? Int16.Parse(dtRow["number_of_columns"].ToString()) : 0;
                                        seatMap.NumberOfRows = dtRow["number_of_rows"] != null && !string.IsNullOrEmpty(dtRow["number_of_rows"].ToString()) ? Int16.Parse(dtRow["number_of_rows"].ToString()) : 0;
                                        // seatMap.OdDestinationRcd = dtRow["od_destination_rcd"].ToString();
                                        //seatMap.OdOriginRcd  = dtRow["od_destination_rcd"].ToString();passenger_count
                                        seatMap.OriginRcd = dtRow["origin_rcd"].ToString();
                                        seatMap.PassengerCount = dtRow["passenger_count"] != null && !string.IsNullOrEmpty(dtRow["passenger_count"].ToString()) ? Int16.Parse(dtRow["passenger_count"].ToString()) : 0;
                                        seatMap.SeatColumn = dtRow["seat_column"].ToString();
                                        seatMap.FeeRcd = dtRow["fee_rcd"].ToString();
                                        seatMap.SeatRow = dtRow["seat_row"] != null && !string.IsNullOrEmpty(dtRow["seat_row"].ToString()) ? Int16.Parse(dtRow["seat_row"].ToString()) : 0;
                                        seatMap.StretcherFlag = Int16.Parse(dtRow["stretcher_flag"].ToString());
                                        //  seatMap.TransitPoints = dtRow["transit_points"].ToString();
                                        //seatMap.TransitpointsName = dtRow["transit_points"].ToString(); 
                                         seatMap.UnAccompaniedMinorsFlag = Int16.Parse(dtRow["unaccompanied_minors_flag"].ToString());
                                        seatMap.WindowFlag = Int16.Parse(dtRow["window_flag"].ToString());
                                    }
                                    seatMaps.Add(seatMap);
                                }

                                response.Code = "000";
                                response.Message = "Success";
                                response.Success = true;
                                response.SeatMaps = seatMaps;
                            }
                            else
                            {
                                response.Code = "F006";
                                response.Message = "Failed.";
                                response.Success = false;

                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
               // Logger.SaveLog("GetSeatMap", DateTime.Now, DateTime.Now, mex.Message, XMLHelper.Serialize(request, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
               // Logger.SaveLog("GetSeatMap", DateTime.Now, DateTime.Now, ex.Message, XMLHelper.Serialize(request, false));
            }

            return response;
        }

        public GetSpecialServicesResponse GetSpecialServices(GetSpecialServicesRequest request)
        {
            GetSpecialServicesResponse response = new GetSpecialServicesResponse();
            IList<Entity.SegmentService> segmentServicesList = null;
            IList<Entity.Booking.PassengerService> passengerServices = null;

            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;
            int numberOfPassenger = 0;
            int numberOfInfant = 0;
            bool bNoVat = false;
            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else
                        {
                            // valid
                            request.Valid(response,objAuthen);
                            if (response.Success)
                            {
                                //read booking
                                BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);

                                if (readResponse != null && readResponse.Success == true)
                                {
                                    //set user id  and agency code from ...
                                    SelectedAgency objAgency = SelectedAgencyForAPI(objAuthen, readResponse.BookingResponse.Header);

                                    // prepare value
                                    userId = objAgency.UserId;
                                    agencyCode = objAgency.AgencyCode;
                                    currencyRcd = objAgency.CurrencyRcd;
                                    languageRcd = objAgency.LanguageRcd;
                                    numberOfPassenger = readResponse.BookingResponse.Header.NumberOfAdults + readResponse.BookingResponse.Header.NumberOfChildren;
                                    numberOfInfant = readResponse.BookingResponse.Header.NumberOfInfants;
                                    bNoVat = Convert.ToBoolean(readResponse.BookingResponse.Header.NoVatFlag == 0 ? false : true);

                                    //prepare Passenger Service get from system map with ssr code
                                    segmentServicesList = GetSegmentServices(readResponse);
                                    passengerServices = GetPassengerServices(request.ServiceCode);

                                    //   process get ssr from segmentfee
                                    if (passengerServices != null && passengerServices.Count > 0)
                                    {
                                        // get fee ssr
                                        IList<Entity.ServiceFee> ServiceFees = GetSSRFromSegmentFee(agencyCode, currencyRcd, languageRcd, numberOfPassenger, numberOfInfant, bNoVat, segmentServicesList, passengerServices);

                                        if (ServiceFees != null)
                                        {
                                            if (ServiceFees.Count > 0)
                                            {
                                                // map to response
                                                response.ServiceFees = ServiceFees.ToFeeMessage();
                                                response.Code = "000";
                                                response.Message = "Success";
                                                response.Success = true;
                                            }
                                            else
                                            {
                                                response.Code = "V011";
                                                response.Message = "Get SSR fee not found.";
                                                response.Success = false;
                                            }
                                        }
                                        else
                                        {
                                            response.Code = "V011";
                                            response.Message = "Get SSR failed.";
                                            response.Success = false;
                                        }

                                    }
                                    else
                                    {
                                        response.Code = "V012";
                                        response.Success = false;
                                        response.Message = "The SSR code is invalid.";

                                    }
                                }
                                else
                                {
                                    response.Code = "P007";
                                    response.Message = "Booking not found.";
                                    response.Success = false;
                                }

                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetSpecialServices", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.ServiceCode, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
               // Logger.SaveLog("GetSpecialServices", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.ServiceCode, false));
            }
            return response;
        }

        public BaggageFeesResponse GetBaggageFee(BaggageFeesRequest request)
        {
            BaggageFeesResponse response = new BaggageFeesResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else if (request.Baggage == null || request.Baggage.Count == 0)
                        {
                            response.Success = false;
                            response.Message = "Baggage is required.";
                            response.Code = "";
                        }
                        else
                        {

                            List<Entity.Booking.Fee> BagFees = new List<Entity.Booking.Fee>();
                            BookingReadResponse readResponse = new BookingReadResponse();

                            // get booking
                            readResponse = GetBooking(request.BookingId, request.Token);

                            // valid bag
                            Booking booking = new Booking();
                            booking.Header = readResponse.BookingResponse.Header.ToEntity();
                            booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                            booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                            booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();

                            // valid bag
                            request.Baggage.Valid(response, booking, objAuthen);

                            if (response.Success == true)
                            {
                                if (readResponse != null && readResponse.Success == true)
                                {
                                    //set user id  and agency code from ...
                                    SelectedAgency objAgency = SelectedAgencyForAPI(objAuthen, readResponse.BookingResponse.Header);

                                    // prepare value
                                    userId = objAgency.UserId;
                                    agencyCode = objAgency.AgencyCode;
                                    currencyRcd = objAgency.CurrencyRcd;
                                    languageRcd = objAgency.LanguageRcd;

                                    // get Bag Fee
                                    BagFees = CalBagageFee(readResponse, request.Baggage, agencyCode, languageRcd);

                                    // return bag fee
                                    if (BagFees != null && BagFees.Count > 0)
                                    {
                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Fees = BagFees.ToBookingMessage();

                                        response.Success = true;
                                        response.Message = "Success";
                                        response.Code = "000";
                                    }
                                    else
                                    {
                                        response.Code = "";
                                        response.Message = "Baggage Fee not found.";
                                        response.Success = false;

                                    }
                                }
                                else
                                {
                                    response.Code = "P007";
                                    response.Message = "Booking not found.";
                                    response.Success = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetBaggageFee", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.Baggage, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request.Baggage));
               // Logger.SaveLog("GetBaggageFee", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.Baggage, false));
            }

            return response;
        }

        
        public PassengerInfoResponse UpdatePassengerInfo(PassengerInfoRequest request)
        {
            PassengerInfoResponse response = new PassengerInfoResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            Booking bookingResponse = new Booking();

            bool bNoVat = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else if (request.PassengerInfo == null || request.PassengerInfo.Count == 0)
                        {
                            response.Code = "I001";
                            response.Success = false;
                            response.Message = "Passenger information is required.";
                        }
                        else
                        {
                            //read booking
                            BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);
                            Booking booking = new Booking();
                            if (readResponse != null && readResponse.Success == true)
                            {
                                booking.Header = readResponse.BookingResponse.Header.ToEntity();
                                booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                                booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                                booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();


                                // validate info request
                                request.PassengerInfo.Valid(response, booking, objAuthen);

                                if (response.Success == true)
                                {
                                    List<Message.Booking.Mapping> mList = new List<Message.Booking.Mapping>();
                                    List<Message.Booking.Passenger> pList = new List<Message.Booking.Passenger>();

                                    //set passenger mapping
                                    mList = SetPassengerInfoMapping(request.PassengerInfo);
                                    pList = SetPassengerInfoPassenger(request.PassengerInfo);

                                    // save booking
                                    bool result = objBookingService.UpdatePassengerInfo(request.BookingId,
                                                                                              pList.ToListEntity(),
                                                                                              mList.ToListEntity(),
                                                                                              userId,
                                                                                              agencyCode,
                                                                                              bNoVat);

                                    if (result)
                                    {
                                        //read booking
                                        readResponse = GetBooking(request.BookingId, request.Token);

                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Header = readResponse.BookingResponse.Header;
                                        response.BookingResponse.Passengers = readResponse.BookingResponse.Passengers;
                                        response.BookingResponse.Mappings = readResponse.BookingResponse.Mappings;
                                        response.Code = "000";
                                        response.Message = "Success";
                                        response.Success = true;
                                    }
                                    else
                                    {
                                        response.Code = "A002";
                                        response.Message = "Fail";
                                        response.Success = false;
                                    }

                                }
                            }
                            else
                            {
                                response.Code = "P007";
                                response.Message = "Booking not found.";
                                response.Success = false;
                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("UpdatePassengerInfo", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.PassengerInfo, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.SaveLog("UpdatePassengerInfo", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.PassengerInfo, false));
            }

            return response;
        }
        

            
        public UpdatedTicketResponse UpdateTicketBaggageAllowance(UpdatedTicketRequest request)
        {
            UpdatedTicketResponse response = new UpdatedTicketResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            Booking bookingResponse = new Booking();
            Booking booking = new Booking();

            bool bNoVat = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else if (request.UpdatedTicket == null || request.UpdatedTicket.Count == 0)
                        {
                            response.Code = "B001";
                            response.Success = false;
                            response.Message = "UpdatedTicket information  is required.";
                        }
                        else
                        {
                            //read booking
                            BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);

                            if (readResponse != null && readResponse.Success == true)
                            {
                                booking.Header = readResponse.BookingResponse.Header.ToEntity();
                                booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                                booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                                booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();


                                // validate info request
                                request.UpdatedTicket.Valid(response, booking, objAuthen);

                                if (response.Success == true)
                                {
                                    List<Message.Booking.Mapping> mList = new List<Message.Booking.Mapping>();

                                    //set passenger mapping
                                    mList = SetUpdateTicketMapping(request.UpdatedTicket);

                                    // save booking
                                    bool result = objBookingService.UpdateTicket(request.BookingId,
                                                                                              mList.ToListEntity(),
                                                                                              userId,
                                                                                              agencyCode,
                                                                                              bNoVat);

                                    if (result)
                                    {
                                        //read booking
                                        readResponse = GetBooking(request.BookingId, request.Token);

                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Header = readResponse.BookingResponse.Header;
                                        response.BookingResponse.Passengers = readResponse.BookingResponse.Passengers;
                                        response.BookingResponse.Mappings = readResponse.BookingResponse.Mappings;
                                        response.Code = "000";
                                        response.Message = "Success";
                                        response.Success = true;
                                    }
                                    else
                                    {
                                        response.Code = "A002";
                                        response.Message = "Fail";
                                        response.Success = false;
                                    }
                                }
                            }
                            else
                            {
                                response.Code = "P007";
                                response.Message = "Booking not found.";
                                response.Success = false;
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("UpdatedTicket", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.UpdatedTicket, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.SaveLog("UpdatedTicket", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.UpdatedTicket, false));
            }

            return response;
        }
        

            
        public BookingCancelResponse CancelBooking(BookingCancelRequest request)
        {
            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            BookingCancelResponse response = new BookingCancelResponse();
            string bookingReference = string.Empty;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            double bookingNumber = 0;
            bool bWaveFee = false;
            bool bVoid = false;
            bool result = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        if (!String.IsNullOrEmpty(request.BookingId))
                        {
                            userId = objAuthen.UserId.ToString();
                            agencyCode = objAuthen.AgencyCode;

                            request.Valid(response,objAuthen);

                            if (response.Success == true)
                            {
                                result = objBookingService.CancelBooking(request.BookingId,
                                                                          bookingReference,
                                                                          bookingNumber,
                                                                          userId,
                                                                          agencyCode,
                                                                          bWaveFee,
                                                                          bVoid);

                                if (result)
                                {
                                    response.Code = "000";
                                    response.Message = "Success";
                                    response.Success = true;
                                }
                                else
                                {
                                    response.Code = "295";
                                    response.Message = "Cancel Booking Failed.";
                                    response.Success = false;
                                }
                            }
                        }
                        else
                        {
                            response.Code = "B009";
                            response.Message = "Booking ID is required.";
                            response.Success = false;
                        }
                    }
                    else
                    {
                        response.Success = objAuthen.ResponseSuccess;
                        response.Message = objAuthen.ResponseMessage;
                        response.Code = objAuthen.ResponseCode;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }

            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("CancelBooking", DateTime.Now, DateTime.Now, mex.Message, request.BookingId);
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.SaveLog("CancelBooking", DateTime.Now, DateTime.Now, ex.Message, request.BookingId);
            }

            return response;
        }
    
        public SegmentCancelResponse CancelSegment(SegmentCancelRequest request)
        {
            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            SegmentCancelResponse response = new SegmentCancelResponse();
            string bookingReference = string.Empty;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            bool bWaveFee = false;
            bool bVoid = false;
            bool result = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        if (!String.IsNullOrEmpty(request.BookingId) && !String.IsNullOrEmpty(request.SegmentId))
                        {
                            userId = objAuthen.UserId.ToString();
                            agencyCode = objAuthen.AgencyCode;

                            if (DataType.IsGuid(request.SegmentId))
                            {
                                result = objBookingService.CancelSegment(request.BookingId,
                                                                          request.SegmentId,
                                                                          userId,
                                                                          agencyCode,
                                                                          bWaveFee,
                                                                          bVoid);

                                if (result)
                                {
                                    response.Code = "000";
                                    response.Message = "Cancel Segment Success.";
                                    response.Success = true;
                                }
                                else
                                {
                                    response.Code = "296";
                                    response.Message = "Cancel Segment Failed.";
                                    response.Success = false;
                                }
                            }
                            else
                            {
                                response.Code = "C003";
                                response.Message = "Segment ID is Invalid.";
                                response.Success = false;
                            }
                        }
                        else
                        {
                            response.Code = "B009";
                            response.Message = "BookingId and SegmentId are required.";
                            response.Success = false;
                        }
                    }
                    else
                    {
                        response.Success = objAuthen.ResponseSuccess;
                        response.Message = objAuthen.ResponseMessage;
                        response.Code = objAuthen.ResponseCode;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("CancelSegment", DateTime.Now, DateTime.Now, mex.Message, request.BookingId );
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.SaveLog("CancelSegment", DateTime.Now, DateTime.Now, ex.Message, request.BookingId);
            }

            return response;
        }
        

        public PaymentFeeResponse GetPaymentFee(PaymentFeeRequest request)
        {
            Booking objBookingResponse = new Booking();

            PaymentFeeResponse response = new PaymentFeeResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;

            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            Booking bookingResponse = new Booking();

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();

                        if (string.IsNullOrEmpty(request.PaymentFee.AgencyCode))
                            agencyCode = objAuthen.AgencyCode;
                        else
                            agencyCode = request.PaymentFee.AgencyCode;

                        // validate request
                        if (request.PaymentFee == null )
                        {
                            response.Code = "P005";
                            response.Success = false;
                            response.Message = "PaymentFee  is required.";
                        }
                        else
                        {
                            // valid
                            response = request.Valid(response);

                            if (response.Success == true)
                            {
                                BookingReadResponse readResponse = new BookingReadResponse();

                                    // get payment type fee
                                    IPaymentService objPaymentService = PaymentServiceFactory.CreateInstance();
                                    IList<Entity.Booking.Fee> fees = objPaymentService.GetPaymentTypeFee(request.PaymentFee.Origin.Trim().ToUpper(),
                                        request.PaymentFee.Destination.Trim().ToUpper(),
                                        request.PaymentFee.FormOfPaymentRcd.Trim().ToUpper(),
                                        request.PaymentFee.FormOfPaymentSubtypeRcd.Trim().ToUpper(),
                                        request.PaymentFee.CurrencyRcd.Trim().ToUpper(),
                                        agencyCode, 
                                        DateTime.MinValue);

                                        // map to response
                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Fees = fees.ToBookingMessage();//objBookingResponse.Fees.ToBookingMessage();
                                        if (response.BookingResponse.Fees != null && response.BookingResponse.Fees.Count > 0)
                                        {
                                            response.Code = "000";
                                            response.Message = "Success.";
                                            response.Success = true;
                                        }
                                        else
                                        {
                                            response.Code = "V017";
                                            response.Message = "Payment not found.";
                                            response.Success = false;
                                        }
                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetPaymentFee", DateTime.Now, DateTime.Now, mex.Message,  XMLHelper.Serialize(request.PaymentFee, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                //Logger.SaveLog("GetPaymentFee", DateTime.Now, DateTime.Now, ex.Message,  XMLHelper.Serialize(request.PaymentFee, false));

            }

            return response;
        }

        public GetFeeResponse GetFeeDefinition(GetFeeDefinitionRequest request)
        {
            GetFeeResponse response = new GetFeeResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        currencyRcd = request.Currency;
                        languageRcd = objAuthen.LanguageRcd;

                        //valid
                        request.Valid(response);

                        if (response.Success)
                        {
                            // map data
                            GetFeeRequest getFeeRequest = new GetFeeRequest();
                            getFeeRequest = request.GetFeeDefinitionMapRequest(agencyCode, currencyRcd, languageRcd);

                            BookingService objBookingService = new BookingService();
                            response = objBookingService.GetFee(getFeeRequest);
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetFeeDefinition", DateTime.Now, DateTime.Now, mex.Message, "");
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                //Logger.SaveLog("GetFeeDefinition", DateTime.Now, DateTime.Now, ex.Message, "");

            }

            return response;
        }

        public AgencyResponse GetAgencyProfile(AgencyRequest request)
        {
            AgencyResponse response = new AgencyResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;

            IAgencyService objAgencyService = AgencyServiceFactory.CreateInstance();

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        Entity.Agency.Agent agent = new Entity.Agency.Agent();
                        Message.Agency.Agent agency = new Message.Agency.Agent();

                        agent = objAgencyService.GetAgencySessionProfile(agencyCode, userId);
                        agency = agent.ToAgentLogonMessage();

                        if (agency != null)
                        {
                            response.Agent = agency;
                            response.Code = "000";
                            response.Success = true;
                            response.Message = "Success.";
                        }
                        else
                        {
                            response.Agent = null;
                            response.Code = "A002";
                            response.Success = false;
                            response.Message = "Fail.";
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
               // Logger.SaveLog("GetPaymentFee", DateTime.Now, DateTime.Now, mex.Message,"");
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
               // Logger.SaveLog("GetPaymentFee", DateTime.Now, DateTime.Now, ex.Message,"");

            }

            return response;
        }

        
        public ContactDetailResponse UpdateContactDetail(ContactDetailRequest request)
        {
            ContactDetailResponse response = new ContactDetailResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            Booking bookingResponse = new Booking();
            Booking booking = new Booking();
            bool bNoVat = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required.";
                        }
                        else if (request.ContactDetail == null )
                        {
                            response.Code = "C001";
                            response.Success = false;
                            response.Message = "ContactDetail is required.";
                        }
                        else
                        {
                            //read booking
                            BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);

                            if (readResponse != null && readResponse.Success == true)
                            {
                                booking.Header = readResponse.BookingResponse.Header.ToEntity();
                                booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                                booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                                booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();


                                // validate info request
                                request.Valid(response, booking, objAuthen);

                                if (response.Success == true)
                                {
                                    Message.Booking.BookingHeader header = new Message.Booking.BookingHeader();

                                    //set passenger BookingHeader

                                    header.BookingId = new Guid(request.BookingId);
                                    header.ContactName = request.ContactDetail.ContactName;
                                    header.ContactEmail = request.ContactDetail.ContactEmail;
                                    header.PhoneMobile = request.ContactDetail.PhoneMobile;
                                    header.PhoneBusiness = request.ContactDetail.PhoneBusiness;
                                    header.PhoneFax = request.ContactDetail.PhoneFax;
                                    header.PhoneHome = request.ContactDetail.PhoneHome;
                                    header.LanguageRcd = request.ContactDetail.Language;
                                    header.MobileEmail = request.ContactDetail.MobileEmail;

                                    // save booking
                                    bool result = objBookingService.UpdateContactDetail(request.BookingId,
                                                                                              header.ToEntity(),
                                                                                              userId,
                                                                                              agencyCode,
                                                                                              bNoVat);



                                    if (result)
                                    {
                                        //read booking
                                        readResponse = GetBooking(request.BookingId, request.Token);

                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Header = readResponse.BookingResponse.Header;
                                        response.BookingResponse.Passengers = readResponse.BookingResponse.Passengers;
                                        response.BookingResponse.Mappings = readResponse.BookingResponse.Mappings;
                                        response.Code = "000";
                                        response.Message = "Success";
                                        response.Success = true;
                                    }
                                    else
                                    {
                                        response.Code = "A002";
                                        response.Message = "Fail";
                                        response.Success = false;
                                    }

                                }
                            }
                            else
                            {
                                response.Code = "P007";
                                response.Message = "Booking not found.";
                                response.Success = false;
                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate web service failed.";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("ContactDetail", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.ContactDetail, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
                //Logger.SaveLog("ContactDetail", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.ContactDetail, false));
            }

            return response;
        }
        

        public BookingOrderResponse ModifyBooking(OrderBookingRequest request)
        {
            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            ModifyBookingResponse modifyResponse = new ModifyBookingResponse();
            BookingModifyResponse res = new BookingModifyResponse();
            BookingOrderResponse response = new BookingOrderResponse();
            Booking bookingResponse = new Booking();
            Booking booking = new Booking();
            Guid passengerIdDefault = Guid.Empty;
            Guid segmentIdDefault = Guid.Empty;
            Guid bookingId = Guid.Empty;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;
            string ActionCode = string.Empty;
            string originRCD = string.Empty;
            string destinationRCD = string.Empty;
            //decimal totalOutStandingBalance = 0;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // test get log
                    if (request.Token.Contains("dev@bravo"))
                    {
                        modifyResponse = LogInformation(request.Token);
                    }
                    else
                    {
                        // valid token
                        Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                        if (objAuthen.ResponseSuccess == true)
                        {
                            userId = objAuthen.UserId.ToString();
                            agencyCode = objAuthen.AgencyCode;
                            currencyRcd = objAuthen.CurrencyRcd;
                            languageRcd = objAuthen.LanguageRcd;

                            if (request != null)
                            {
                                //Validate input parameter.
                                if (String.IsNullOrEmpty(request.BookingId))
                                {
                                    modifyResponse.Code = "";
                                    modifyResponse.Success = false;
                                    modifyResponse.Message = "Booking Id required.";
                                }
                                else
                                {
                                    bookingId = new Guid(request.BookingId);
                                    IList<Message.Booking.FlightSegment> BookingSegments = null;
                                    IList<Message.Booking.Mapping> Mappings = null;
                                    IList<Message.Booking.Fee> Fees = null;
                                    IList<Message.Booking.PassengerService> Services = null;
                                    IList<Message.Booking.Tax> Taxes = null;

                                    if(request.BookingSegments != null )
                                    {
                                        response.BookingSegments = SegmentValidation(request.BookingSegments);

                                        BookingSegments = request.BookingSegments.FillOrderToBookingMessage(bookingId, objAuthen.UserId);

                                    }

                                    if (request.Mappings != null)
                                    {
                                        response.Mappings = MappingValidation(request.Mappings);

                                        Mappings = request.Mappings.FillOrderToBookingMessage(bookingId, objAuthen.UserId, agencyCode);

                                    }

                                    if (request.Fees != null)
                                    {
                                        response.Fees = FeeValidation(request.Fees);
                                        Fees = request.Fees.FillOrderToBookingMessage(bookingId, objAuthen.UserId, agencyCode);
                                    }

                                    if (request.Services != null)
                                    {
                                        response.Services = ServiceValidation(request.Services);
                                        Services = request.Services.FillOrderToBookingMessage(objAuthen.UserId);
                                    }

                                    if (request.Taxes != null)
                                    {
                                        response.Taxes = TaxValitdation(request.Taxes);
                                        Taxes = request.Taxes.FillOrderToBookingMessage(objAuthen.UserId);
                                    }
                                    // fill total tax to mapping
                                    //if (request.Taxes != null && request.Taxes.Count > 0)
                                    //{
                                    //    request.Taxes.FillMapping(Mappings);
                                    //}

                                    if (response.BookingSegments == null && response.Passengers == null && response.Mappings == null && response.Fees == null &&
                                        response.Taxes == null && response.Remarks == null && response.Services == null && response.Payments == null &&
                                        response.BookingHeader == null)
                                    {
                                        bool validSegment = false;

                                        if (request.BookingSegments != null && request.BookingSegments.Count > 0)
                                        {
                                            foreach (Message.OrderBooking.FlightSegment f in request.BookingSegments)
                                            {
                                                if (f.segment_status_rcd.ToUpper() == "XX" && f.exchanged_segment_id == Guid.Empty)
                                                {
                                                    validSegment = true;
                                                    break;
                                                }
                                            }
                                        }



                                        if (!validSegment)
                                        {
                                            bookingResponse = objBookingService.OrderingBooking(
                                                                                request.BookingId,
                                                                                BookingSegments.ToListEntity(),
                                                                                Mappings.ToListEntity(),
                                                                                Fees.ToListEntity(),
                                                                                Services.ToListEntity(),
                                                                                Taxes.ToListEntity(),
                                                                                userId,
                                                                                agencyCode,
                                                                                currencyRcd,
                                                                                languageRcd);
                                        }
                                        else
                                        {
                                            modifyResponse.Code = "";
                                            modifyResponse.Success = false;
                                            modifyResponse.Message = "Invalid ExchangedSegmentId in Segment status XX.";
                                        }

                                        if (bookingResponse != null && bookingResponse.Header != null && bookingResponse.Mappings != null)
                                        {
                                            BookingReadRequest readRequest = new BookingReadRequest();
                                            readRequest.BookingId = request.BookingId;
                                            readRequest.Token = request.Token;


                                            // call InsertBookingChangeQueueAPI
                                            bool IsInsertBookingChangeQueue = false;
                                            foreach (Message.Booking.Mapping m in Mappings)
                                            {
                                                if(!string.IsNullOrEmpty(m.SeatNumber))
                                                {
                                                    IsInsertBookingChangeQueue = true;break;
                                                }
                                            }

                                            if(IsInsertBookingChangeQueue)
                                            {
                                                InsertBookingChange(new Guid(userId), bookingId);
                                            }

                                            //get response
                                            Avantik.Web.Service.Message.Booking.BookingReadResponse wsReadResponse = ReadBooking(readRequest);

                                            //reset modifyResponse
                                            modifyResponse.Success = true;

                                            if (wsReadResponse.Success == true)
                                            {
                                                //Set Booking Header inforamtion.
                                                response.BookingHeader = wsReadResponse.BookingResponse.Header.MapBookingHeaderResponse();

                                                response.BookingHeader.error_code = "000";
                                                response.BookingHeader.error_message = "SUCCESS";

                                                //Fill Flight segment information.
                                                response.BookingSegments = wsReadResponse.BookingResponse.FlightSegments.MapBookingSegmentsResponse();
                                                for (int i = 0; i < response.BookingSegments.Count; i++)
                                                {
                                                    response.BookingSegments[i].error_code = "000";
                                                    response.BookingSegments[i].error_message = "SUCCESS";
                                                }

                                                ////Fill passenger information.
                                                response.Passengers = wsReadResponse.BookingResponse.Passengers.MapPassengersResponse();
                                                for (int i = 0; i < response.Passengers.Count; i++)
                                                {
                                                    response.Passengers[i].error_code = "000";
                                                    response.Passengers[i].error_message = "SUCCESS";
                                                }

                                                ////Fill Mapping information.
                                                response.Mappings = wsReadResponse.BookingResponse.Mappings.MapMappingsResponse();
                                                for (int i = 0; i < response.Mappings.Count; i++)
                                                {
                                                    response.Mappings[i].error_code = "000";
                                                    response.Mappings[i].error_message = "SUCCESS";
                                                }

                                                ////Fill fee information.
                                                if (wsReadResponse.BookingResponse.Fees != null && wsReadResponse.BookingResponse.Fees.Count > 0)
                                                {
                                                    response.Fees = wsReadResponse.BookingResponse.Fees.MapFeesResponse();
                                                    for (int i = 0; i < response.Fees.Count; i++)
                                                    {
                                                        response.Fees[i].error_code = "000";
                                                        response.Fees[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill tax information.
                                                if (wsReadResponse.BookingResponse.Taxs != null && wsReadResponse.BookingResponse.Taxs.Count > 0)
                                                {
                                                    response.Taxes = wsReadResponse.BookingResponse.Taxs.MapTaxsResponse();
                                                    for (int i = 0; i < response.Taxes.Count; i++)
                                                    {
                                                        response.Taxes[i].error_code = "000";
                                                        response.Taxes[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill remark information.
                                                if (wsReadResponse.BookingResponse.Remarks != null && wsReadResponse.BookingResponse.Remarks.Count > 0)
                                                {
                                                    response.Remarks = wsReadResponse.BookingResponse.Remarks.MapRemarksResponse();
                                                    for (int i = 0; i < response.Remarks.Count; i++)
                                                    {
                                                        response.Remarks[i].error_code = "000";
                                                        response.Remarks[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill service information.
                                                if (request.Services != null && request.Services.Count > 0)
                                                {
                                                    response.Services = wsReadResponse.BookingResponse.Services.MapServicesResponse();
                                                    for (int i = 0; i < response.Services.Count; i++)
                                                    {
                                                        response.Services[i].error_code = "000";
                                                        response.Services[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                //Fill payment information
                                                if (wsReadResponse.BookingResponse.Payments != null && wsReadResponse.BookingResponse.Payments.Count > 0)
                                                {
                                                    response.Payments = wsReadResponse.BookingResponse.Payments.MapPaymentsResponse();
                                                    for (int i = 0; i < response.Payments.Count; i++)
                                                    {
                                                        response.Payments[i].error_code = "000";
                                                        response.Payments[i].error_message = "SUCCESS";
                                                    }
                                                }

                                                response.Code = "000";
                                                response.Success = true;
                                                response.Message = "SUCCESS";
                                            }
                                            else
                                            {
                                                response.Code = "129";
                                                response.Success = false;
                                                response.Message = "Read booking failed.";
                                            }
                                        }

                                        if(modifyResponse.Success==false)
                                        {
                                            response.Code = modifyResponse.Code;
                                            response.Success = false;
                                            response.Message = modifyResponse.Message;
                                        }

                                    }


                                    //if (bookingResponse != null && bookingResponse.Mappings != null)
                                    //{
                                    //    modifyResponse.BookingResponse = new BookingResponse();
                                    //    modifyResponse.BookingResponse.Header = bookingResponse.Header.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.FlightSegments = bookingResponse.Segments.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Passengers = bookingResponse.Passengers.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Mappings = bookingResponse.Mappings.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Payments = bookingResponse.Payments.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Remarks = bookingResponse.Remarks.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Fees = bookingResponse.Fees.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Taxs = bookingResponse.Taxs.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Services = bookingResponse.Services.ToBookingMessage();
                                    //    modifyResponse.BookingResponse.Quotes = bookingResponse.Quotes.ToBookingMessage();

                                    //    // for pri
                                    //    // cal totalOutStanding
                                    //    if (ActionCode.ToUpper() == "PRI")
                                    //    {
                                    //        totalOutStandingBalance = booking.CalOutStandingBalance(bookingResponse.Fees, bookingResponse.Mappings);
                                    //        modifyResponse.TotalOutstandingBalance = totalOutStandingBalance + "";
                                    //        modifyResponse.Code = "000";
                                    //        modifyResponse.Message = "Success";
                                    //        modifyResponse.Success = true;
                                    //    }
                                    //    else // CON
                                    //    {
                                    //        totalOutStandingBalance = booking.CalOutStandingBalance(bookingResponse.Fees, bookingResponse.Mappings);
                                    //        modifyResponse.TotalOutstandingBalance = totalOutStandingBalance + "";

                                    //        if (modifyResponse.BookingResponse != null && modifyResponse.BookingResponse.Header != null && modifyResponse.BookingResponse.Mappings != null)
                                    //        {
                                    //            modifyResponse.Code = "000";
                                    //            modifyResponse.Message = "Success";
                                    //            modifyResponse.Success = true;
                                    //        }
                                    //    }
                                    //}

                                }
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Invalid token.";
                            response.Code = "005";
                        }
                    } // test get log
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token.";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = "300";
                response.Message = mex.Message;
                response.Success = false;

                // clear obj Response
               // ClearResponse(modifyResponse.BookingResponse);
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;

                // clear obj Response
               // ClearResponse(modifyResponse.BookingResponse);
            }
            finally
            {
                // save log
                SaveOrderingLog(request, response);
            }

            return response;
        }


        // list check
        // check null before trim  in valid
        // check read booking not found
        // update by

        //not use this function
        public AddSpecialServiceResponse SaveSpecialService(AddSpecialServiceRequest request)
        {
            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            AddSpecialServiceResponse response = new AddSpecialServiceResponse();
            Booking objBookingRequest = new Booking();
            Booking objBookingResponse = new Booking();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            bool IsDupSSR = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);
                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required";
                        }
                        else if (request.Services == null || request.Services.Count == 0)
                        {
                            response.Code = "";
                            response.Success = false;
                            response.Message = "Services is required";
                        }
                        else
                        {
                            // prepare payment obj
                            IList<Entity.Booking.Payment> payments = request.Payments.ToListEntity();

                            // get booking obj  from  calculate fee
                            CalculateSpecialServiceFeesRequest calculateFeeRequest = new CalculateSpecialServiceFeesRequest();
                            calculateFeeRequest.BookingId = request.BookingId;
                            calculateFeeRequest.Services = request.Services;
                            calculateFeeRequest.Token = request.Token;

                            CalculateFeesBookingResponse calculateFeeResponse = new CalculateFeesBookingResponse();
                            CalculateSpecialServiceFeesResponse resRead = new CalculateSpecialServiceFeesResponse();
                            resRead = AddSpecialServices(calculateFeeRequest);

                            // map to booking obj
                            objBookingRequest.Header = resRead.BookingResponse.Header.ToEntity();
                            objBookingRequest.Segments = resRead.BookingResponse.FlightSegments.ToListEntity();
                            objBookingRequest.Passengers = resRead.BookingResponse.Passengers.ToListEntity();
                            objBookingRequest.Mappings = resRead.BookingResponse.Mappings.ToListEntity();
                            objBookingRequest.Fees = resRead.BookingResponse.Fees.ToListEntity();
                            objBookingRequest.Services = resRead.BookingResponse.Services.ToListEntity();
                            objBookingRequest.Remarks = resRead.BookingResponse.Remarks.ToListEntity();


                            // check duplicate SSR / segment
                            foreach (Message.SSR.Service s in request.Services)
                            {
                                foreach (Entity.Booking.PassengerService ps in objBookingRequest.Services)
                                {
                                    if (new Guid(s.BookingSegmentId) == ps.BookingSegmentId && s.SpecialServiceCode == ps.SpecialServiceRcd)
                                    {
                                        IsDupSSR = true;
                                        break;
                                    }
                                }
                            }
                            if (!IsDupSSR)
                            {
                                // call save payment and save booking
                                // bool z = objBookingService.COBPayment(objBookingRequest, payments, userId);

                                if (objBookingResponse != null)
                                {
                                    response.BookingResponse = new BookingResponse();
                                    response.BookingResponse.Services = objBookingResponse.Services.ToBookingMessage();

                                    response.Code = "000";
                                    response.Message = "Success";
                                    response.Success = true;
                                }
                                else
                                {
                                    response.Code = "300";
                                    response.Message = "Add SSR fail";
                                    response.Success = false;
                                }
                            }
                            else
                            {
                                response.Code = "";
                                response.Message = "duplicate SSR";
                                response.Success = false;
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate fail";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = ex.Message;
                response.Success = false;
                //Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
            }
            return response;
        }

        //not use this function
        public NameChangeResponse GetNameChangeFee(NameChangeRequest request)
        {
            Booking objBookingResponse = new Booking();

            NameChangeResponse response = new NameChangeResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyRcd = string.Empty;
            string languageRcd = string.Empty;

            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            Booking bookingResponse = new Booking();

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required";
                        }
                        else if (request.NameChange == null || request.NameChange.Count == 0)
                        {
                            response.Code = "";
                            response.Success = false;
                            response.Message = "Passenger information  is required";
                        }
                        else
                        {
                            BookingReadResponse readResponse = new BookingReadResponse();
                            Booking booking = new Booking();

                            // get booking
                            readResponse = GetBooking(request.BookingId, request.Token);
                            // this code not succuss not use this function
                            if (readResponse != null && readResponse.Success == true)
                            {
                                booking.Header = readResponse.BookingResponse.Header.ToEntity();
                                booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                                booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                                booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();

                            }

                            // validate name change request
                            response = request.NameChange.Valid(response, objAuthen, booking);

                            if (response.Success == true)
                            {
                                // BookingReadResponse readResponse = new BookingReadResponse();

                                // get booking
                                readResponse = GetBooking(request.BookingId, request.Token);

                                if (readResponse != null && readResponse.Success == true)
                                {
                                    //set user id  and agency code from ...
                                    SelectedAgency objAgency = SelectedAgencyForAPI(objAuthen, readResponse.BookingResponse.Header);
                                    // prepare value
                                    userId = objAgency.UserId;
                                    agencyCode = objAgency.AgencyCode;
                                    currencyRcd = objAgency.CurrencyRcd;
                                    languageRcd = objAgency.LanguageRcd;

                                    //cal fee
                                    objBookingResponse.Fees = CalNameChangeFee(readResponse, request.NameChange, agencyCode, currencyRcd, languageRcd, userId);

                                    if (objBookingResponse != null)
                                    {
                                        // map to response
                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Fees = objBookingResponse.Fees.ToBookingMessage();
                                        if (response.BookingResponse.Fees != null && response.BookingResponse.Fees.Count > 0)
                                        {
                                            response.Code = "000";
                                            response.Message = "Success";
                                            response.Success = true;
                                        }
                                    }
                                    else
                                    {
                                        response.Message = "calculate name change fee fail";
                                        response.Success = false;
                                    }
                                }

                            }

                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate fail";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetNameChangeFee", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.NameChange, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = ex.Message;
                response.Success = false;
                //Logger.SaveLog("GetNameChangeFee", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.NameChange, false));

            }

            return response;
        }

        //not use this function
        public CalculateSpecialServiceFeesResponse AddSpecialServices(CalculateSpecialServiceFeesRequest request)
        {
            CalculateSpecialServiceFeesResponse response = new CalculateSpecialServiceFeesResponse();
            Booking objBookingResponse = new Booking();
            string userId = string.Empty;
            string agencyCode = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (objAuthen.B2bAllowService == "0")
                        {
                            response.Code = "";
                            response.Success = false;
                            response.Message = "Agency not allowed services";
                        }
                        else if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required";
                        }
                        else if (request.Services == null || request.Services.Count == 0)
                        {
                            response.Code = "";
                            response.Success = false;
                            response.Message = "Services is required";
                        }
                        else
                        {
                            // get booking
                            BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);

                            if (readResponse != null && readResponse.Success == true)
                            {
                                // Valid ssr
                                Booking booking = new Booking();
                                booking.Services = readResponse.BookingResponse.Services.ToListEntity();
                                request.Services.Valid(response, booking, objAuthen);

                                if (response.Success == true)
                                {
                                    // call fee
                                    objBookingResponse = CalSSRFee(readResponse, request.Services, objAuthen.AgencyCode, "",objAuthen.LanguageRcd);

                                    if (objBookingResponse != null
                                        && objBookingResponse.Fees != null && objBookingResponse.Fees.Count > 0
                                        && objBookingResponse.Services != null && objBookingResponse.Services.Count > 0
                                        )
                                    {
                                        // map to response
                                        response.BookingResponse = new BookingResponse();
                                        response.BookingResponse.Fees = objBookingResponse.Fees.ToBookingMessage();
                                        response.BookingResponse.Services = objBookingResponse.Services.ToBookingMessage();

                                        response.Code = "000";
                                        response.Message = "Success";
                                        response.Success = true;
                                    }
                                    else
                                    {
                                        response.Message = "calculate SSR fee fail";
                                        response.Success = false;
                                    }
                                }

                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate fail";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("AddSpecialServices", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.Services, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = ex.Message;
                response.Success = false;
                // Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
                //Logger.SaveLog("AddSpecialServices", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.Services, false));
            }
            return response;
        }

        //not use this function
        public CalculateSeatFeesResponse SelectSeat(CalculateSeatFeesRequest request)
        {
            CalculateSeatFeesResponse response = new CalculateSeatFeesResponse();
            Booking booking = new Booking();
            string userId = string.Empty;
            string agencyCode = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required";
                        }
                        else if (request.Seats == null || request.Seats.Count == 0)
                        {
                            response.Code = "";
                            response.Success = false;
                            response.Message = "Seat  is required";
                        }
                        else
                        {
                            //read booking
                            BookingReadResponse readResponse = GetBooking(request.BookingId, request.Token);

                            if (readResponse != null && readResponse.Success == true)
                            {

                                booking.Header = readResponse.BookingResponse.Header.ToEntity();
                                booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
                                booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
                                booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();


                                // validate seat
                                request.Seats.Valid(response, booking, objAuthen);

                                if (response.Success == true)
                                {
                                    if (request.Seats != null && request.Seats.Count > 0)
                                    {
                                        // get fee
                                      //  CalculateFeesBookingResponse calSeatFeeResponse = CalSeatFee(readResponse, request.Seats, objAuthen.AgencyCode,"");

                                       // response.BookingResponse = new BookingResponse();
                                       // response.BookingResponse.Fees = calSeatFeeResponse.Fees;

                                        if (response.BookingResponse.Fees != null && response.BookingResponse.Fees.Count > 0)
                                        {
                                            response.Success = true;
                                            response.Message = "Success";
                                            response.Code = "000";
                                        }
                                        else
                                        {
                                            response.Code = "";
                                            response.Message = "Seat Fee not found";
                                            response.Success = false;
                                        }
                                    }
                                    else
                                    {
                                        response.Success = false;
                                    }
                                }

                            }
                            else
                            {
                                response.Code = "P007";
                                response.Message = "Booking not found";
                                response.Success = false;
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate fail";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("SelectSeat", DateTime.Now, DateTime.Now, mex.Message, XMLHelper.Serialize(request.Seats, false));
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = ex.Message;
                response.Success = false;
               // Logger.SaveLog("SelectSeat", DateTime.Now, DateTime.Now, ex.Message, XMLHelper.Serialize(request.Seats, false));
            }

            return response;
        }

        //not use this function
        public CalculateChangeFlightFeesResponse GetChangeFlightFees(CalculateChangeFlightFeesRequest request)
        {
            IBookingModelService objBookingService = BookingServiceFactory.CreateInstance();
            CalculateChangeFlightFeesResponse response = new CalculateChangeFlightFeesResponse();
            Booking bookingResponse = new Booking();
            IList<Entity.Booking.Flight> flights = null;
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;
            bool bNoVat = false;

            try
            {
                if (!string.IsNullOrEmpty(request.Token))
                {
                    // valid token
                    Avantik.Web.Service.Entity.Authentication objAuthen = Avantik.Web.Service.Infrastructrue.Authentication.Authenticate(request.Token);

                    if (objAuthen.ResponseSuccess)
                    {
                        //set user id  and agency code from obj authen
                        userId = objAuthen.UserId.ToString();
                        agencyCode = objAuthen.AgencyCode;
                        currencyCode = objAuthen.CurrencyRcd;
                        languageCode = objAuthen.LanguageRcd;

                        // validate request
                        if (string.IsNullOrEmpty(request.BookingId))
                        {
                            response.Code = "P010";
                            response.Success = false;
                            response.Message = "Booking id is required";
                        }
                        else if (request.ModifyFlights == null || request.ModifyFlights.Count == 0)
                        {
                            response.Code = "P012";
                            response.Success = false;
                            response.Message = "Flight is required";
                        }
                        else
                        {
                            request.ModifyFlights.Valid(response, objAuthen);

                            if (response.Success)
                            {

                                //flight 
                                if (request.ModifyFlights != null && request.ModifyFlights.Count > 0)
                                {
                                    flights = request.ModifyFlights.ToListEntity();
                                }

                                bookingResponse = objBookingService.ChangeFlightFee(request.BookingId,
                                                                                      flights,
                                                                                      objAuthen.UserId.ToString(),
                                                                                      objAuthen.AgencyCode,
                                                                                      currencyCode,
                                                                                      languageCode,
                                                                                      bNoVat);

                                response.BookingResponse = new BookingResponse();
                                List<Avantik.Web.Service.Entity.Booking.Fee> listFee = new List<Entity.Booking.Fee>();

                                foreach (Avantik.Web.Service.Entity.Booking.Fee f in bookingResponse.Fees)
                                {
                                    if (f.FeeRcd == "PNRUPD")
                                    {
                                        listFee.Add(f);
                                    }
                                }

                                response.BookingResponse.Fees = listFee.ToBookingMessage();

                                if (response.BookingResponse.Fees != null && response.BookingResponse.Fees.Count > 0)
                                {
                                    response.Success = true;
                                    response.Message = "Success";
                                    response.Code = "000";
                                }
                            }
                        }
                    }
                    else
                    {
                        response.Code = "A006";
                        response.Message = "Athenticate fail";
                        response.Success = false;
                    }
                }
                else
                {
                    response.Code = "A004";
                    response.Message = "Require Token";
                    response.Success = false;
                }
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
                //Logger.SaveLog("GetChangeFlightFees", DateTime.Now, DateTime.Now, mex.Message, request.BookingId + XMLHelper.Serialize(request.ModifyFlights, false));

            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = ex.Message;
                response.Success = false;
                //  Logger.Instance(Logger.LogType.Mail).WriteLog(ex, XMLHelper.JsonSerializer(typeof(GetSpecialServicesRequest), request));
               // Logger.SaveLog("GetChangeFlightFees", DateTime.Now, DateTime.Now, ex.Message, request.BookingId + XMLHelper.Serialize(request.ModifyFlights, false));
            }

            return response;
        }

        #region Private Method 

        private void ClearResponse(BookingResponse BookingResponse)
        {
            if (BookingResponse != null)
            {
                BookingResponse.Header = null;
                BookingResponse.Services = null;
                BookingResponse.FlightSegments = null;
                BookingResponse.Mappings = null;
                BookingResponse.Passengers = null;
                BookingResponse.Fees = null;
            }
        }

        //private IList<Entity.Booking.Fee> StoreEntityFee(IList<Entity.Booking.Fee> objStoreEntityFee,IList<Entity.Booking.Fee> fees)
        //{
        //    if (objStoreEntityFee != null && fees != null && fees.Count > 0)
        //    {
        //        foreach (Entity.Booking.Fee f in fees)
        //        {
        //            objStoreEntityFee.Add(f);
        //        }
        //    }
        //    return objStoreEntityFee;
        //}

        private SelectedAgency SelectedAgencyForAPI(Authentication objAuthen, Message.Booking.BookingHeader header)
        {
            SelectedAgency obj = new SelectedAgency();
            string selectedAgency = ConfigHelper.ToString("selectedAgency");

            switch (selectedAgency)
            {
                    // currency always come from booking header
                case "BookingAgency":
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = header.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = header.LanguageRcd;
                    break;
                case "LoginAgency":
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = objAuthen.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = objAuthen.LanguageRcd;
                    break;
                default:
                    obj.UserId = objAuthen.UserId.ToString();
                    obj.AgencyCode = objAuthen.AgencyCode;
                    obj.CurrencyRcd = header.CurrencyRcd;
                    obj.LanguageRcd = objAuthen.LanguageRcd;
                    break;
            }

            return obj;
        }
        private IList<Entity.ServiceFee> GetSSRFromSegmentFee(
                                                                string agencyCode,
                                                                string currencyCode,
                                                                string languageCode,
                                                                int numberOfPassenger,
                                                                int numberOfInfant,
                                                                bool bNoVat,
                                                                IList<Entity.SegmentService> segmentServicesList, 
                                                                IList<Entity.Booking.PassengerService> passengerServices)
                                                            {
            IFeeService objFeeService = FeeServiceFactory.CreateInstance();
            IList<Entity.ServiceFee> ServiceFees = null;

            //   process get ssr fee
            if (passengerServices != null && passengerServices.Count > 0)
            {
                // get fee ssr
                ServiceFees = objFeeService.GetSegmentFee(agencyCode,
                                                        currencyCode,
                                                        languageCode,
                                                        numberOfPassenger,
                                                        numberOfInfant,
                                                        passengerServices,
                                                        segmentServicesList,
                                                        true,
                                                        bNoVat);
            }

            return ServiceFees;
        }

        private CalculateFeesBookingResponse CalAssignlSeatFee(BookingReadResponse readResponse, IList<Entity.SeatAssign> Seats, string agencyCode, string CurrencyRcd)
        {
            BookingService objService = new BookingService();
            Booking booking = new Booking();
            booking.Header = readResponse.BookingResponse.Header.ToEntity();
            booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
            booking.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
            booking.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();

            //set seat to mapping
            booking.Mappings = SetSeatMapping(Seats, booking.Mappings);

            CalculateFeesSeatAssignmentRequest calSeatFeeRequest = new CalculateFeesSeatAssignmentRequest();
            CalculateFeesBookingResponse calSeatFeeResponse = new CalculateFeesBookingResponse();

            calSeatFeeRequest.AgencyCode = agencyCode;

            if (string.IsNullOrEmpty(CurrencyRcd))
                calSeatFeeRequest.Currency = readResponse.BookingResponse.Header.CurrencyRcd;
            else
                calSeatFeeRequest.Currency = CurrencyRcd;

            calSeatFeeRequest.bNovat = false;
            calSeatFeeRequest.Booking = new BookingRequest();
            calSeatFeeRequest.Booking.Header = booking.Header.ToBookingMessage();
            calSeatFeeRequest.Booking.Mappings = booking.Mappings.ToBookingMessage();
            calSeatFeeRequest.Booking.FlightSegments = booking.Segments.ToBookingMessage();
            calSeatFeeRequest.Booking.Passengers = booking.Passengers.ToBookingMessage();

            calSeatFeeResponse = objService.CalculateFeesSeatAssignment(calSeatFeeRequest);

            return calSeatFeeResponse;
        }

        //private BookingReadResponse GetBooking(string bookingId, string token)
        //{
        //    BookingService objService = new BookingService();
        //    BookingReadResponse readResponse = new BookingReadResponse();
        //    BookingReadRequest readRequest = new BookingReadRequest();

        //    readRequest.BookingId = bookingId;
        //    readRequest.Token = token;
        //    try
        //    {
        //        readResponse = objService.ReadBooking(readRequest);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        //Logger.SaveLog("GetBooking", DateTime.Now, DateTime.Now, ex.Message, bookingId);
        //    }

        //    return readResponse;

        //}

        private List<Avantik.Web.Service.Entity.Booking.Fee> CalBagageFee(BookingReadResponse readResponse, IList<Baggage> Baggage, string agencyCode, string languageCode)
        {
            IFeeService objFeeService = FeeServiceFactory.CreateInstance();
            List<Avantik.Web.Service.Entity.Booking.Fee> BagFees = new List<Avantik.Web.Service.Entity.Booking.Fee>();
            Booking booking = new Booking();
            bool bNoVat = false;

            booking.Header = readResponse.BookingResponse.Header.ToEntity();
            booking.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();

            bNoVat = Convert.ToBoolean(booking.Header.NoVatFlag == 0 ? false : true);

            for (int i = 0; i < Baggage.Count; i++)
            {
                IList<Avantik.Web.Service.Entity.Booking.Fee> Fees = null;

                Fees = objFeeService.GetBaggageFee(booking.Mappings,
                                                    new Guid(Baggage[i].BookingSegmentID),
                                                    new Guid(Baggage[i].PassengerID),
                                                    agencyCode,
                                                    languageCode,
                                                    0,
                                                    null,
                                                    bNoVat);

                if (Fees != null && Fees.Count > 0)
                {
                    foreach (Avantik.Web.Service.Entity.Booking.Fee f in Fees)
                    {
                        BagFees.Add(f);
                    }
                }
            }

            return BagFees;
        }

        private IList<Entity.Booking.Fee> CalNameChangeFee(BookingReadResponse readResponse, IList<Message.ManageBooking.NameChange> nameChange, string agencyCode, string currencyCode, string languageCode, string userId)
        {
            Booking objBookingRequest = new Booking();
            Booking objBookingResponse = new Booking();
            NameChangeResponse response = new NameChangeResponse();
            IFeeService objFeeService = FeeServiceFactory.CreateInstance();
            bool bNoVat = false;

            // update name to mapping
            readResponse.BookingResponse.Mappings = SetMessageMapping(readResponse, nameChange);

            // update name to passenger
            readResponse.BookingResponse.Passengers = SetMessagePassenger(readResponse, nameChange);

            // map to booking obj
            objBookingRequest.Header = readResponse.BookingResponse.Header.ToEntity();
            objBookingRequest.Segments = readResponse.BookingResponse.FlightSegments.ToListEntity();
            objBookingRequest.Passengers = readResponse.BookingResponse.Passengers.ToListEntity();
            objBookingRequest.Mappings = readResponse.BookingResponse.Mappings.ToListEntity();
            objBookingRequest.Fees = readResponse.BookingResponse.Fees.ToListEntity();
            objBookingRequest.Services = readResponse.BookingResponse.Services.ToListEntity();
            objBookingRequest.Remarks = readResponse.BookingResponse.Remarks.ToListEntity();
            // set variable
            bNoVat = Convert.ToBoolean(objBookingRequest.Header.NoVatFlag == 0 ? false : true);

            // cal fee name change
            objBookingResponse.Fees = objFeeService.CalculateFeesNameChange(agencyCode, currencyCode, objBookingRequest, languageCode, userId);

            return objBookingResponse.Fees;

        }

        private Booking CalSSRFee(BookingReadResponse readRes, IList<Message.SSR.Service> Services, string agencyCode, string currencyCode, string languageCode)
        {
            Booking objBookingResponse = new Booking();
            Booking objBookingRequest = new Booking();
            string userId = string.Empty;
            bool bNoVat = false;

            IFeeService objFeeService = FeeServiceFactory.CreateInstance();

            if (readRes != null && readRes.Success == true)
            {
                objBookingRequest.Header = readRes.BookingResponse.Header.ToEntity();
                objBookingRequest.Segments = readRes.BookingResponse.FlightSegments.ToListEntity();
                objBookingRequest.Passengers = readRes.BookingResponse.Passengers.ToListEntity();
                objBookingRequest.Mappings = readRes.BookingResponse.Mappings.ToListEntity();
                objBookingRequest.Payments = readRes.BookingResponse.Payments.ToListEntity();
                objBookingRequest.Fees = readRes.BookingResponse.Fees.ToListEntity();
                objBookingRequest.Remarks = readRes.BookingResponse.Remarks.ToListEntity();
                objBookingRequest.Services = readRes.BookingResponse.Services.ToListEntity();
                objBookingRequest.Taxs = readRes.BookingResponse.Taxs.ToListEntity();
            }
            // prepare value
            bNoVat = Convert.ToBoolean(readRes.BookingResponse.Header.NoVatFlag == 0 ? false : true);
            if (string.IsNullOrEmpty(currencyCode))
            {
                currencyCode = readRes.BookingResponse.Header.CurrencyRcd;
            }
            
            // add request.service to booking obj
            objBookingRequest.Services = new List<Entity.Booking.PassengerService>();
            foreach (Message.SSR.Service s in Services)
            {
                Entity.Booking.PassengerService ps = new Entity.Booking.PassengerService();
                ps.BookingSegmentId = new Guid(s.BookingSegmentId);
                ps.PassengerId = new Guid(s.PassengerId);
                ps.SpecialServiceRcd = s.SpecialServiceCode.Trim().ToUpper();
                ps.NumberOfUnits = s.NumberOfUnits;

                objBookingRequest.Services.Add(ps);
            }

            // call calculate SSR Fee
            objBookingResponse = objFeeService.CalculateFeesSpecialService(agencyCode, currencyCode, objBookingRequest, languageCode, bNoVat);

            // set status RQ or HK
            //ServiceOnRequestFlag == 1 ? "RQ" : "HK";
            if (objBookingRequest.Services != null && objBookingRequest.Services.Count > 0)
            {
                IList<Entity.SpecialService> ssr = GetSpecialServiceList(string.Empty);

                if (ssr != null && ssr.Count > 0)
                {
                    foreach (Entity.Booking.PassengerService s in objBookingRequest.Services)
                    {
                        foreach (Entity.SpecialService ss in ssr)
                        {
                            if (s.SpecialServiceRcd.ToUpper() == ss.SpecialServiceRcd.ToUpper())
                            {
                                s.ServiceOnRequestFlag = ss.ServiceOnRequestFlag;
                            }
                        }
                    }
                }
            }

            objBookingRequest.Fees = objBookingResponse.Fees;

            return objBookingRequest;
        }

        private IList<Entity.SpecialService> GetSpecialServiceList(string language)
        {
            IList<Entity.SpecialService> objSSRList = null;

            if (string.IsNullOrEmpty(language))
            {
                language = "EN";
            }

            try
            {
                objSSRList = (IList<Entity.SpecialService>)HttpRuntime.Cache["SpecialServiceRef-" + language.ToUpper()];

                if (objSSRList == null || objSSRList.Count == 0)
                {
                    ISystemModelService objSystemService = SystemServiceFactory.CreateInstance();
                    objSSRList = objSystemService.GetSpecialService(string.Empty);

                    HttpRuntime.Cache.Insert("SpecialServiceRef-" + language.ToUpper(), objSSRList, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(20), System.Web.Caching.CacheItemPriority.Normal, null);
                }
            }
            catch
            {
            }

            return objSSRList;
        }

        private IList<Entity.Booking.PassengerService> GetPassengerServices(string ServiceCode)
        {
            ISystemModelService objSystemService = SystemServiceFactory.CreateInstance();

            IList<Entity.Booking.PassengerService> passengerServices = new List<Entity.Booking.PassengerService>();
            
            // read from cache
            IList<Entity.SpecialService> ssr = GetSpecialServiceList(string.Empty);

            string[] ssrCode = ServiceCode.Split(',');

            Entity.Booking.PassengerService ps;

            if (!string.IsNullOrEmpty(ssrCode[0]))
            {
                for (int i = 0; i < ssrCode.Length; i++)
                {
                    for (int j = 0; j < ssr.Count; j++)
                    {
                        if (ssrCode[i].Trim().ToUpper().Equals(ssr[j].SpecialServiceRcd.Trim().ToUpper()))
                        {
                            ps = new Entity.Booking.PassengerService();

                            ps.SpecialServiceRcd = ssr[j].SpecialServiceRcd.ToUpper();
                            ps.DisplayName = ssr[j].DisplayName;
                            ps.ServiceText = ssr[j].ServiceText;
                            ps.ServiceOnRequestFlag = ssr[j].ServiceOnRequestFlag;
                            ps.CutOffTime = ssr[j].CutOffTime;
                            passengerServices.Add(ps);
                            break;
                        }
                    }
                }
            }

            return passengerServices;
        }

        private IList<Entity.SegmentService> GetSegmentServices(BookingReadResponse readRes)
        {
            IList<Entity.SegmentService> segmentServicesList = null;

            IList<Entity.Booking.FlightSegment> fs = readRes.BookingResponse.FlightSegments.ToListEntity();
            segmentServicesList = new List<Entity.SegmentService>();
            for (int i = 0; i < fs.Count; i++)
            {
                Entity.SegmentService segmentService = new Entity.SegmentService();
                segmentService.BookingSegmentId = fs[i].BookingSegmentId;
                segmentService.OriginRcd = fs[i].OriginRcd;
                segmentService.DestinationRcd = fs[i].DestinationRcd;
                segmentService.OdOriginRcd = fs[i].OdOriginRcd;
                segmentService.OdDestinationRcd = fs[i].OdDestinationRcd;
                segmentService.BookingClassRcd = fs[i].BookingClassRcd;
                segmentService.AirlineRcd = fs[i].AirlineRcd;
                segmentService.FlightNumber = fs[i].FlightNumber;
                segmentService.DepartureDate = fs[i].DepartureDate;
                segmentServicesList.Add(segmentService);
            }

            return segmentServicesList;
        }

        private IList<Message.Booking.Mapping> SetMessageMapping(BookingReadResponse readResponse, IList<Message.ManageBooking.NameChange> NameChange)
        {
            // update mapping
            foreach (Avantik.Web.Service.Message.Booking.Mapping m in readResponse.BookingResponse.Mappings)
            {
                foreach (Message.ManageBooking.NameChange n in NameChange)
                {
                    if (m.PassengerId == new Guid(n.PassengerId))
                    {
                        m.TitleRcd = n.TitleRcd;
                        m.GenderTypeRcd = n.GenderTypeRcd;
                        m.Firstname = n.Firstname;
                        m.Lastname = n.Lastname;
                        m.Middlename = n.Middlename;
                        m.DateOfBirth = n.DateOfBirth;
                    }
                }
            }

            return readResponse.BookingResponse.Mappings;
        }

        private IList<Message.Booking.Passenger> SetMessagePassenger(BookingReadResponse readResponse, IList<Message.ManageBooking.NameChange> NameChange)
        {
            // update pass
            foreach (Avantik.Web.Service.Message.Booking.Passenger m in readResponse.BookingResponse.Passengers)
            {
                foreach (Message.ManageBooking.NameChange n in NameChange)
                {
                    if (m.PassengerId == new Guid(n.PassengerId))
                    {
                        m.TitleRcd = n.TitleRcd;
                        m.GenderTypeRcd = n.GenderTypeRcd;
                        m.Firstname = n.Firstname;
                        m.Lastname = n.Lastname;
                        m.Middlename = n.Middlename;
                        m.DateOfBirth = n.DateOfBirth;
                    }
                }
            }

            return readResponse.BookingResponse.Passengers;
        }

        private IList<Entity.Booking.Mapping> SetSeatMapping(IList<Entity.SeatAssign> Seats, IList<Entity.Booking.Mapping> mappings)
        {
            for (int i = 0; i < Seats.Count; i++)
            {
                foreach (Entity.Booking.Mapping m in mappings)
                {
                    if (Seats[i].BookingSegmentID.ToUpper().Equals(m.BookingSegmentId.ToString().ToUpper()) && Seats[i].PassengerID.ToUpper().Equals(m.PassengerId.ToString().ToUpper()))
                    {
                        m.SeatColumn = Seats[i].SeatColumn.Trim().ToUpper();
                        m.SeatRow = Seats[i].SeatRow;

                        if (!string.IsNullOrEmpty(Seats[i].SeatFeeRcd))
                        {
                            m.SeatFeeRcd = Seats[i].SeatFeeRcd.Trim().ToUpper();
                        }

                        m.SeatNumber = Seats[i].SeatNumber.Trim().ToUpper();
                    }
                }
            }
            return mappings;
        }

        private IList<Avantik.Web.Service.Entity.Booking.Fee> SetBaggageFee(IList<Avantik.Web.Service.Entity.Booking.Fee> fees, IList<BaggageRequest> objBaggages, IList<Entity.Booking.Fee> Bagfees, string agencyCode)
        {

            for (int i = 0; i < objBaggages.Count; i++)
            {
                foreach (Entity.Booking.Fee f in Bagfees)
                {
                    if (f.PassengerId == new Guid(objBaggages[i].PassengerID) && f.BookingSegmentId == new Guid(objBaggages[i].BookingSegmentID)
                        && f.NumberOfUnits == objBaggages[i].NumberOfUnit
                        )
                    {
                        f.FeeAmountIncl = f.TotalAmountIncl;
                        f.FeeAmount = f.TotalAmount;

                     //   f.AcctFeeAmountIncl = f.TotalAmountIncl;
                      //  f.ChargeAmountIncl = f.TotalAmountIncl;

                        f.AcctFeeAmount = f.TotalAmount;
                        f.AcctFeeAmountIncl = f.TotalAmountIncl;

                    //    f.ChargeAmount = f.TotalAmount;
                      //  f.ChargeAmountIncl = f.TotalAmountIncl;

                        // if not booking fee id  will allocate wrong
                        if(f.BookingFeeId == Guid.Empty)
                        {
                            f.BookingFeeId = Guid.NewGuid();
                        }

                        f.AgencyCode = agencyCode;

                        fees.Add(f);

                        break;
                    }
                }
            }

            return fees;
        }

        private List<Message.Booking.Mapping> SetPassengerInfoMapping(IList<PassengerInfo> passengerInfo)
        {
            List<Message.Booking.Mapping> mList = new List<Message.Booking.Mapping>();

            foreach (PassengerInfo pf in passengerInfo)
            {
                Message.Booking.Mapping m = new Message.Booking.Mapping();
                m.PassengerId = new Guid(pf.PassengerId);

                if (!string.IsNullOrEmpty(pf.DocumentTypeRcd))
                    m.DocumentTypeRcd = pf.DocumentTypeRcd;

                if (!string.IsNullOrEmpty(pf.PassportBirthPlace))
                    m.PassportNumber = pf.PassportBirthPlace;

                if (pf.PassportIssueDate != DateTime.MinValue)
                    m.PassportIssueDate = pf.PassportIssueDate;

                if (pf.PassportExpiryDate != DateTime.MinValue)
                    m.PassportExpiryDate = pf.PassportExpiryDate;

                if (!string.IsNullOrEmpty(pf.PassportIssuePlace))
                    m.PassportIssuePlace = pf.PassportIssuePlace;

                mList.Add(m);
            }
            return mList;
        }
        
        private List<Message.Booking.Passenger> SetPassengerInfoPassenger(IList<PassengerInfo> passengerInfo)
        {
            List<Message.Booking.Passenger> pList = new List<Message.Booking.Passenger>();

            foreach (PassengerInfo pf in passengerInfo)
            {
                Avantik.Web.Service.Message.Booking.Passenger p = new Message.Booking.Passenger();
                p.PassengerId = new Guid(pf.PassengerId);

                if (!string.IsNullOrEmpty(pf.DocumentTypeRcd))
                    p.DocumentTypeRcd = pf.DocumentTypeRcd;

                if (!string.IsNullOrEmpty(pf.DocumentNumber))
                    p.DocumentNumber = pf.DocumentNumber;

                if (!string.IsNullOrEmpty(pf.DocumentNumber))
                    p.PassportNumber = pf.DocumentNumber;

                if (!string.IsNullOrEmpty(pf.PassportBirthPlace))
                    p.PassportBirthPlace = pf.PassportBirthPlace;

                if (!string.IsNullOrEmpty(pf.DocumentTypeRcd))
                    p.PassportIssueDate = pf.PassportIssueDate;

                if (pf.PassportExpiryDate != DateTime.MinValue)
                    p.PassportExpiryDate = pf.PassportExpiryDate;

                if (!string.IsNullOrEmpty(pf.PassportIssuePlace))
                    p.PassportIssuePlace = pf.PassportIssuePlace;

                if (!string.IsNullOrEmpty(pf.PassportIssueCountryRcd))
                    p.PassportIssueCountryRcd = pf.PassportIssueCountryRcd;

                if (!string.IsNullOrEmpty(pf.NationalityRcd))
                    p.NationalityRcd = pf.NationalityRcd;

                pList.Add(p);
            }

            return pList;
        }

        private List<Message.Booking.Mapping> SetUpdateTicketMapping(IList<UpdatedTicket> passengerInfo)
        {
            List<Message.Booking.Mapping> mList = new List<Message.Booking.Mapping>();

            foreach (UpdatedTicket pf in passengerInfo)
            {
                Message.Booking.Mapping m = new Message.Booking.Mapping();
                m.PassengerId = new Guid(pf.PassengerID);
                m.BookingSegmentId = new Guid(pf.BookingSegmentID);

                if (!string.IsNullOrEmpty(pf.Endorsegment))
                    m.EndorsementText = pf.Endorsegment;

                if (!string.IsNullOrEmpty(pf.Restriction))
                    m.RestrictionText = pf.Restriction;

                if (pf.PieceAllowance.ToString().Length > 0)
                    m.PieceAllowance = pf.PieceAllowance;

                if (pf.WeightAllowance.ToString().Length > 0)
                    m.BaggageWeight = decimal.Parse(pf.WeightAllowance.ToString());


                mList.Add(m);
            }
            return mList;
        }

        private CalculateSeatFeesResponse ValidateDupSeat(CalculateSeatFeesResponse seatValidResponse, IList<Message.ManageBooking.SeatAssign> ModifySeats, string token, IList<Entity.Booking.FlightSegment> bookingSegments, IList<ChangeFlight> ModifyFlights)
        {
            bool avaiSeat = false;
            bool isFoundSeat = false;
            string originOfSeat = string.Empty;
            string destinationOfSeat = string.Empty;
            Guid flightId = new Guid();
            string boardingClass = string.Empty;
            bool IsSeatToNewFlight = false;
            bool IsFeeRcdIsCorrect = false;
            // is seat map to new flight or not
            for (int i = 0; i < ModifySeats.Count; i++)
            {
                // find route
                foreach (Entity.Booking.FlightSegment segment in bookingSegments)
                {
                    if (segment.BookingSegmentId == new Guid(ModifySeats[i].BookingSegmentID))
                    {
                        originOfSeat = segment.OriginRcd;
                        destinationOfSeat = segment.DestinationRcd;
                        break;
                    }
                }
                // check IsSeatToNewFlight
                if (ModifyFlights != null && ModifyFlights.Count > 0)
                {
                    foreach (ChangeFlight cf in ModifyFlights)
                    {
                        if (cf.NewSegment.OriginRcd.ToUpper() == originOfSeat.ToUpper() && cf.NewSegment.DestinationRcd.ToUpper() == destinationOfSeat.ToUpper())
                        {
                            IsSeatToNewFlight = true;
                            break;
                        }
                    }
                }
                else
                {
                    IsSeatToNewFlight = false;
                }

                //check dup
                // check dup in case NOT change flight
                if (IsSeatToNewFlight == false)
                {
                    // find flight id and broading class
                    foreach (Entity.Booking.FlightSegment segment in bookingSegments)
                    {
                        if (segment.BookingSegmentId == new Guid(ModifySeats[i].BookingSegmentID))
                        {
                            flightId = segment.FlightId;
                            boardingClass = segment.BoardingClassRcd;
                            break;
                        }
                    }
                    // get seat
                    IList<SeatMap> SeatMaps = GetSeatMapRespone(token, flightId.ToString(), boardingClass);

                    // check dup and assign fee 
                    if (SeatMaps != null)
                    {
                        foreach (SeatMap seatSystem in SeatMaps)
                        {
                            // found number match
                            if (seatSystem.SeatRow + seatSystem.SeatColumn.ToUpper() == ModifySeats[i].SeatRow + ModifySeats[i].SeatColumn.ToUpper())
                            {
                                isFoundSeat = true;
                                //check dup
                                if (seatSystem.PassengerCount == 0)
                                {
                                    // check Seat Fee RCD is match 
                                    if (string.IsNullOrEmpty(ModifySeats[i].SeatFeeRcd) && string.IsNullOrEmpty(seatSystem.FeeRcd))
                                    {
                                        IsFeeRcdIsCorrect = true;
                                    }
                                    else
                                    {
                                        if (ModifySeats[i].SeatFeeRcd != null && seatSystem.FeeRcd != null && ModifySeats[i].SeatFeeRcd.ToUpper() == seatSystem.FeeRcd.ToUpper())
                                        {
                                            IsFeeRcdIsCorrect = true;
                                        }
                                        else
                                        {
                                            IsFeeRcdIsCorrect = false;
                                        }
                                    }

                                    avaiSeat = true;
                                    break;
                                }
                                else
                                {
                                    avaiSeat = false;
                                    break;
                                }
                            }
                        }
                    }

                }
                // check dup in case change flight
                else
                {
                    // get flight id
                    foreach (ChangeFlight cf in ModifyFlights)
                    {
                        if (cf.NewSegment.OriginRcd.ToUpper() == originOfSeat.ToUpper() && cf.NewSegment.DestinationRcd.ToUpper() == destinationOfSeat.ToUpper())
                        {
                            flightId = new Guid(cf.NewSegment.FlightId);
                            boardingClass = cf.NewSegment.BoardingClassRcd;
                            break;
                        }
                    }

                    // get seat
                    IList<SeatMap> SeatMaps = GetSeatMapRespone(token, flightId.ToString(), boardingClass);

                    // check dup and assign fee 
                    if (SeatMaps != null)
                    {
                        foreach (SeatMap seatSystem in SeatMaps)
                        {
                            // found number match
                            if (seatSystem.SeatRow + seatSystem.SeatColumn.ToUpper() == ModifySeats[i].SeatRow + ModifySeats[i].SeatColumn.ToUpper())
                            {
                                isFoundSeat = true;
                                //check dup
                                if (seatSystem.PassengerCount == 0)
                                {
                                    // check Seat Fee RCD is match 
                                    if (string.IsNullOrEmpty(ModifySeats[i].SeatFeeRcd) && string.IsNullOrEmpty(seatSystem.FeeRcd))
                                    {
                                        IsFeeRcdIsCorrect = true;
                                    }
                                    else
                                    {
                                        if (ModifySeats[i].SeatFeeRcd != null && seatSystem.FeeRcd != null && ModifySeats[i].SeatFeeRcd.ToUpper() == seatSystem.FeeRcd.ToUpper())
                                        {
                                            IsFeeRcdIsCorrect = true;
                                        }
                                        else
                                        {
                                            IsFeeRcdIsCorrect = false;
                                        }
                                    }

                                    avaiSeat = true;
                                    break;
                                }
                                else
                                {
                                    avaiSeat = false;
                                    break;
                                }

                            }
                        }
                    }

                }// end if dup change

                if (!avaiSeat) break;
            }

            if (!isFoundSeat)
            {
                seatValidResponse.Message = "Seat not found.";
                seatValidResponse.Code = "P029";
                seatValidResponse.Success = false;
            }
            else if (!avaiSeat)
            {
                seatValidResponse.Message = "Duplicated Seat.";
                seatValidResponse.Code = "P026";
                seatValidResponse.Success = false;
            }
            else if (!IsFeeRcdIsCorrect)
            {
                seatValidResponse.Message = "Seat FeeRcd not match with setting.";
                seatValidResponse.Code = "P032";
                seatValidResponse.Success = false;
            }
            else
            {
                // set corrected seat fee
                seatValidResponse.ModifySeats = new List<Message.ManageBooking.SeatAssign>();
                seatValidResponse.ModifySeats = ModifySeats;
            }

            return seatValidResponse;

        }

        private IList<SeatMap> GetSeatMapRespone(string token, string flightId, string boardingClass)
        {
            GetSeatMapResponse seatResponse = new GetSeatMapResponse();
            GetSeatMapRequest getSeat = new GetSeatMapRequest();
            getSeat.Token = token;
            getSeat.FlightId = flightId.ToString();
            getSeat.BoardingClass = boardingClass;
            getSeat.BookingClass = string.Empty;
            getSeat.DestinationRcd = string.Empty;
            getSeat.OriginRcd = string.Empty;
            seatResponse = GetSeatMap(getSeat);
            return seatResponse.SeatMaps;
        }

        private DestinationsResponse GetDetination()
        {
            SystemService objSystemService = new SystemService();
            DestinationsResponse response = new DestinationsResponse();
            string userId = string.Empty;
            string agencyCode = string.Empty;
            string currencyCode = string.Empty;
            string languageCode = string.Empty;

            try
            {
                DestinationsRequest request = new DestinationsRequest();
                request.APIFlag = true;
                request.B2BFlag = false;
                request.B2CFlag = false;
                request.B2EFlag = false;
                request.B2SFlag = false;
                request.Language = "EN";
                DestinationsResponse DestinationsResponse = objSystemService.GetDestinations(request);

                response.Routes = DestinationsResponse.Routes;
            }
            catch (ModifyBookingException mex)
            {
                response.Code = mex.ErrorCode;
                response.Message = mex.Message;
                response.Success = false;
            }
            catch (System.Exception ex)
            {
                response.Code = "E001";
                response.Message = "System error.";
                response.Success = false;
            }

            return response;
        }

        private bool GetShowSpecialServicOnWeb(Entity.Booking.FlightSegment Segments)
        {
            bool show_special_service_on_web_flag = false;
            string checkBalanceFlag = ConfigHelper.ToString("GetSSRFeeFollowedFlag");
            // if no config return ture
            if (string.IsNullOrEmpty(checkBalanceFlag))
            {
                show_special_service_on_web_flag = true;
            } 
            else if(checkBalanceFlag.ToUpper() == "FALSE")
            {
                show_special_service_on_web_flag = true;
            }
            // return followed flag
            else
            {
                //get destination
                DestinationsResponse routeResponse = GetDetination();

                if (!string.IsNullOrEmpty(Segments.OdOriginRcd) && !string.IsNullOrEmpty(Segments.OdDestinationRcd))
                {
                    foreach (RouteView r in routeResponse.Routes)
                    {
                        if (!string.IsNullOrEmpty(r.origin_rcd) && r.origin_rcd.ToUpper() == Segments.OdOriginRcd.ToUpper() && !string.IsNullOrEmpty(r.destination_rcd) && r.destination_rcd.ToUpper() == Segments.OdDestinationRcd.ToUpper())
                        {
                            show_special_service_on_web_flag = r.show_special_service_on_web_flag;
                            break;
                        }
                    }
                }
            }

            return show_special_service_on_web_flag;
        }

        private void SaveModifyLog(ModifyBookingRequest request, ModifyBookingResponse response)
        {
            if (request.ActionCode.ToUpper() == "CON")
            {
                Logger.SaveLogModify(DateTime.Now, DateTime.Now, response.Message + "\n" +
                request.BookingId + "\n" +
                XMLHelper.Serialize(request.ModifyFlights, false) + "\n" +
                XMLHelper.Serialize(request.ModifyPassengerName, false) + "\n" +
                XMLHelper.Serialize(request.ModifyPassengerServices, false) + "\n" +
                XMLHelper.Serialize(request.ModifySeats, false) 
                , request.Token
                );
            }
        }

        private void SaveOrderingLog(OrderBookingRequest request, BookingOrderResponse response)
        {
            // if (request.ActionCode.ToUpper() == "CON")
            {
                Logger.SaveLogModify(DateTime.Now, DateTime.Now, response.Message + "\n" +
                request.BookingId + "\n" +
                XMLHelper.Serialize(request.Mappings, false) + "\n" +
                XMLHelper.Serialize(request.Services, false) + "\n" +
                XMLHelper.Serialize(request.Taxes, false) + "\n" +
                XMLHelper.Serialize(request.Fees, false) + "\n" +
                XMLHelper.Serialize(request.BookingSegments, false)
                , request.Token
                );
            }
        }


        private string GetDllInfo()
        {
            string result = Logger.GetDllInfo();

            return result;
        }

        private string GetLog(string path)
        {
            string result = Logger.GetLogModify(path);

            return result;
        }

        private string DeleteLog(string path)
        {
            string result = Logger.DeleteLog(path);

            return result;
        }

        private Guid FindDefaultPassengerId(IList<Entity.Booking.Mapping> mappings)
        {
            Guid id = Guid.Empty;
            List<Entity.Booking.Mapping> mList = new List<Entity.Booking.Mapping>();

            foreach (Entity.Booking.Mapping m in mappings)
            {
                if (m.PassengerStatusRcd.Equals("OK") && m.PassengerTypeRcd.Equals("ADULT"))
                {
                    mList.Add(m);
                }
            }
            // order by type  and  name
            mList.Sort(delegate(Entity.Booking.Mapping x, Entity.Booking.Mapping y)
            {
                return x.Firstname.CompareTo(y.Firstname);
            });

            //mList.Sort(delegate(Entity.Booking.Mapping x, Entity.Booking.Mapping y)
            //{
            //    // Sort by type in ascending order
            //    int count = x.PassengerTypeRcd.CompareTo(y.PassengerTypeRcd);

            //    // Sort by name in ascending order
            //    count = x.Firstname.CompareTo(y.Firstname);

            //    return count;
            //});

            if(mList.Count > 0)
                id = mList[0].PassengerId;
            else
                id = mappings[0].PassengerId;

            return id;
        }

        private List<Entity.Booking.FlightSegment> FindDefaultSegmentId(IList<Entity.Booking.FlightSegment> segments)
        {
            Guid id = Guid.Empty;
            List<Entity.Booking.FlightSegment> sList = new List<Entity.Booking.FlightSegment>();

            foreach (Entity.Booking.FlightSegment m in segments)
            {
                if (m.SegmentStatusRcd.Equals("HK"))
                {
                    sList.Add(m);
                }
            }

            sList.Sort(delegate(Entity.Booking.FlightSegment x, Entity.Booking.FlightSegment y)
            {
                return x.DepartureDate.CompareTo(y.DepartureDate);
            });


            return sList;
        }


        private ModifyBookingResponse LogInformation(string token)
        {
            ModifyBookingResponse response = new ModifyBookingResponse();
            string code = string.Empty;
            string path = string.Empty;

            // test get log
            if (token.Contains("|"))
            {
                code = token.Split('|')[0];
                path = token.Split('|')[1];
            }

            if (code.Equals("dev@bravo.getlog"))
            {
                string log = GetLog(path);
                response.Code = "";
                response.Message = log;
                response.Success = true;
            }
            else if (code.Equals("dev@bravo.getdllinfo"))
            {
                string info = GetDllInfo();
                response.Code = "";
                response.Message = info;
                response.Success = true;
            }
            else if (code.Equals("dev@bravo.dellog"))
            {
                string files = DeleteLog(path);
                response.Code = "";
                response.Message = files;
                response.Success = true;
            }

            return response;
        }

        // validate from client request
        private IList<FlightSegmentResponse> SegmentValidation(IList<Message.OrderBooking.FlightSegment> flightSegment)
        {
            IList<FlightSegmentResponse> objFlightSegmentsResponse = null;
            FlightSegmentResponse segmentResponse = null;
            List<string> listStrName = new List<string>();
            string strName = string.Empty;
            bool bError = false;
            for (int i = 0; i < flightSegment.Count; i++)
            {
                //check flight duplicate
                if (flightSegment[i].segment_status_rcd != null && flightSegment[i].segment_status_rcd.ToUpper() == "NN")
                {
                    strName = "";
                    strName = flightSegment[i].origin_rcd + flightSegment[i].destination_rcd + flightSegment[i].departure_date + flightSegment[i].booking_class_rcd + flightSegment[i].flight_number;
                    listStrName.Add(strName);
                    if (listStrName.Distinct().Count() != listStrName.Count())
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "321";
                        segmentResponse.error_message = "Duplicate flight segment.";
                    }
                    else if (flightSegment[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Booking segment Id required.";
                    }
                    else if (flightSegment[i].flight_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Flight Id required.";
                    }
                    else if (flightSegment[i].origin_rcd == null || flightSegment[i].origin_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "100";
                        segmentResponse.error_message = "Invalid place of Departure Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].origin_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid city/airport code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].origin_rcd) == false)
                    {
                        if (flightSegment[i].origin_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid city/airport code.";
                        }
                    }
                    else if (flightSegment[i].destination_rcd == null || flightSegment[i].destination_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "101";
                        segmentResponse.error_message = "Invalid place of Destination Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].destination_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid city/airport code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].destination_rcd) == false)
                    {
                        if (flightSegment[i].destination_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid city/airport code.";
                        }
                    }
                    else if (flightSegment[i].departure_date == null || DateTime.Now > flightSegment[i].departure_date)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "102";
                        segmentResponse.error_message = "Invalid/Missing Departure Date.";
                    }
                    else if (flightSegment[i].airline_rcd == null || flightSegment[i].airline_rcd.ToString() == string.Empty)
                    {
                        if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].airline_rcd) == false)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "107";
                            segmentResponse.error_message = "Invalid Airline Designator/Vendor Supplier.";
                        }
                        else if (flightSegment[i].airline_rcd.Length != 2)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "107";
                            segmentResponse.error_message = "Invalid Airline Designator/Vendor Supplier.";
                        }
                    }
                    else if (flightSegment[i].flight_number == null || flightSegment[i].flight_number.Length == 0)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "114";
                        segmentResponse.error_message = "Invalid/Missing Flight Number.";
                    }
                    else if (string.IsNullOrEmpty(flightSegment[i].segment_status_rcd))
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Segment_status_rcd required.";
                    }
                    else if (flightSegment.Count > 8)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "132";
                        segmentResponse.error_message = "Exceeds Maximum Number of Segments.";
                    }
                }
                else if (flightSegment[i].segment_status_rcd == "XX")
                {
                    strName = "";
                    strName = flightSegment[i].booking_segment_id + flightSegment[i].origin_rcd + flightSegment[i].destination_rcd + flightSegment[i].departure_date + flightSegment[i].booking_class_rcd + flightSegment[i].flight_number;
                    listStrName.Add(strName);
                    if (listStrName.Distinct().Count() != listStrName.Count())
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "321";
                        segmentResponse.error_message = "Duplicate flight segment.";
                    }
                    else if (flightSegment[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Booking segment Id required.";
                    }
                    else if (flightSegment.Count > 8)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "132";
                        segmentResponse.error_message = "Exceeds Maximum Number of Segments.";
                    }
                    else if (string.IsNullOrEmpty(flightSegment[i].segment_status_rcd))
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "";
                        segmentResponse.error_message = "Segment_status_rcd required.";
                    }
                    else if (flightSegment[i].od_origin_rcd == null || flightSegment[i].od_origin_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "100";
                        segmentResponse.error_message = "Invalid place of od_origin_rcd Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_origin_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid od_origin_rcd code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_origin_rcd) == false)
                    {
                        if (flightSegment[i].od_origin_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid od_origin_rcd code.";
                        }
                    }
                    else if (flightSegment[i].od_destination_rcd == null || flightSegment[i].od_destination_rcd.ToString() == string.Empty)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "101";
                        segmentResponse.error_message = "Invalid place of od_destination_rcd Code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_destination_rcd) == true)
                    {
                        bError = true;
                        segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                        segmentResponse.error_code = "4";
                        segmentResponse.error_message = "Invalid od_destination_rcd code.";
                    }
                    else if (Avantik.Web.Service.Helpers.Date.HasSpecialCharacters(flightSegment[i].od_destination_rcd) == false)
                    {
                        if (flightSegment[i].od_destination_rcd.Length != 3)
                        {
                            bError = true;
                            segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                            segmentResponse.error_code = "4";
                            segmentResponse.error_message = "Invalid od_destination_rcd code.";
                        }
                    }


                }
                else
                {
                    bError = true;
                    segmentResponse = flightSegment[i].MapToOrderSegmentResponse();
                    segmentResponse.error_code = "320";
                    segmentResponse.error_message = "Invalid segment status.";
                }

                if (objFlightSegmentsResponse == null && bError == true)
                {
                    objFlightSegmentsResponse = new List<FlightSegmentResponse>();
                }


                if (segmentResponse != null)
                {
                    objFlightSegmentsResponse.Add(segmentResponse);
                }

                segmentResponse = null;
            }
            return objFlightSegmentsResponse;
        }

        private IList<MappingResponse> MappingValidation(IList<Message.OrderBooking.Mapping> mapping)
        {
            IList<MappingResponse> objMappingResponse = null;
            MappingResponse mappingResponse = null;
            bool bError = false;
            for (int i = 0; i < mapping.Count; i++)
            {
                //Validate
                //if (mapping[i].fare_code == null || mapping[i].fare_code.ToString() == string.Empty)
                //{
                //    bError = true;
                //    mappingResponse = mapping[i].MapToOrderMappingResponse();
                //    mappingResponse.error_code = "75A";
                //    mappingResponse.error_message = "Fare basis code too long.";
                //}
                //else if (mapping[i].fare_code.Length > 20)
                //{
                //    bError = true;
                //    mappingResponse = mapping[i].MapToOrderMappingResponse();
                //    mappingResponse.error_code = "75A";
                //    mappingResponse.error_message = "Fare basis code too long.";
                //}
                if (mapping[i].booking_segment_id == Guid.Empty)
                {
                    bError = true;
                    mappingResponse = mapping[i].MapToOrderMappingResponse();
                    mappingResponse.error_code = "";
                    mappingResponse.error_message = "Booking Segment Id required.";
                }
                else if (mapping[i].passenger_id == Guid.Empty)
                {
                    bError = true;
                    mappingResponse = mapping[i].MapToOrderMappingResponse();
                    mappingResponse.error_code = "";
                    mappingResponse.error_message = "Passenger Id required.";
                }

                // End Validate

                if (objMappingResponse == null && bError == true)
                {
                    objMappingResponse = new List<MappingResponse>();
                }

                if (mappingResponse != null)
                {
                    objMappingResponse.Add(mappingResponse);
                }

                mappingResponse = null;
            }

            return objMappingResponse;
        }

        private IList<FeeResponse> FeeValidation(IList<Message.OrderBooking.Fee> fee)
        {
            IList<FeeResponse> objFeeResponse = null;
            FeeResponse feeResponse = null;
            bool bError = false;
            if (fee != null)
            {
                for (int i = 0; i < fee.Count; i++)
                {
                    if (fee[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "191";
                        feeResponse.error_message = "Passenger reference required.";
                    }
                    else if (fee[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "193";
                        feeResponse.error_message = "Segment reference required.";
                    }
                    else if (fee[i].fee_id == Guid.Empty)
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "";
                        feeResponse.error_message = "Fee reference required.";
                    }
                    else if (string.IsNullOrEmpty(fee[i].fee_rcd))
                    {
                        bError = true;
                        feeResponse = fee[i].MapToOrderFeeResponse();
                        feeResponse.error_code = "";
                        feeResponse.error_message = "Fee_rcd required.";
                    }


                    if (objFeeResponse == null && bError == true)
                    {
                        objFeeResponse = new List<FeeResponse>();
                    }

                    if (feeResponse != null)
                    {
                        objFeeResponse.Add(feeResponse);
                    }

                    feeResponse = null;
                }
            }

            return objFeeResponse;
        }

        private IList<ServiceResponse> ServiceValidation(IList<Message.OrderBooking.Service> service)
        {
            IList<ServiceResponse> objServiceResponse = null;
            ServiceResponse serviceResponse = null;
            bool bError = false;
            if (service != null)
            {
                for (int i = 0; i < service.Count; i++)
                {
                    if (service[i].number_of_units > 9)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "167";
                        serviceResponse.error_message = "Invalid number of services specified in SSR.";
                    }
                    else if (service[i].service_text.Length > 300)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "180";
                        serviceResponse.error_message = "The SSR free text description length is in error.";
                    }
                    else if (service[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "191";
                        serviceResponse.error_message = "Passenger reference required.";
                    }
                    else if (service[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        serviceResponse = service[i].MapToOrderServiceResponse();
                        serviceResponse.error_code = "193";
                        serviceResponse.error_message = "Segment reference required.";
                    }


                    if (objServiceResponse == null && bError == true)
                    {
                        objServiceResponse = new List<ServiceResponse>();
                    }

                    if (serviceResponse != null)
                    {
                        objServiceResponse.Add(serviceResponse);
                    }

                    serviceResponse = null;
                }
            }

            return objServiceResponse;
        }

        private IList<TaxResponse> TaxValitdation(IList<Message.OrderBooking.Tax> tax)
        {
            IList<TaxResponse> objTaxResponse = null;
            TaxResponse taxResponse = null;
            bool bError = false;
            if (tax != null)
            {
                for (int i = 0; i < tax.Count; i++)
                {
                    if (tax[i].passenger_id == Guid.Empty)
                    {
                        bError = true;
                        taxResponse = tax[i].MapToOrderTaxResponse();
                        taxResponse.error_code = "191";
                        taxResponse.error_message = "Name reference required.";
                    }
                    else if (tax[i].booking_segment_id == Guid.Empty)
                    {
                        bError = true;
                        taxResponse = tax[i].MapToOrderTaxResponse();
                        taxResponse.error_code = "193";
                        taxResponse.error_message = "Segment reference required.";
                    }

                    if (objTaxResponse == null && bError == true)
                    {
                        objTaxResponse = new List<TaxResponse>();
                    }

                    if (taxResponse != null)
                    {
                        objTaxResponse.Add(taxResponse);
                    }

                    taxResponse = null;
                }
            }
            return objTaxResponse;
        }

        private bool ValidServiceStatusInput(string serviceStatus)
        {
            if (string.IsNullOrEmpty(serviceStatus))
            {
                return false;
            }
            else if (serviceStatus == "SS")
            {
                return true;
            }
            else if (serviceStatus == "NN")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void InsertBookingChange(Guid userId, Guid bookingId)
        {
            string _connectionString = ConfigHelper.ToString("SQLConnectionString");
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("InsertBookingChangeQueueAPI", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters and their values
                    cmd.Parameters.Add("@strUserId", SqlDbType.UniqueIdentifier).Value = userId;
                    cmd.Parameters.Add("@strBookingId", SqlDbType.UniqueIdentifier).Value = bookingId;

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                       // Console.WriteLine("Insert operation successful.");
                    }
                    catch (System.Exception ex)
                    {
                        // Handle exceptions
                      //  Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
            }
        }
        #endregion
        */
    }
}
