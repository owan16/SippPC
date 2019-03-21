using Newtonsoft.Json;
using Sipp_PC.Sensor;
using System;
using System.IO;
using System.Web.Http;

namespace Sipp_PC
{
    public class ThingController: ApiController
    {

        public string Get(string type) {
            
            //要輸出的變數
            StringWriter sw = new StringWriter();

            string status = "";
            if (type.Equals("system"))
            {
                try
                {
                    status = SippThing.Instance.GetState() == SippThing.STATE_READY ? "READY" : "NOT_READY";
                }
                catch (System.NullReferenceException e) {
                    status = "NOT_READY";
                }
            } else
            {
                status = "Unknown type";
            }

            
       
            //建立JsonTextWriter
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.WriteStartObject();
            writer.WritePropertyName("status"); writer.WriteValue(status);
            writer.WriteEndObject();

            return sw.ToString();

        }
        
        
    }
}
