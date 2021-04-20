using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

public class OutValue : System.Attribute
{
    public OutValue() { }
}

namespace Sb
{

    public class ApplicationContext : DbContext
    {
        public virtual DbSet<PaymentRequestHistory> paymentRequestHistories { get; set; }
        public virtual DbSet<OrderInfo> orderInfo { get; set; }

        public bool SpExec(string spname, ref object obj)
        {
            return new Executor(Database).SpExec(spname, ref obj);
        }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
                : base(options)
        {
            Database.EnsureCreated();
        }


    }

    class CheckOrderStatus
    {
        public string orderNumber { get; set; }
        [OutValue]
        public string answer { get; set; }
    }

    [Table("OrderInf")]
    public class OrderInfo
    {
        public int id { get; set; }
        public int response_id { get; set; }
        public int emiss { get; set; }
        public decimal amount { get; set; }
        public string orderNumber { get; set; }
    }

    [Table("PayHist")]
    public class PaymentRequestHistory
    {
        public int id { get; set; }
        public string uid { get; set; }
        public string url { get; set; }
        public string body { get; set; }
        public bool isResponse { get; set; }
        public DateTime date { get; set; }
    }


    public class CallBack
    {
        public string mdOrder { get; set; }
        public string orderNumber { get; set; }
        public string checksum { get; set; }
        public string operation { get; set; }
        public bool status { get; set; }
        public string request { get; set; }
        public string ip { get; set; }
        public DateTime date { get; set; }
    }
}

namespace Rc
{


    public class ApplicationContext : DbContext
    {


        public bool SpExec(string spname, ref object obj)
        {
            return new Executor(Database).SpExec(spname, ref obj);
        }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
                : base(options)
        {
            Database.EnsureCreated();
        }


    }

    public class Validate
    {
        [StringLength(128)]
        public string txn_id { get; set; }
        public int account { get; set; }
        public decimal? amount { get; set; }
        [OutValue]
        public int error_code { get; set; }
        [OutValue]
        //[StringLength(400)]
        public string error_text { get; set; }
        [OutValue]
        public decimal tariff_price { get; set; }
       
    }


    public class Create
    {
        [StringLength(32)]
        public string txn_id { get; set; }
        public int account { get; set; }
        public decimal amount { get; set; }
        [OutValue]
        public int error_code { get; set; }
        [OutValue]
        //[StringLength(400)]
        public string error_text { get; set; }
    }


}



public class Executor
{
    private DatabaseFacade Database = null;

    public Executor(DatabaseFacade Database)
    {
        this.Database = Database;
    }
        

    public bool SpExec(string spname, ref object obj)
    {
        try
        {
            if (Database.GetDbConnection().State != ConnectionState.Open)
                Database.GetDbConnection().Open();

            using (var cmd = Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = spname;

                List<string> ouputsNames = new List<string>();

                foreach (PropertyInfo p in obj.GetType().GetProperties())
                {
                    var val = obj.GetType().GetProperty(p.Name).GetValue(obj, null) ?? DBNull.Value;
                    if (val.GetType() == typeof(DateTime) && (DateTime)val < DateTime.Now.AddYears(-100))
                        continue;

                    var parametr = new SqlParameter("@" + p.Name, val);                    
                    if (p.CustomAttributes.Where(m => m.AttributeType.Name == "OutValue").Count() == 1)
                    {
                        if (p.PropertyType.Name == "String")
                        {
                            var strLen = p.CustomAttributes.Where(m => m.AttributeType.Name == "StringLengthAttribute");
                            if (strLen.Count() == 1)
                            {
                                parametr.Size = (int)strLen.First().ConstructorArguments.First().Value;
                            }
                            else
                                parametr.Size = 8000;
                        }

                        parametr.Direction = ParameterDirection.Output;
                        ouputsNames.Add(p.Name);
                    }
                    cmd.Parameters.Add(parametr);
                }

                cmd.ExecuteNonQuery();

                foreach (var pname in ouputsNames)
                {
                    var outVal = cmd.Parameters["@" + pname].Value;
                    if (outVal != DBNull.Value && outVal != null)
                        obj.GetType().GetProperty(pname).SetValue(obj, cmd.Parameters["@" + pname].Value);
                }

                return true;
            }
            throw new Exception();
        }
        catch
        {
            return false;
        }
        finally
        {
            if (Database.GetDbConnection().State == ConnectionState.Open)
                Database.GetDbConnection().Close();
        }
    }
}


