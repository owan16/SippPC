using Newtonsoft.Json;
using Sipp_PC.Sensor;
using System.IO;
using System.Web.Http;

namespace Sipp_PC.Restful
{
    class AccelerometerController: ApiController
    {
        public string Get()
        {
            StringWriter sw = new StringWriter();
            //建立JsonTextWriter
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(SippThing.Instance.aX);
            writer.WritePropertyName("y"); writer.WriteValue(SippThing.Instance.aY);
            writer.WritePropertyName("z"); writer.WriteValue(SippThing.Instance.aZ);
            writer.WriteEndObject();
            return sw.ToString();
        }
    }
}
