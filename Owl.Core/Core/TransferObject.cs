using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using Owl.Util;

namespace System
{
    /// <summary>
    /// 数据传输对象
    /// </summary>
    public class TransferObject : Object2, IDictionary<string, object>//  IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// 
        /// </summary>
        protected IDictionary<string, object> InnerDict { get; private set; }

        public TransferObject()
        {
            InnerDict = new Dictionary<string, object>();
        }

        public TransferObject(int capacity)
        {
            InnerDict = new Dictionary<string, object>(capacity);
        }

        public TransferObject(Dictionary<string, object> dict)
            : this((IDictionary<string, object>)dict)
        {

        }

        public TransferObject(IDictionary<string, object> values)
        {
            InnerDict = new Dictionary<string, object>();
            if (values != null && values.Count > 0)
            {
                foreach (var key in values.Keys)
                    this[key] = values[key];
            }
        }

        public TransferObject(IDictionary dict)
        {
            InnerDict = new Dictionary<string, object>();
            foreach (var key in dict.Keys)
            {
                InnerDict[key.ToString()] = dict[key];
            }
        }

        public TransferObject(TransferObject obj)
        {
            if (obj == null)
                InnerDict = new Dictionary<string, object>();
            else
                InnerDict = new Dictionary<string, object>(obj.InnerDict);
        }

        public TransferObject(IEnumerable<KeyValuePair> pairs)
        {
            InnerDict = new Dictionary<string, object>();
            foreach (var pair in pairs)
                InnerDict[pair.Key] = pair.Value;
        }
        public override object this[string property]
        {
            get
            {
                if (InnerDict.ContainsKey(property))
                    return InnerDict[property];
                return null;
            }
            set
            {
                if (value is IDictionary<string, object> && !(value is TransferObject))
                    InnerDict[property] = new TransferObject(value as IDictionary<string, object>);
                else if (value is IEnumerable<IDictionary<string, object>> && !(value is IEnumerable<TransferObject>))
                {
                    List<TransferObject> tol = new List<TransferObject>();
                    foreach (IDictionary<string, object> vd in value as IEnumerable<IDictionary<string, object>>)
                    {
                        tol.Add(new TransferObject(vd));
                    }
                    InnerDict[property] = tol;
                }
                else
                    InnerDict[property] = value;
            }
        }

        public override IEnumerable<string> Keys
        {
            get { return InnerDict.Keys; }
        }

        public IEnumerable<object> Values
        {
            get { return InnerDict.Values; }
        }

        public override bool ContainsKey(string key)
        {
            return InnerDict.ContainsKey(key);
        }

        protected override TransferObject _Read(bool hasdisplay)
        {
            return new TransferObject(this);
        }

        public override void Write(Object2 dto)
        {
            if (dto == null)
                return;
            foreach (var key in dto.Keys)
            {
                this[key] = dto[key];
            }
        }
        public override void Write(IDictionary<string, object> dto)
        {
            if (dto == null || dto.Count == 0)
                return;
            foreach (var pair in dto)
            {
                this[pair.Key] = pair.Value;
            }
        }

        public void Add(string key, object value)
        {
            this[key] = value;
        }

