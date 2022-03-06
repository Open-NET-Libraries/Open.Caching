using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Caching;

public abstract class CacheExpirationPolicyBase<TKey>
	: ICachePolicyProvider<TKey, ExpirationPolicy>
{
	public abstract ICacheAdapter<TKey> Policy(ExpirationPolicy policy);
}
