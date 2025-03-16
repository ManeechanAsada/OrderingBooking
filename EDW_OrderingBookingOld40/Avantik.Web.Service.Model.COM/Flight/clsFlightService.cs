using Avantik.Web.Service.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.COMHelper;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Entity;
using Avantik.Web.Service.Entity.Flight;
using Avantik.Web.Service.Entity.Booking;

namespace Avantik.Web.Service.Model.COM
{
    public class FlightService : RunComplus, IFlightService
    {
        string _server = string.Empty;
        public FlightService(string server, string user, string pass, string domain)
            :base(user,pass,domain)
        {
            _server = server;
        }
        public List<SeatMap> GetSeatMap(Entity.Booking.Flight flight)
        {          
          

            return null;
        }
    }
}
