using System;
using Owl.Util;
using System.Collections.Generic;
using Owl.Domain;
namespace Owl.Feature
{
    public class ChangeName
    {
        public string OrgName { get; private set; }

        public string DestName { get; private set; }

        public ChangeName(string orgname, string destname)
        {
            if (string.IsNullOrEmpty(orgname))
                throw new ArgumentNullException("orgname");
            if (string.IsNullOrEmpty(destname))
                throw new ArgumentNullException("destname");
            OrgName = orgname.ToLower();
            DestName = destname.ToLower();
        }
    }
}

namespace Owl.Feature.iModule
{


    public abstract class ModuleProvider : Provider
    {
        /// <summary>
        /// ��װģ��
        /// </summary>
        public abstract void Install();

        /// <summary>
        /// �ı��������
        /// </summary>
        /// <param name="alters"></param>
        public abstract void ChangeName(IEnumerable<ChangeName> alters);

        /// <summary>
        /// �ı��ɫ����
        /// </summary>
        /// <param name="orgrole"></param>
        /// <param name="destrole"></param>
        /// <param name="namechanged"></param>
        public abstract void ChangeRole(string orgrole, string destrole, bool namechanged);

        /// <summary>
        /// ��������Ա�˺�
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public abstract void CreageAdmin(string username, string password);

        /// <summary>
        /// ��ȡģ�����ݰ汾��
        /// </summary>
        /// <returns></returns>
        public abstract IDictionary<string, string> GetVersions();

        /// <summary>
        /// ����ģ������ݰ汾��
        /// </summary>
        /// <param name="versions"></param>
        public abstract void SetVersions(IDictionary<string, string> versions);
    }
}

