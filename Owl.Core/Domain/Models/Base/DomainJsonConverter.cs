using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Owl.Util;

namespace Owl.Domain
{
    public class DomainConverter : xJsonConverter
    {
        static Type xobject = typeof(Object2);
        static Type domain = typeof(DomainObject);

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsSubclassOf(xobject))
                return true;
            return false;
        }

        protected object fromToken(JToken token)
        {
            if (token.Type == JTokenType.Array)
            {
                List<object> objs = new List<object>();
                foreach (var v in token)
                {
                    objs.Add(fromToken(v));
                }
                return objs;
            }
            else if (token.Type == JTokenType.Object)
            {
                TransferObject obj = new TransferObject();
                foreach (JProperty property in token)
                {
                    obj[property.Name] = fromToken(property.Value);
                }
                return obj;
            }
            else
            {
                switch (token.Type)
                {
                    case JTokenType.Null:
                    case JTokenType.None:
                    case JTokenType.Undefined: return null;
                    default: return ((JValue)token).Value;
                }
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dto = (TransferObject)fromToken((JToken)serializer.Deserialize(reader));
            if (objectType == typeof(TransferObject))
                return dto;
            if (dto != null)
            {
                Object2 obj = null;
                if (objectType.IsSubclassOf(domain))
                    obj = DomainFactory.Create(dto.__ModelName__ ?? objectType.MetaName());
                else
                    obj = Activator.CreateInstance(objectType) as Object2;
                obj.Write(dto);
                return obj;
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((Object2)value).Read().ToDict());
        }
    }
}
