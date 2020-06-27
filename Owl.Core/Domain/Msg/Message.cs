using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Owl.Domain.Driver;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MsgType
    {
        /// <summary>
        /// 普通消息
        /// </summary>
        General,
        /// <summary>
        /// 只读消息
        /// </summary>
        Read
    }

    /// <summary>
    /// 消息
    /// </summary>
    public class Message : SmartObject
    {
        /// <summary>
        /// 消息名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; private set; }
        /// <summary>
        /// 对象修改时间
        /// </summary>
        public DateTime? Modified { get; private set; }

        /// <summary>
        /// 是否可异步执行
        /// </summary>
        public bool Async { get; private set; }

        #region 元数据相关
        ModelMetadata metadata;
        /// <summary>
        /// 元数据
        /// </summary>
        [IgnoreField]
        public ModelMetadata Metadata
        {
            get
            {
                if (metadata == null && !string.IsNullOrEmpty(ModelName))
                    metadata = ModelMetadataEngine.GetModel(ModelName);
                return metadata;
            }
            set { metadata = value; }
        }

        public IEnumerable<object> GetKeys(bool findfromdb = false)
        {
            var pname = Metadata.PrimaryField == null ? "Id" : metadata.PrimaryField.Name;
            var ptype = Metadata.PrimaryField == null ? typeof(Guid) : metadata.PrimaryField.PropertyType;
            if (!Param.ContainsKey(pname))
                return findfromdb ? _GetKeys() : null;

            var value = Param[pname];
            List<object> keys = new List<object>();
            if (value is string || !(value is IEnumerable))
            {
                keys.Add(Util.Convert2.ChangeType(value, ptype));
            }
            else if (value != null)
            {
                foreach (var v in (IEnumerable)value)
                    keys.Add(Util.Convert2.ChangeType(v, ptype));
            }
            Param[pname] = keys;
            return keys;
        }
        protected virtual IEnumerable<object> _GetKeys()
        {
            return null;
        }
        /// <summary>
        /// 是否必须至少选中一行
        /// </summary>
        /// <param name="needcheck"></param>
        /// <returns></returns>
        public IEnumerable<AggRoot> GetRoot(RootRestrict restrict = RootRestrict.Special)
        {
            var keys = GetKeys();
            if (keys != null)
            {
                var spec = Specification.Create(Metadata.PrimaryField.Name, CmpCode.IN, keys);
                return Repository.FindAll(Metadata, spec.GetExpression(Metadata));
            }
            else
            {

                if (restrict == RootRestrict.All || Param.GetRealValue<bool>("__selectall"))
                    return _getroot(Param.GetRealValue<string>("__gSearch__"));
                else if (restrict == RootRestrict.None)
                    return new List<AggRoot>();
                else
                    throw new AlertException("error.owl.domain.message.roots.noexists", "本消息没有有效的对象！");
            }
        }

        protected virtual IEnumerable<AggRoot> _getroot(string gsearch = "")
        {
            return Repository.FindAll(Metadata, "");
        }
        #endregion

        public Message()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="model">对象名称</param>
        /// <param name="type">消息类型</param>
        /// <param name="async">是否异步执行</param>
        public Message(string name, string model, bool async = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(model))
                throw new ArgumentNullException("model");
            Name = name;
            ModelName = model;
            Async = async;
        }

        /// <summary>
        /// 请求
        /// </summary>
        public TransferObject Query { get; set; }


        TransferObject m_param;
        /// <summary>
        /// 额外参数
        /// </summary>
        public TransferObject Param
        {
            get
            {
                if (m_param == null)
                    m_param = new TransferObject();
                return m_param;
            }
            set
            {
                m_param = value;
            }
        }
        TransferObject m_context;
        /// <summary>
        /// 上下文依赖值
        /// </summary>
        public TransferObject Context
        {
            get
            {
                if (m_context == null)
                    m_context = new TransferObject();
                return m_context;
            }
            set
            {
                m_context = value;
            }
        }
        TransferObject m_body;
        /// <summary>
        /// 消息体
        /// </summary>
        public TransferObject Body
        {
            get
            {
                if (m_body == null)
                    m_body = new TransferObject();
                return m_body;
            }
            set
            {
                m_body = value;
            }
        }

        public string GetFieldDisplay(string key)
        {
            if (!Body.ContainsKey(key) || Body[key] == null)
                return "";
            var v = Body[key];
            if (v is string[])
            {
                var value = v as string[];
                if (value.Length == 2)
                    return value[1];
                return string.Join(",", value);
            }
            //api端参数会被Json解析为List<object>类型
            else if (v is List<object>)
            {
                var value = v as List<object>;
                if (value.Count == 2)
                    return value.LastOrDefault() != null ? value.LastOrDefault().ToString() : "";
                return string.Join(",", value.Where(t => t != null));
            }
            return v.ToString();
        }

        #region 创建消息
        /// <summary>
        /// 根据上下文初始化Message
        /// </summary>
        /// <param name="query">查询</param>
        /// <param name="form">表单</param>
        protected virtual void Initialize(TransferObject query, TransferObject form)
        {
            foreach (var key in query.Keys)
            {
                var value = query[key];
                switch (key)
                {
                    case "name": Name = (string)value; break;
                    case "model": ModelName = (string)value; break;
                    case "modified":
                        long unixTimeStamp = long.Parse((string)value);
                        System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
                        Modified = startTime.AddSeconds(unixTimeStamp);
                        break;
                    case "async": Async = bool.Parse((string)value); break;
                    case "param": Param = value as TransferObject; break;
                    case "context": Context = value as TransferObject; break;
                }
            }
            Query = query;
            Body = form ?? new TransferObject();
            if (Body.ContainsKey("__msg_param__"))
            {
                Param = Body["__msg_param__"] as TransferObject;
            }
            if (Body.ContainsKey("__msg_context__"))
            {
                Context = Body["__msg_context__"] as TransferObject;
            }
        }

        /// <summary>
        /// 根据上下文创建消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">上下文</param>
        /// <param name="form">本次提交的数据</param>
        /// <returns></returns>
        public static T Create<T>(TransferObject query, TransferObject form = null)
            where T : Message, new()
        {
            var t = new T();
            t.Initialize(query, form);
            return t;
        }

        public static Message Create(Type msgtype, TransferObject query, TransferObject form = null)
        {
            if (msgtype == null)
                throw new ArgumentNullException("msgtype");
            var message = Activator.CreateInstance(msgtype) as Message;
            if (message != null)
                message.Initialize(query, form);
            return message;
        }

        /// <summary>
        /// 根据上下文创建消息
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="form">本次提交的数据</param>
        /// <returns></returns>
        public static Message Create(TransferObject context, TransferObject form = null)
        {
            return Create<Message>(context, form);
        }

        public static Message FromJson(string msgjson)
        {
            var dto = msgjson.DeJson<TransferObject>();
            //var type = Type.GetType(dto.GetRealValue<string>("Name"));
            //请求传递过来name是小写的
            var type = Type.GetType(dto.GetRealValue<string>("name"));
            return Create(type, dto.GetRealValue<TransferObject>("query"), dto.GetRealValue<TransferObject>("form"));
        }

        #endregion

        public override string ToString()
        {
            TransferObject obj = new TransferObject();
            var type = GetType();
            obj["name"] = string.Format("{0},{1}", type.FullName, type.Assembly.GetName().Name);
            obj["query"] = Query;
            obj["form"] = Body;
            return obj.ToJson();
        }
    }
}
