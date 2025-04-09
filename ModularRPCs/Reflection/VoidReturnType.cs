using System.ComponentModel;
using DanielWillett.ModularRpcs.Routing;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Used to invoke <see cref="IRpcRouter.HandleInvokeReturnValue{TReturnType}"/> when the receiver returns <see langword="void"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public struct VoidReturnType;