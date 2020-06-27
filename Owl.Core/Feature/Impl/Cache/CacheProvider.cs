using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using Owl.Util;
using System.IO;
namespace Owl.Feature.Impl.Cache
{
    internal abstract class CacheProvider : Provider
    {
        /// <summary>
        /// 是否进程内缓存
        /// </summary>
        public abstract bool Inproc { get; }

        public abstract void Add(string key, object value, bool autoremove);

        public abstract void Add(string key, object value, DateTime expire, bool autoremove);

        public abstract void Add(string key, object value, TimeSpan expire, bool autoremove);

        byte[] LoadFile(string filepath)
        {
            if (File.Exists(filepath))
                return File.ReadAllBytes(filepath);
            return null;
        }

        protected byte[] Load(string path)
        {
            if (Directory.Exists(path))
            {
                List<byte[]> bytes = new List<byte[]>();
                foreach (var filepath in Directory.EnumerateFiles(path))
                {
                    var tmp = LoadFile(filepath);
                    if (tmp != null)
                        bytes.Add(tmp);
                }
                return bytes.SelectMany(s => s).ToArray();
            }
            return LoadFile(path);
        }

        public abstract void Add(string key, string fielname, Func<byte[], object> resolver, DateTime expire, bool autoremove);

        public abstract void Add(string key, string fielname, Func<byte[], object> resolver, TimeSpan expire, bool autoremove);

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key"></param>
        public abstract void Remove(string key);
        /// <summary>
        /// 从缓存中获取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract object Get(string key);
    }

    internal class InprocGCacheProvider : CacheProvider
    {
        public override bool Inproc
        {
            get { return true; }
        }
        public override int Priority
        {
            get { return 500; }
        }
        CacheItemPriority GetPriority(bool autoremove)
        {
            return autoremove ? CacheItemPriority.Default : CacheItemPriority.NotRemovable;
        }
        public override void Add(string key, object value, bool autoremove)
        {
            HttpRuntime.Cache.Insert(key, value, null, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, GetPriority(autoremove), null);
        }

        public override void Add(string key, object value, DateTime expire, bool autoremove)
        {
            HttpRuntime.Cache.Insert(key, value, null, expire, System.Web.Caching.Cache.NoSlidingExpiration, GetPriority(autoremove), null);
        }

        public override void Add(string key, object value, TimeSpan expire, bool autoremove)
        {
            HttpRuntime.Cache.Insert(key, value, null, System.Web.Caching.Cache.NoAbsoluteExpiration, expire, GetPriority(autoremove), null);
        }



        public override void Add(string key, string fielname, Func<byte[], object> resolver, DateTime expire, bool autoremove)
        {
            HttpRuntime.Cache.Insert(
                key,
                resolver(Load(fielname)),
                new CacheDependency(fielname),
                expire,
                System.Web.Caching.Cache.NoSlidingExpiration,
                GetPriority(autoremove),
                (s, o, r) =>
                {
                    if (r == CacheItemRemovedReason.Removed)
                        Add(key, fielname, resolver, expire, autoremove);
                }
            );
        }

        public override void Add(string key, string fielname, Func<byte[], object> resolver, TimeSpan expire, bool autoremove)
        {
            HttpRuntime.Cache.Insert(
                key,
                resolver(Load(fielname)),
                new CacheDependency(fielname),
                System.Web.Caching.Cache.NoAbsoluteExpiration,
                expire,
                GetPriority(autoremove),
                (s, o, r) =>
                {
                    if (r == CacheItemRemovedReason.Removed)
                        Add(key, fielname, resolver, expire, autoremove);
                }
            );
        }

        public override void Remove(string key)
        {
            HttpRuntime.Cache.Remove(key);
        }

        public override object Get(string key)
        {
            return HttpRuntime.Cache[key];
        }
    }

    public class JsonCache
    {
        public string Type { get; set; }

        public string Json { get; set; }
    }

    internal class FileCacheProvider : CacheProvider
    {
        public override bool Inproc
        {
            get { return false; }
        }
        Dictionary<string, object> Caches;
        string CacheFilePath = Path.Combine(AppConfig.Section.ResPath, "Cache") + Path.DirectorySeparatorChar;
        public FileCacheProvider()
        {
            Caches = new Dictionary<string, object>();
            if (!Directory.Exists(CacheFilePath))
                Directory.CreateDirectory(CacheFilePath);

            foreach (var file in Directory.EnumerateFiles(CacheFilePath))
            {
                var content = File.ReadAllText(file);
                var jcache = content.DeJson<Dictionary<string, string>>();
                if (jcache.ContainsKey("Type") && jcache.ContainsKey("Json"))
                {
                    var type = Type.GetType(jcache["Type"]);
                    if (type != null)
                        Caches[Path.GetFileNameWithoutExtension(file)] = jcache["Json"].DeJson(type);
                }
            }
        }
        string ReadFile(string filepath)
        {
            using (var reader = new StreamReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return reader.ReadToEnd();
            }
        }
        object GetFromFile(string path, string key)
        {
            var file = Path.Combine(path, key);
            if (File.Exists(file))
            {
                var content = File.ReadAllText(file);
                var jcache = content.DeJson<Dictionary<string, string>>();
                if (jcache.ContainsKey("Type") && jcache.ContainsKey("Json"))
                {
                    var type = Type.GetType(jcache["Type"]);
                    if (type != null)
                        return jcache["Json"].DeJson(type);
                }
            }
            return null;
        }
        static void Persist(string path, string key, object value)
        {
            path = Path.Combine(path, key);
            var jcahce = new Dictionary<string, string>();
            var type = value.GetType();
            jcahce["Type"] = string.Format("{0},{1}", type.FullName, type.Assembly.FullName);
            jcahce["Json"] = value.ToJson();
            File.WriteAllText(path, jcahce.ToJson());
        }
        Action<string, string> RemoveFile = (path, key) =>
        {
            path = Path.Combine(path, key);
            File.Delete(path);
        };
        void PersistCache()
        {

        }
        void AddCache(string key, object value)
        {
            Caches[key] = value;
            TaskMgr.StartTask(Persist, CacheFilePath, key, value);
        }

        void RemoveCache(string key)
        {
            Caches.Remove(key);
            TaskMgr.StartTask(RemoveFile, CacheFilePath, key);
        }


        public override void Add(string key, object value, bool autoremove)
        {
            AddCache(key, value);
        }

        public override void Add(string key, object value, DateTime expire, bool autoremove)
        {
            AddCache(key, value);
        }

        public override void Add(string key, object value, TimeSpan expire, bool autoremove)
        {
            AddCache(key, value);
        }

        public override void Add(string key, string fielname, Func<byte[], object> resolver, DateTime expire, bool autoremove)
        {
            throw new NotImplementedException();
        }

        public override void Add(string key, string fielname, Func<byte[], object> resolver, TimeSpan expire, bool autoremove)
        {
            throw new NotImplementedException();
        }

        public override void Remove(string key)
        {
            RemoveCache(key);
        }

        public override object Get(string key)
        {
            if (Caches.ContainsKey(key))
                return Caches[key];
            return null;
        }

        public override int Priority
        {
            get { return 1; }
        }
    }
}
