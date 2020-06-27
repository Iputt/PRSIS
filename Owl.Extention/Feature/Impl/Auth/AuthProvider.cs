using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;

namespace Owl.Feature.iAuth
{

    public abstract class AuthProvider : Provider
    {
        /// <summary>
        /// 获取对象授权
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public abstract IEnumerable<AuthObject> GetAuth(Guid? actionid, string modelname);
        /// <summary>
        /// 获取对象的字段、消息授权
        /// </summary>
        /// <param name="name">字段或消息的名称</param>
        /// <param name="modelname"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public abstract IEnumerable<AuthObject> GetAuth(Guid? actionid, string name, string modelname, AuthTarget target);
    }

    internal class MetaAuthProvicer : AuthProvider
    {
        public override int Priority
        {
            get { return 1; }
        }

        Dictionary<string, AuthModel> m_authmodels = new Dictionary<string, AuthModel>();
        protected AuthModel GetAuthModel(string modelname)
        {
            AuthModel authmodel = null;
            if (m_authmodels.ContainsKey(modelname))
            {
                authmodel = m_authmodels[modelname];
            }
            else
            {
                var meta = MetaEngine.GetModel(modelname);
                if (meta != null)
                {
                    authmodel = new AuthModel(modelname);
                    m_authmodels[modelname] = authmodel;
                    //如果meta.Authorization 不为空的时候再做处理
                    if (meta.Authorization != null)
                    {
                        foreach (var obj in meta.Authorization)
                            authmodel.Push(obj);
                    }
                    foreach (var field in meta.GetFields())
                    {
                        if (field.AuthObj != null)
                            authmodel.Push(field.AuthObj);
                    }
                }
            }
            return authmodel;
        }

        public override IEnumerable<AuthObject> GetAuth(Guid? actionid, string modelname)
        {
            AuthModel authmodel = GetAuthModel(modelname);
            return authmodel == null ? null : authmodel.General.Values;
        }

        public override IEnumerable<AuthObject> GetAuth(Guid? actionid, string name, string modelname, AuthTarget target)
        {
            List<AuthObject> result = new List<AuthObject>();
            var authmodel = GetAuthModel(modelname);
            if (authmodel != null)
            {
                if (target == AuthTarget.Field)
                {
                    if (authmodel.Fields.ContainsKey(name))
                        result.Add(authmodel.Fields[name]);
                }
                else if (target == AuthTarget.Message)
                {
                    if (authmodel.Messages.ContainsKey(name))
                        result.Add(authmodel.Messages[name]);
                    else
                    {
                        var des = MessageBus.GetDescrip(name, modelname);
                        if (des != null)
                        {
                            if (des.Source == MetaMode.Base)
                                authmodel.Push(des.AuthObj);
                            result.Add(des.AuthObj);
                        }
                    }
                }
            }
            return result;
        }

    }
}
