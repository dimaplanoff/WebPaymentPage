using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayPage
{
    public class Config
    {
        public static int Timeout { get; set; }
        public static string DbPrefixSb { get; set; }
        public static string DbPrefixRc { get; set; }
        public static object sync = new object();
        public static string ReturnUrl { get; set; }
        public static readonly string vd_ServerMessagePay = Guid.NewGuid().ToString();
        public static readonly string vd_ServerMessageStatus = Guid.NewGuid().ToString();
        public static readonly string vd_TmpStatus = Guid.NewGuid().ToString();
        public static readonly string vd_TmpValue = Guid.NewGuid().ToString();        
        public static readonly string vd_Redirect = Guid.NewGuid().ToString();
        public static readonly string ss_OrderInfo = Guid.NewGuid().ToString();
        public static readonly string ss_SberValues_getOrderStatusExtended_toSber = Guid.NewGuid().ToString();
        public static readonly string ss_SberValues_getOrderStatusExtended_fromSber = Guid.NewGuid().ToString();
        public static readonly string ss_SberValues_register_toSber = Guid.NewGuid().ToString();
        public static readonly string ss_SberValues_register_fromSber = Guid.NewGuid().ToString();


        public static char RndChar()
        {
            var chars = "QWERTYUIOPLKJHGFDSAZXCVBNM".ToCharArray();
            return chars[new Random().Next(0, chars.Length)];
        }

        public static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static void SetSessionValue(HttpContext http, object value)
        {
            SetSessionValue(http, value.GetType().Name, value);
        }

        public static void SetSessionValue(HttpContext http, string name, object value)
        {
            http.Session.Set(name, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, jsonSerializerSettings)));
        }

        public static dynamic GetSessionValue<T>(HttpContext http, string name)
        {

            Type type = typeof(T);
            if (http.Session.TryGetValue(name, out byte[] outbyte))
            {
                return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(outbyte), type, jsonSerializerSettings);
            }

            return Activator.CreateInstance(type);
        }
    }
}
