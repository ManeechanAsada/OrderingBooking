using Avantik.Web.Service.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avantik.Web.Service.Entity;
using Avantik.Web.Service.COMHelper;
using System.Runtime.InteropServices;

using Avantik.Web.Service.Exception.Booking;
using Avantik.Web.Service.Entity.FormOfPayment;
using Avantik.Web.Service.Entity.Booking;

namespace Avantik.Web.Service.Model.COM
{
    public class SystemService : RunComplus, ISystemModelService
    {
        string _server = string.Empty;
        public SystemService(string server, string user, string pass, string domain)
            :base(user,pass,domain)
        {
            _server = server;
        }

        public IList<Country> GetCountry(string language)
        {
          
            List<Country> countries = new List<Country>();

        

            return countries;

        }

        public IList<Language> GetLanguage(string language)
        {
           
            List<Language> languages = new List<Language>();

         
            return languages;

        }

        public IList<Title> GetTitle(string language)
        {
           
            List<Title> titles = new List<Title>();

       

            return titles;

        }

        public IList<Currency> GetCurrency(string language)
        {
           
            List<Currency> currencies = new List<Currency>();
          
            return currencies;
        }

        public IList<SpecialService> GetSpecialService(string language)
        {

            return null;
        }

        public FormOfPayment GetFormOfPaymentSubType(string type, string language)
        {


            return null;
        }

        public List<Document> GetDocumentType(string language)
        {


            return null;

        }
    }
}
