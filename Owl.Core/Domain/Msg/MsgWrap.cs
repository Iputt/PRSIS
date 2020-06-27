//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Collections;
//using Owl.Util;
//using Owl.Feature;
//namespace Owl.Domain
//{
//    /// <summary>
//    /// 消息封装处理器
//    /// </summary>
//    public class MsgWrapHandler : MsgHandler
//    {
//        MsgHandler m_inner;
//        string m_msgkey;
//        public MsgWrapHandler(MsgHandler inner, string msgkey)
//        {
//            m_inner = inner;
//            m_msgkey = msgkey;
//            Descrip = inner.Descrip;
//        }

//        protected override void OnLoad()
//        {
//            foreach (var pair in m_inner.GetDefault(ObjectKeys))
//                AppendResult(pair.Key, pair.Value);
//        }

//        protected override void Prepare()
//        {

//        }

//        public override object Execute()
//        {
//            return Descrip.Wrapper.Execute(m_inner, Message, m_msgkey);
//        }
//    }
//    /// <summary>
//    /// 消息处理封装器
//    /// </summary>
//    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
//    public abstract class MsgHandlerWrapper : Attribute
//    {

//        Dictionary<FieldMetadata, string> Parse(ModelMetadata meta, TransferObject dto)
//        {
//            var sb = new Dictionary<FieldMetadata, string>();
//            foreach (var field in meta.GetFields())
//            {
//                var tmp = "";
//                if (field.Field_Type == FieldType.one2many)
//                {
//                    var value = dto[field.Name];
//                    var tmps = new List<string>();
//                    if (value != null)
//                    {

//                        tmps.Add(string.Format("{0}：", field.Label));
//                        if (value is string)
//                            value = (value as string).DeJson<List<TransferObject>>();
//                        NavigatField nfield = field as NavigatField;
//                        foreach (TransferObject v in (IEnumerable)value)
//                        {
//                            tmps.Add("  " + string.Join(";", Parse(nfield.RelationModelMeta, v).Values));
//                        }
//                    }
//                    tmp = string.Join("\r\n", tmps);
//                }
//                else
//                    tmp = string.Format("{0}：{1}", field.Label, dto.GetDisplay(field.Name));
//                sb[field] = tmp;
//            }
//            return sb;
//        }

//        /// <summary>
//        /// 解析消息体
//        /// </summary>
//        /// <param name="msg"></param>
//        /// <returns></returns>
//        protected Tuple<string[], string[]> Parse(MsgHandler handler)
//        {
//            var meta = handler.Descrip.ParamMetadata;
//            var dto = handler.Message.Body;
//            var tmp = Parse(meta, dto);
//            List<string> document = new List<string>();
//            List<string> summary = new List<string>();
//            foreach (var pair in tmp)
//            {
//                switch (pair.Key.Field_Type)
//                {
//                    case FieldType.file:
//                    case FieldType.image:
//                        document.Add(pair.Value); break;
//                    default:
//                        summary.Add(pair.Value); break;
//                }
//            }
//            return new Tuple<string[], string[]>(summary.ToArray(), document.ToArray());
//        }

//        /// <summary>
//        /// 执行封装
//        /// </summary>
//        public virtual object Execute(MsgHandler handler, Message msg, string msgkey)
//        {
//            var descrip = handler.Descrip;
//            if (descrip.Restrict == RootRestrict.Special)
//                handler.Initial(msg);
//            if (handler.Roots == null || handler.Roots.Count() != 1)
//                throw new Exception2("当前操作有且只能有一个操作对象，请选择一个操作对象再试！");
//            return _Execute(handler, msgkey);
//        }
//        protected virtual object _Execute(MsgHandler handler, string msgkey)
//        {
//            return null;
//        }
//    }
//}
