using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Async;

namespace DanielWillett.ModularRpcs.Examples.Samples;
internal class ExampleProxyClass : SampleClass
{
    private readonly ConcurrentDictionary<int, WeakReference> _instances = new ConcurrentDictionary<int, WeakReference>();
    private readonly int _identifier;
    private bool _suppressFinalize = true;
    public ExampleProxyClass()
    {
        _identifier = Identifier;
        _instances.TryAdd(_identifier, new WeakReference(this));
    }
    public ExampleProxyClass(string test1) : base(test1)
    {
        _identifier = Identifier;
        _instances.TryAdd(_identifier, new WeakReference(this));
    }
    public ExampleProxyClass(string test1, bool test2) : base(test1, test2)
    {
        _identifier = Identifier;
        _instances.TryAdd(_identifier, new WeakReference(this));
    }

    internal override RpcTask CallRpcOne()
    {
        // todo;
        return RpcTask.NotImplemented;
    }
    
    ~ExampleProxyClass()
    {
        if (!_suppressFinalize)
            Release();
    }

    public void Release()
    {
        _suppressFinalize = true;
    }
}