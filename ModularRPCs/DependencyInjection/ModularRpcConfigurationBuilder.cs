using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DanielWillett.ModularRpcs.DependencyInjection;
internal class ModularRpcConfigurationBuilder(IServiceCollection collection) : IModularRpcConfigurationBuilder
{
    private readonly IServiceCollection _collection = collection;
}
