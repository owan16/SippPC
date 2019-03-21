using Newtonsoft.Json;
using Sipp_PC.Sensor;
using System.IO;
using System.Web.Http;

namespace Sipp_PC.Restful
{
    class GyroController: ApiController
    {
        public string Get()
        {
            StringWriter sw = new StringWriter();
            //建立JsonTextWriter
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(Sipp6X.Instance.gX);
            writer.WritePropertyName("y"); writer.WriteValue(Sipp6X.Instance.gY);
            writer.WritePropertyName("z"); writer.WriteValue(Sipp6X.Instance.gZ);
            writer.WriteEndObject();
            return sw.ToString();
        }
    }
}
