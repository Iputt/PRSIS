using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Feature.Impl.Cache
{
    public abstract class OuterCacheProvider : Provider, ICache
    {

        public abstract bool KeyExists(string key);

        public abstract void KeyRemove(string key);

        public abstract void KeyExpire(string key, TimeSpan? expire);

        public abstract void Set(string key, object value, TimeSpan? expire = null);

        public abstract bool SetNE(string key, object value, TimeSpan? expire = null);

        public abstract object Get(string key);

        public abstract object GetSet(string key, object value);

        public abstract void HashSet(string key, string field, object value);

        public abstract object HashGet(string key, string field);

        public abstract void HashDelete(string key, string field, bool async);

        public abstract Hashtable HashGetAll(string key);

        public abstract long Increment(string key, long value = 1);

        public abstract void ListLeftPush(string key, params object[] value);

        public abstract object ListLeftPop(string key);

        public abstract void ListRightPush(string key, params object[] value);

        public abstract object ListRightPop(string key);

        public abstract IEnumerable<object> ListRange(string key, int start = 0, int end = -1);
    }

    public class OuterCache : Engine<OuterCacheProvider, OuterCache>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
    }
}
