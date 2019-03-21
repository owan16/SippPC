using Newtonsoft.Json;
using Sipp_PC.Sensor;
using System;
using System.IO;
using System.Web.Http;


namespace Sipp_PC
{
    public class PedometerController : ApiController
    {


        public string Get(string type)
        {
            Console.WriteLine(type);
            //要輸出的變數
            StringWriter sw = new StringWriter();
            //建立JsonTextWriter
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            if (type.Equals("step"))
            {
                
                writer.WriteStartObject();
                writer.WritePropertyName("value"); writer.WriteValue(SippThing.Instance.step);
                writer.WriteEndObject();
                
     
            }

            if (type.Equals("currentStep"))
            {

                writer.WriteStartObject();
                writer.WritePropertyName("value"); writer.WriteValue(SippThing.Instance.currentStep);
                writer.WriteEndObject();


            }

            else if (type.Equals("calorie"))
            {
                
                
                writer.WriteStartObject();
                writer.WritePropertyName("value"); writer.WriteValue(SippThing.Instance.calorie);
                writer.WriteEndObject();
                
            }
            else if (type.Equals("distance"))
            {
                
               
                writer.WriteStartObject();
                writer.WritePropertyName("value"); writer.WriteValue(SippThing.Instance.distance);
                writer.WriteEndObject();
                
            }
            else if (type.Equals("duration")) {
                
                
                writer.WriteStartObject();
                writer.WritePropertyName("value"); writer.WriteValue(SippThing.Instance.duration);
                writer.WriteEndObject();
                
            }
            Console.WriteLine(sw.ToString());
            return sw.ToString();
        }
    }
}