        /// <summary>
        /// 删除之间关键字的元素
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (!key.Contains("."))
                return InnerDict.Remove(key);
            var tmp = key.Split(new char[] { '.' }, 2);
            var value = InnerDict[tmp[0]];
            if (value is IEnumerable<TransferObject>)
            {
                var succ = false;
                foreach (var dto in value as IEnumerable<TransferObject>)
                {
                    if (dto.Remove(tmp[1]))
                        succ = true;
                }
                return succ;
            }
            return false;
        }

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public int Count
        {
            get { return InnerDict.Count; }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return InnerDict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [IgnoreField]
        public string __ModelName__
        {
            get { return this["__ModelName__"] as string; }
            set { this["__ModelName__"] = value; }
        }



        bool IsObjectArray(object value)
        {
            if (value == null)
                return false;
            var type = value.GetType().ToString();
            return type == "System.Object[]";
        }

        /// <summary>
        /// 是否是List<object>类型对象
        /// API接口取数据之后，经过接口转换，会被转换为List<object>而不是object[]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool IsObjectList(object value)
        {
            if (value == null)
                return false;
            var type = value.GetType().ToString();
            return type == "System.Collections.Generic.List`1[System.Object]"; 
        }
        /// <summary>
        /// 获取排序的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="convertnull">是否将Null转为空字符串</param>
        /// <returns></returns>
        public object GetSortValue(string key, bool convertnull = true)
        {
            var value = this[key];
            if (value == null && convertnull)
                return "";
            if (IsObjectArray(value))
            {
                var tmp = value as object[];
                if (tmp.Length == 2)
                    return tmp[1] as string;
                else
                    return string.Join(",", tmp);
            }
            return value;
        }

        protected override string _GetDisplay(string key)
        {
            var value = this[key];
            if (value == null)
                return "";
            if (IsObjectArray(value))
            {
                var tmp = value as object[];
                if (tmp.Length == 2)
                    return tmp[1] as string;
                else
                    return string.Join(",", tmp);
            }
            //如果是ObjectList类型,先转换为List<object>再做处理
            if (IsObjectList(value))
            {
                var tmp = value as List<object>;
                if (tmp.Count == 2)
                    return tmp[1] as string;
                else
                    return string.Join(",", tmp);
            }
            return value.ToString();
        }
        protected override object _GetRealValue(string key)
        {
            var value = this[key];
            if (IsObjectArray(value))
            {
                var tmp = value as object[];
                if (tmp.Length == 2)
                    value = tmp[0];
            } 
            //如果是ObjectList类型,先转换为List<object>再做处理
            if (IsObjectList(value))
            {
                var tmp = value as List<object>;
                if (tmp.Count == 2)
                    value = tmp[0];
            }
            return value;
        }
        public void Clear()
        {
            InnerDict.Clear();
        }
        public TransferObject ToDisplay()
        {
            var result = new TransferObject();
            foreach (var key in Keys)
            {
                result[key] = GetDisplay(key);
            }
            return result;
        }
        /// <summary>
        /// 转为字典
        /// </summary>
        public IDictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>(InnerDict.Count);
            foreach (var pair in InnerDict)
            {
                var value = pair.Value;
                if (pair.Value is TransferObject)
                    value = (value as TransferObject).ToDict();
                else if (value is IEnumerable<TransferObject>)
                {
                    var tvalues = new List<IDictionary<string, object>>();
                    foreach (TransferObject v in (IEnumerable)value)
                    {
                        tvalues.Add(v.ToDict());
                    }
                    value = tvalues;
                }
                dict[pair.Key] = value;
            }
            return dict;
        }
        #region 从NameValueCollection 转换为 TransferObject
        static readonly Regex pararegex = new Regex(@"^([\w|\.]*)(\[(\d{1,3})\])?(\[([\w\.]+)\])?(\[(\d{1,3})\])?(\[\])?");
        static string ValidateInput(string input)
        {
            if (input == "null" || input == "undefined" || string.IsNullOrEmpty(input))
                return null;
            return input;
        }
        public static void ParseInput(TransferObject obj, string key, NameValueCollection coll)
        {
            var val = ValidateInput(coll[key].Trim());
            object value = val;
            Match match = pararegex.Match(key);
            if (match.Success)
            {
                string property = match.Groups[1].Value;
                string field = match.Groups[5].Success ? match.Groups[5].Value : null;
                bool isarray = match.Groups[8].Success;
                TransferObject dto = obj;
                if (match.Groups[3].Success && match.Groups[5].Success)
                {
                    int index = int.Parse(match.Groups[3].Value);
                    if (!obj.ContainsKey(property))
                        obj[property] = new List<TransferObject>();
                    var collection = obj[property] as List<TransferObject>;
                    for (int i = 0; i <= index - collection.Count; i++)
                    {
                        collection.Add(new TransferObject());
                    }
                    dto = collection[index];
                }
                else if (match.Groups[5].Success)
                {
                    if (!obj.ContainsKey(property))
                        obj[property] = new TransferObject();
                    dto = obj[property] as TransferObject;
                }
                object v = value;
                if (isarray)
                {
                    if (val == "true,false")
                        v = "true";
                    else
                        v = coll.GetValues(key);
                }
                else if (property.Contains("."))
                {
                    var tmpkey = property.Split('.');
                    property = tmpkey[0];
                }
                dto[field ?? property] = v;
            }
            else if (!obj.ContainsKey(key))
            {
                obj[key] = value;
            }
        }
        /// <summary>
        /// 从NameValueCollection 转换为 TransferObject
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static TransferObject Parse(NameValueCollection coll)
        {
            TransferObject obj = new TransferObject();
            foreach (var key in coll.AllKeys)
            {
                //var val = ValidateInput(coll[key].Trim());
                ParseInput(obj, key, coll);
            }
            return obj;
        }
        #endregion

        ICollection<string> IDictionary<string, object>.Keys => InnerDict.Keys;

        ICollection<object> IDictionary<string, object>.Values => InnerDict.Values;

        public bool IsReadOnly => InnerDict.IsReadOnly;

        public bool TryGetValue(string key, out object value)
        {
            return InnerDict.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return InnerDict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            InnerDict.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return InnerDict.Remove(item);
        }
    }

}
