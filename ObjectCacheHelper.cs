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
using System.Runtime.Caching;

namespace Open.Caching
{
	/// <summary>
	/// An synchronized wrapper for use with MemoryCache or any ObjectCache derived class.
	/// </summary>
	public class ObjectCacheHelper : CacheBase<CacheItemPriority>
	{

		readonly ObjectCache _cache;

		public ObjectCacheHelper(ObjectCache cache,
			TimeSpan? defaultExpiration = null,
			ExpirationMode mode = ExpirationMode.Absolute) : base()
		{
			_cache = cache;
			ExpiresAfter = defaultExpiration ?? TimeSpan.Zero;
			Mode = mode;
		}

		public override T GetOrAdd<T>(string key, ExpirationMode mode, TimeSpan elapsed, CacheItemPriority priority, Func<T> valueFactory)
		{
			return _cache.GetOrAdd(key, mode, elapsed, priority, valueFactory);
		}

		public override object this[string key]
		{
			get
			{
				return _cache[key];
			}
			set
			{
				_cache[key] = value;
			}
		}

		
	}
}
