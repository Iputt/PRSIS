using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using Owl.Feature.Impl.Variable;
using Owl.Domain;
namespace Owl.Feature
{
    /// <summary>
    /// 变量管理
    /// </summary>
    public class Variable : Engine<VariableProvider, Variable>
    {
        static readonly string key = "owl.util.variable";

        public static IDictionary<string, object> CurrentParameters
        {
            get
            {
                return Cache.Thread<IDictionary<string, object>>(key, () =>
                {
                    var values = new Dictionary<string, object>();
                    values["now"] = DateTime.Now;
                    return values;
                });
            }
        }
        static bool _Contain(string key)
        {
            if (CurrentParameters.ContainsKey(key))
                return true;
            return Execute2<string, bool>(s => s.Contain, key, s => s == true);
        }
        public static bool Contain(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (_Contain(key))
                return true;
            key = key.Split('.')[0];
            if (_Contain(key))
                return true;
            return false;

        }

        static object _GetValue(string key)
        {
            object value = null;
            if (CurrentParameters.ContainsKey(key))
                value = CurrentParameters[key];
            if (value == null)
                value = Execute2<string, object>(s => s.GetValue, key);
            return value;
        }

        public static object GetValue(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return null;
            var value = _GetValue(parameter);
            if (value != null)
                return value;

            var tmp = parameter.Split(new char[] { '.' }, 2);
            value = _GetValue(tmp[0]);

            if (value != null && tmp.Length == 2)
            {
                var tobj = value as Object2;
                if (tobj != null)
                    return tobj.GetDepthValue(tmp[1]);
            }
            return value;
        }

        public static object GetValue(object value, bool split = false)
        {
            if (value is string && (string)value != string.Empty)
            {
                var tmp = value as string;
                if (split)
                {
                    return tmp.Split(',').Select(s => s[0] == '@' ? GetValue(s.Substring(1)) : s);
                }
                if (tmp[0] == '@')
                {
                    return GetValue(tmp.Substring(1));
                }
            }
            return value;
        }
    }
}
