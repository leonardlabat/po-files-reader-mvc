using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PofilesReader.Localization
{
        public interface ICacheManager
        {
            TResult Get<TKey, TResult>(TKey key, Func<TKey, TResult> acquire);
            ICache<TKey, TResult> GetCache<TKey, TResult>();
        }   
}
