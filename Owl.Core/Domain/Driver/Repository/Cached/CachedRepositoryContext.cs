using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Threading;

namespace Owl.Domain.Driver.Repository
{
    internal class CachedRepositoryContext :RepositoryContext
    {
        internal static readonly SyncDictionary<string, SyncDictionary<Guid, AggRoot>> Caches = new SyncDictionary<string, SyncDictionary<Guid, AggRoot>>();
        internal static readonly SyncDictionary<string, Timer> Timers = new SyncDictionary<string, Timer>();
        internal static readonly SyncDictionary<string, IAsyncResult> LoadCompletes = new SyncDictionary<string, IAsyncResult>();
        internal static readonly SyncDictionary<string, DateTime> LastUpdates = new SyncDictionary<string, DateTime>();

        
        public override void Commit()
        {
            foreach (var add in ForAdd.Values)
            {
                Caches[add.Metadata.Name][add.Id] = add;
            }
            foreach (var update in ForUpdate.Values)
            {
                Caches[update.Metadata.Name][update.Id] = update;
            }
            foreach (var remove in ForRemove.Values)
            {
                Caches[remove.Metadata.Name].Remove(remove.Id);
            }
        }

        public override void RollBack()
        {
            
        }
    }
}
