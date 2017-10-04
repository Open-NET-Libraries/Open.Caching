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
	public abstract class CachingModifierBase<TPriority> :
		CachePolicyControllerBase<TPriority>, ICachingModifier<TPriority>
	{

		
		public virtual T GetOrAdd<T>(string key, Func<string, T> valueFactory)
		{
			return GetOrAdd(key, () => valueFactory(key));
		}

		public abstract T GetOrAdd<T>(string key, Func<T> valueFactory);

		#region IPolicyController<ICachingModifier<TPriority>,TPriority> Members


		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(TimeSpan elaspsed)
		{
			this.SetAbsolute(elaspsed);
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(int seconds)
		{
			if (seconds < 0) throw new ArgumentOutOfRangeException("seconds");
			this.SetAbsolute(TimeSpan.FromSeconds(seconds));
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(uint seconds)
		{
			this.SetAbsolute(TimeSpan.FromSeconds(seconds));
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(TimeSpan elaspsed)
		{
			this.SetSliding(elaspsed);
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(int seconds)
		{
			if (seconds < 0) throw new ArgumentOutOfRangeException("seconds");
			this.SetSliding(TimeSpan.FromSeconds(seconds));
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(uint seconds)
		{
			this.SetSliding(TimeSpan.FromSeconds(seconds));
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Prioritize(TPriority level)
		{
			this.Priority = level;
			return this;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> SyncTimeoutAfter(TimeSpan timeout)
		{
			this.SyncTimeout = timeout;
			return this;
		}

		#endregion
	}
}
