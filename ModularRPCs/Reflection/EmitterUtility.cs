using DanielWillett.ReflectionTools.Emit;
using System.Diagnostics;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class EmitterUtility
{
    [Conditional("DEBUG")]
    public static void CommentIfDebug(this IOpCodeEmitter emitter, string comment) => emitter.Comment(comment);
}
