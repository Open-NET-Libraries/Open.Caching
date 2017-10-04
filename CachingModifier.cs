/* The MIT License (MIT)

Copyright (c) 2015 Oren J. Ferrari

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.Caching
{
	public sealed class CachingModifier<TPriority> :
		CachingModifierBase<TPriority>
	{

		internal CachingModifier(ICache<TPriority> cache, bool importCacheDefaults = true) : base () {
			Cache = cache;
			if (importCacheDefaults)
			{
				this.Mode = cache.Mode;
				this.ExpiresAfter = cache.ExpiresAfter;
				this.Priority = cache.Priority;
				this.SyncTimeout = cache.SyncTimeout;
			}
		}

		public ICache<TPriority> Cache
		{
			get;
			private set;
		}

		#region ICachingModifier<TPriority> Members
		
		public override object this[string key]
		{
			get
			{
				return Cache[key];
			}
			set
			{
				Cache[key] = value;
			}
		}

		#endregion

		#region IGetOrAdd Members

		public override T GetOrAdd<T>(string key, Func<T> valueFactory)
		{
			return Cache.GetOrAdd<T>(key, Mode, ExpiresAfter, Priority, valueFactory);
		}

		#endregion
	}
}
