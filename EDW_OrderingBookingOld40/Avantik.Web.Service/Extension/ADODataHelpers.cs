using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Avantik.Web.Service.Extension
{
    public static class ADODataHelpers
    {
        public static string DBToString(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return row[columnName].ToString();
            }
            return string.Empty;
        }

        public static int DBToInt32(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Convert.ToInt32(row[columnName]);
            }
            return 0;
        }

        public static byte DBToByte(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Convert.ToByte(row[columnName]);
            }
            return 0;
        }

        public static decimal DBToDecimal(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Convert.ToDecimal(row[columnName]);
            }
            return 0m;
        }

        public static Guid DBToGuid(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Guid.Parse(row[columnName].ToString());
            }
            return Guid.Empty;
        }

        public static DateTime DBToDateTime(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Convert.ToDateTime(row[columnName]);
            }
            return DateTime.MinValue;
        }

        public static bool DBToBool(DataRow row, string columnName)
        {
            if (row[columnName] != DBNull.Value)
            {
                return Convert.ToBoolean(row[columnName]);
            }
            return false;
        }
    }

}
