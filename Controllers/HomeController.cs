using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop.Infrastructure;
using Newtonsoft.Json;
using PayPage.Models;

namespace PayPage.Controllers
{

    public class HomeController : Controller
    {
        private readonly Rc.ApplicationContext _rc_context;
        private readonly Sb.ApplicationContext _sb_context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, Rc.ApplicationContext rc_context, Sb.ApplicationContext sb_context)
        {
            try
            {
                _rc_context = rc_context;
                _sb_context = sb_context;
                _logger = logger;
            }
            catch (Exception e)
            {
                Log.Write(e);
            }

        }

        public IActionResult PrivacyPolicy() 
        {
            return View();
        }

        [HttpGet]
        public IActionResult PayResultTest()
        {
            try
            {
                SberClasses.Register_toSber register_toSber = new SberClasses.Register_toSber()
                {
                    amount = 1000,
                    orderNumber = Guid.NewGuid().ToString().Replace("-", "0").ToUpper()
                };
                Config.SetSessionValue(HttpContext, Config.ss_SberValues_register_toSber);
                ViewData[Config.vd_ServerMessagePay] = $"Операция № {register_toSber.orderNumber} на сумму {(int)(register_toSber.amount / 100)}руб. {(register_toSber.amount % 100)}коп. отправлена в обработку со статусом: тестовая операция";
                ViewData[Config.vd_TmpStatus + register_toSber.orderNumber] = 0;
                ViewData[Config.vd_TmpValue + register_toSber.orderNumber] = register_toSber.orderNumber;
            }
            catch (Exception e)
            {
                Log.Write(e);
            }
            return View("PayResult");
        }

        [HttpGet]
        public IActionResult PayStatus()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PayStatus(string page ,string order = null)
        {
            try
            {
                if (order == null || string.IsNullOrEmpty(order))
                {
                    ViewData[Config.vd_ServerMessageStatus] = "Неверный номер операции";
                    return View(page);
                }
                else
                {
                    order = order.ToUpper().Trim();
                    if (_sb_context.orderInfo.Where(m => m.orderNumber.ToUpper().Trim() == order).FirstOrDefault() == null)
                    {
                        ViewData[Config.vd_ServerMessageStatus] = "Операция с номером " + order + " не найдена";
                        return View(page);
                    }
                    else
                        ViewData[Config.vd_TmpValue] = order;
                }
            }
            catch (Exception e)
            {
                Log.Write(e);
            }
            return View();
        }

        public IActionResult PayResult()
        {
            return View();
        }


        [HttpGet]
        public IActionResult GetRes()
        {
            try
            {
                SberClasses.Register_toSber register_toSber = Config.GetSessionValue<SberClasses.Register_toSber>(HttpContext, Config.ss_SberValues_register_toSber);
                SberClasses.Register_fromSber register_fromSber = Config.GetSessionValue<SberClasses.Register_fromSber>(HttpContext, Config.ss_SberValues_register_fromSber);
                
                var getOrderStatusExtended_toSber = new SberClasses.GetOrderStatusExtended_toSber()
                {
                    orderId = register_fromSber.orderId,
                    orderNumber = register_toSber.orderNumber,
                    password = register_toSber.password,
                    userName = register_toSber.userName
                };              

                var getOrderStatusExtended_fromSber = LNet.reqResp<SberClasses.GetOrderStatusExtended_fromSber>(HttpContext, _sb_context ,getOrderStatusExtended_toSber, LNet.addr_getOrderStatusExtended, HttpMethod.Post);

                Config.SetSessionValue(HttpContext, Config.ss_SberValues_getOrderStatusExtended_toSber,getOrderStatusExtended_toSber);
                Config.SetSessionValue(HttpContext, Config.ss_SberValues_getOrderStatusExtended_fromSber,getOrderStatusExtended_fromSber);

                ViewData[Config.vd_ServerMessagePay] = $"Операция № {getOrderStatusExtended_fromSber.orderNumber} на сумму {(int)(getOrderStatusExtended_fromSber.amount/100)}руб. {(getOrderStatusExtended_fromSber.amount % 100)}коп. отправлена в обработку со статусом: {getOrderStatusExtended_fromSber.errorMessage}";
                ViewData[Config.vd_TmpStatus + register_toSber.orderNumber] = getOrderStatusExtended_fromSber.errorCode;
                ViewData[Config.vd_TmpValue + register_toSber.orderNumber] = getOrderStatusExtended_fromSber.orderNumber;
                return View("PayResult");
            }
            catch(Exception e)
            {
                Log.Write(e);
                return RedirectToAction("Error");
            }
        }

