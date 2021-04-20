using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PayPage
{
    public class LNet
    {
        public static readonly string apiLogin = "xxxxyyyyzzzzz-api";
        public static readonly string apiPass = "passwd";


        public static readonly string addr_register = "register.do";
        public static readonly string addr_getOrderStatusExtended = "getOrderStatusExtended.do";
        public static readonly Uri baseAddress = new Uri("https://site.ru/payment/rest/");
        public static int webTimeout = 3000;

        public static T reqResp<T>(HttpContext http, Sb.ApplicationContext sb_context, object _obj_req, string action, HttpMethod meth = null)
        {
            var log_requst = new Sb.PaymentRequestHistory() {
                date = DateTime.Now,
                isResponse = false,
                url = baseAddress + action,
                uid = http.Session.Id,
                body = JsonConvert.SerializeObject(_obj_req, Config.jsonSerializerSettings)
            };

            sb_context.paymentRequestHistories.Add(log_requst);
            sb_context.SaveChanges();


            if (_obj_req.GetType() == typeof(SberClasses.Register_toSber))
            {
                Sb.OrderInfo orderInfo = Config.GetSessionValue<Sb.OrderInfo>(http, Config.ss_OrderInfo);
                orderInfo.response_id = log_requst.id;
                  
                sb_context.orderInfo.Add(orderInfo);
                sb_context.SaveChanges();
            }


            var obj_resp = typeof(T);
            var obj_req = _obj_req.GetType();

            object a = Activator.CreateInstance(obj_resp);

            try
            {

                var form = new Dictionary<string, string>();
                var query = action + "?";
                var json = "{";
                foreach (var p in obj_req.GetProperties())
                {
                    var v = obj_req.GetProperty(p.Name).GetValue(_obj_req);
                    if (v != null)
                    {
                        //var val = System.Web.HttpUtility.UrlEncode(v.ToString().Replace("\"", "'"));
                        var val = v.ToString();

                        json += "\"" + p.Name + "\":\"" + val + "\",";
                        form.Add(p.Name, val);
                        query += p.Name + "=" + val + "&";
                    }
                }
                json = (json.Length > 1 ? json.Substring(0, json.Length - 1) : json) + "}";
                query = query.Substring(0, query.Length - 1);

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = baseAddress;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.79 Safari/537.36");
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                    HttpRequestMessage httpRequest = null;

                    if (meth != null && meth.Equals(HttpMethod.Get))
                    {
                        httpRequest = new HttpRequestMessage(HttpMethod.Get, query);
                    }
                    else
                    {
                        httpRequest = new HttpRequestMessage(HttpMethod.Post, action);
                        //httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");//"application/x-www-form-urlencoded"//"text/plain"//"application/json"                        
                        httpRequest.Content = new FormUrlEncodedContent(form);
                        httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded");
                    }

                    Task.Run(async () =>
                    {
                        var response = await client.SendAsync(httpRequest);

                        //Console.WriteLine("url=>    " + response.RequestMessage.RequestUri);
                        //foreach (var h in httpRequest.Content.Headers) Console.WriteLine("header=> " + h.Key + ": " + h.Value.FirstOrDefault());
                        //Console.WriteLine("body=>   " + await httpRequest.Content.ReadAsStringAsync());

                        var res = await response.Content.ReadAsStringAsync();
                        a = JsonConvert.DeserializeObject(res, obj_resp, Config.jsonSerializerSettings);
                    }).Wait();

                }

            }
            catch (Exception err)
            {
                Console.WriteLine("error=>  " + err.Message);
            }
            finally
            {

                var log_response = new Sb.PaymentRequestHistory()
                {
                    date = DateTime.Now,
                    isResponse = true,
                    url = baseAddress + action,
                    uid = http.Session.Id,
                    body = JsonConvert.SerializeObject(a, Config.jsonSerializerSettings)
                };

                sb_context.paymentRequestHistories.Add(log_response);
                sb_context.SaveChanges();


                GC.Collect();
            }        

            return (T)a;
        }

    }

        /// /////////////////////////////////////////////////////////////////////////////////////////////

        public class SberClasses { 

        public class GetOrderStatusExtended_toSber
        {
            public string userName { get; set; }
            public string password { get; set; }
            public string token { get; set; }
            public string orderId { get; set; }
            public string orderNumber { get; set; }
            public string language { get; set; }
        }

        public class GetOrderStatusExtended_fromSber
        {
            public string orderNumber { get; set; }
            public int? orderStatus { get; set; }
            public int? actionCode { get; set; }
            public string actionCodeDescription { get; set; }
            public int? errorCode { get; set; }
            public string errorMessage { get; set; }
            public int? amount { get; set; }
            public int? currency { get; set; }
            public string date { get; set; }
            public string orderDescription { get; set; }
            public string ip { get; set; }
            public string authRefNum { get; set; }
            public string refundedDate { get; set; }
            public string paymentWay { get; set; }
            private string ___terminalId { get; set; }
            public string terminalId { get { return string.IsNullOrEmpty(___terminalId) ? bindingInfo.terminalId : ___terminalId; } set { ___terminalId = value; if (bindingInfo != null && string.IsNullOrEmpty(bindingInfo.terminalId) && !string.IsNullOrEmpty(___terminalId)) bindingInfo.terminalId = ___terminalId; } }
            private GetOrderStatusExtended_fromSber_merchantOrderParams[] ___merchantOrderParams;
            public GetOrderStatusExtended_fromSber_merchantOrderParams[] merchantOrderParams { get { return ___merchantOrderParams; } set { ___merchantOrderParams = value; } }
            public GetOrderStatusExtended_fromSber_merchantOrderParams[] transactionAttributes { get { return ___merchantOrderParams; } set { ___merchantOrderParams = value; } }
            public GetOrderStatusExtended_fromSber_merchantOrderParams[] attributes { get { return ___merchantOrderParams; } set { ___merchantOrderParams = value; } }
            public GetOrderStatusExtended_fromSber_cardAuthInfo cardAuthInfo { get; set; }
            public GetOrderStatusExtended_fromSber_bindingInfo bindingInfo { get; set; }
            public GetOrderStatusExtended_fromSber_paymentAmountInfo paymentAmountInfo { get; set; }
            public GetOrderStatusExtended_fromSber_bankInfo bankInfo { get; set; }
            public GetOrderStatusExtended_fromSber_payerData payerData { get; set; }

        }



        public class GetOrderStatusExtended_fromSber_merchantOrderParams
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class GetOrderStatusExtended_fromSber_cardAuthInfo
        {
            public string maskedPan { get; set; }
            public int? expiration { get; set; }
            public string cardholderName { get; set; }
            public string approvalCode { get; set; }
            public string chargeback { get; set; }
            public string paymentSystem { get; set; }
            public string product { get; set; }
            public GetOrderStatusExtended_fromSber_secureAuthInfo[] secureAuthInfo { get; set; }
        }

        public class GetOrderStatusExtended_fromSber_secureAuthInfo
        {
            public int? eci { get; set; }
            public string cavv { get; set; }
            public string xid { get; set; }
        }

        public class GetOrderStatusExtended_fromSber_bindingInfo
        {
            public string clientId { get; set; }
            public string bindingId { get; set; }
            public string authDateTime { get; set; }
            public string terminalId { get; set; }
        }


        public class GetOrderStatusExtended_fromSber_paymentAmountInfo
        {
            public int? approvedAmount { get; set; }
            public int? depositedAmount { get; set; }
            public int? refundedAmount { get; set; }
            public string paymentState { get; set; }
            public int? feeAmount { get; set; }
        }

        public class GetOrderStatusExtended_fromSber_bankInfo
        {
            public string bankName { get; set; }
            public string bankCountryCode { get; set; }
            public string bankCountryName { get; set; }
        }

        public class GetOrderStatusExtended_fromSber_payerData
        {
            public string email { get; set; }
            public object transactionAttributes { get; set; }
        }

        /// <summary>
        /// Register
        /// </summary>
        public class Register_fromSber
        {
            public string orderId { get; set; }
            public string formUrl { get; set; }
            public int? errorCode { get; set; }
            public string errorMessage { get; set; }
            public Register_fromSber_externalParams externalParams { get; set; }
        }

        public class Register_fromSber_externalParams
        {
            public string sbolDeepLink { get; set; }
            public string sbolBankInvoiceId { get; set; }
        }

        public class Register_toSber
        {
            public string userName { get; set; }
            public string password { get; set; }
            public string returnUrl { get; set; }
            public string token { get; set; }
            public string orderNumber { get; set; }
            public int? amount { get; set; } 
            public int? currency { get; set; }
            public string failUrl { get; set; } 
            public string description { get; set; }
            public string language { get; set; }
            public string pageView { get; set; }
            public string clientId { get; set; }
            public string merchantLogin { get; set; }
            public string jsonParams { get; set; }
            public int? sessionTimeoutSecs { get; set; }
            public string expirationDate { get; set; }
            public string bindingId { get; set; }
            public string features { get; set; }
            public string email { get; set; }
            public long? phone { get; set; }


        }
    }


}
