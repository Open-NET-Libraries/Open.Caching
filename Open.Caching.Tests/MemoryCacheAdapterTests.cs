using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.Caching.Tests;

public class MemoryCacheAdapterTests
{
	private readonly MemoryCacheAdapter Cache = new(new MemoryCache(new MemoryCacheOptions()));
}