        [HttpGet]
        public IActionResult Index()
        {            
            return View();
        }

       


        [HttpPost]
        public JsonResult CheckStatus(string orderNumber)
        {
            try
            {
                object parametrs = new Sb.CheckOrderStatus() { orderNumber = orderNumber };
                _sb_context.SpExec(Config.DbPrefixSb + "CheckOrderStatus", ref parametrs);

                return new JsonResult(((Sb.CheckOrderStatus)parametrs).answer);
            }
            catch (Exception e)
            {
                Log.Write(e);
                return null;
            }
        }

        [HttpPost]
        public object DynamicValidate(string card, string sum = null)
        {
            try
            {
                if (int.TryParse(card, out int account))
                {
                    object sia_v = new Rc.SIA_Validate()
                    {
                        account = account
                    };

                    decimal decamount;
                    if (sum != null && decimal.TryParse(sum.Replace(".", ","), out decamount))
                        ((Rc.SIA_Validate)sia_v).amount = decamount;

                    if (_rc_context.SpExec(Config.DbPrefixRc + "SIA_Validate", ref sia_v))
                    {
                        if (((Rc.SIA_Validate)sia_v).error_code == 0 && ((Rc.SIA_Validate)sia_v).tariff_price == 0)
                            return null;
                        else
                            return new JsonResult(new { text = ((Rc.SIA_Validate)sia_v).error_text, price = ((Rc.SIA_Validate)sia_v).tariff_price });
                    }


                }
                return null;
            }
            catch (Exception e)
            {
                Log.Write(e);
                return null;
            }
        }


        [HttpPost]
        public IActionResult Index(string card, string sum)
        {
            try
            {

                var register_toSber = new SberClasses.Register_toSber();

                

                if (string.IsNullOrEmpty(card) || !int.TryParse(card, out int account))
                {
                    ViewData[Config.vd_ServerMessagePay] = "Неверный номер карты";
                    return View();
                }
                else
                {
                    if (sum == null || !decimal.TryParse(sum.Replace(".", ","), out decimal decamount))
                    {

                        ViewData[Config.vd_ServerMessagePay] = "Неверная сумма пополнения";
                        return View();
                    }
                    else
                    {
                        var amount = (int)(decamount * 100);
                        object sia_v = new Rc.SIA_Validate()
                        {
                            account = account,
                            amount = decamount
                        };                       

                        var isOk = _rc_context.SpExec(Config.DbPrefixRc + "SIA_Validate", ref sia_v) && ((Rc.SIA_Validate)sia_v).error_code == 0;

                        if (!isOk)
                        {
                            ViewData[Config.vd_ServerMessagePay] = "Номер карты не зарегистрирован в системе";
                            return View();
                        }

                        if (((Rc.SIA_Validate)sia_v).tariff_price > 0)
                            register_toSber.amount = (int)(((Rc.SIA_Validate)sia_v).tariff_price * 100);
                        else
                            register_toSber.amount = amount;

                        register_toSber.userName = LNet.apiLogin;
                        register_toSber.password = LNet.apiPass;
                        register_toSber.orderNumber = Guid.NewGuid().ToString().Replace('-', Config.RndChar()).ToUpper().Substring(0,32);
                        register_toSber.returnUrl = Config.ReturnUrl + "GetRes";

                        Config.SetSessionValue(HttpContext, Config.ss_OrderInfo, new Sb.OrderInfo()
                        {
                            amount = decamount,
                            emiss = account,
                            orderNumber = register_toSber.orderNumber                           
                        });

                        var register_fromSber = LNet.reqResp<SberClasses.Register_fromSber>(HttpContext, _sb_context, register_toSber, LNet.addr_register, HttpMethod.Post);

                        if ((register_fromSber.errorCode ?? 0) != 0)
                        {
                            ViewData[Config.vd_ServerMessagePay] = register_fromSber.errorMessage;// "Проблема соединения с сервером, попробуйте еще раз";
                            return View();
                        }
                       

                        Config.SetSessionValue(HttpContext, Config.ss_SberValues_register_toSber, register_toSber);
                        Config.SetSessionValue(HttpContext, Config.ss_SberValues_register_fromSber, register_fromSber);

                        ViewData[Config.vd_Redirect] = register_fromSber.formUrl;

                    
                    }

                }


                return View();
            }
            catch (Exception e)
            {
                Log.Write(e);
                try
                {
                    HttpContext.Session.Clear();
                }
                catch { }
                return Index();
                //return RedirectToAction("Error");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
