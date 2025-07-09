using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DanielWillett.ModularRpcs.Reflection;
using Microsoft.CodeAnalysis;

namespace ModularRPCs.Util;

public static class TypeHelper
{
    public static bool CanBeGenericArgument(this ITypeSymbol symbol)
    {
        if (symbol == null)
            return false;

        if (symbol.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer
            || symbol.SpecialType is SpecialType.System_TypedReference or SpecialType.System_ArgIterator or SpecialType.System_RuntimeArgumentHandle or SpecialType.System_Void
            || symbol.IsStatic)
        {
            return false;
        }

        return !symbol.IsRefLikeType;
    }

    private static readonly string?[] PrimitiveTypes =
    [
        "global::System.Boolean",
        "global::System.Byte",
        "global::System.Char",
        "global::System.Double",
        null,
        "global::System.Int16",
        "global::System.Int32",
        "global::System.Int64",
        null,
        "global::System.IntPtr",
        "global::System.SByte",
        "global::System.Single",
        "global::System.UInt16",
        "global::System.UInt32",
        "global::System.UInt64",
        null,
        "global::System.UIntPtr"
    ];

    private static readonly string[] PrimitiveLikeTypes =
    [
        "global::System.Boolean",
        "global::System.Byte",
        "global::System.Char",
        "global::System.Double",
        "global::System.Half",
        "global::System.Int16",
        "global::System.Int32",
        "global::System.Int64",
        "global::System.Int128",
        "global::System.IntPtr",
        "global::System.SByte",
        "global::System.Single",
        "global::System.UInt16",
        "global::System.UInt32",
        "global::System.UInt64",
        "global::System.UInt128",
        "global::System.UIntPtr",

        "global::System.DateTime",
        "global::System.DateTimeOffset",
        "global::System.Decimal",
        "global::System.Guid",
        "global::System.TimeSpan",
    ];
    public static string ToTypeCodeString(this TypeCode tc)
    {
        return tc switch
        {
            TypeUtility.TypeCodeTimeSpan => "TimeSpan",
            TypeUtility.TypeCodeGuid => "Guid",
            TypeUtility.TypeCodeDateTimeOffset => "DateTimeOffset",
            TypeUtility.TypeCodeIntPtr => "IntPtr",
            TypeUtility.TypeCodeUIntPtr => "UIntPtr",
            _ => tc.ToString()
        };
    }

    [Flags]
    public enum PrimitiveLikeType
    {
        None,
        Boolean,
        Byte,
        Char,
        Double,
        Half,
        Int16,
        Int32,
        Int64,
        Int128,
        IntPtr,
        SByte,
        Single,
        UInt16,
        UInt32,
        UInt64,
        UInt128,
        UIntPtr,

        // sorta primtive
        DateTime,
        DateTimeOffset,
        Decimal,
        Guid,
        TimeSpan,

        UnderlyingTypeMask = 255,
        Enum = 256
    }


    public static PrimitiveLikeType GetPrimitiveType(ITypeSymbol? type)
    {
        if (type == null)
            return PrimitiveLikeType.None;

        if (type is INamedTypeSymbol { EnumUnderlyingType: { } underlying })
        {
            return underlying.EnumUnderlyingType == null ? GetPrimitiveLikeType(underlying) | PrimitiveLikeType.Enum : PrimitiveLikeType.None;
        }

        string name = FixNativeIntName(type.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat), type);
        int index = Array.IndexOf(PrimitiveTypes, name);
        if (index < 0)
            return PrimitiveLikeType.None;

        return (PrimitiveLikeType)(index + 1);
    }

    public static PrimitiveLikeType GetPrimitiveLikeType(ITypeSymbol? type)
    {
        if (type == null)
            return PrimitiveLikeType.None;

        if (type is INamedTypeSymbol { EnumUnderlyingType: { } underlying })
        {
            return underlying.EnumUnderlyingType == null ? GetPrimitiveLikeType(underlying) | PrimitiveLikeType.Enum : PrimitiveLikeType.None;
        }

        string name = FixNativeIntName(type.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat), type);
        int index = Array.IndexOf(PrimitiveLikeTypes, name);
        if (index < 0)
            return PrimitiveLikeType.None;

        return (PrimitiveLikeType)(index + 1);
    }

    public static int GetPrimitiveLikeSize(PrimitiveLikeType type)
    {
        switch (type & PrimitiveLikeType.UnderlyingTypeMask)
        {
            case PrimitiveLikeType.Boolean:
            case PrimitiveLikeType.Byte:
            case PrimitiveLikeType.SByte:
                return 1;

            case PrimitiveLikeType.Int16:
            case PrimitiveLikeType.UInt16:
            case PrimitiveLikeType.Char:
            case PrimitiveLikeType.Half:
                return 2;

            case PrimitiveLikeType.Int32:
            case PrimitiveLikeType.UInt32:
            case PrimitiveLikeType.Single:
                return 4;

            case PrimitiveLikeType.Int64:
            case PrimitiveLikeType.UInt64:
            case PrimitiveLikeType.Double:
            case PrimitiveLikeType.IntPtr:
            case PrimitiveLikeType.UIntPtr:
            case PrimitiveLikeType.DateTime:
            case PrimitiveLikeType.TimeSpan:
                return 8;

            case PrimitiveLikeType.DateTimeOffset:
                return 10;

            case PrimitiveLikeType.Int128:
            case PrimitiveLikeType.UInt128:
            case PrimitiveLikeType.Guid:
            case PrimitiveLikeType.Decimal:
                return 16;

            default:
                return 0;
        }
    }

    /// <summary>
    /// If this type can be directly written to the buffer using pointers.
    /// </summary>
    /// <remarks>Source generated code needs to check <see cref="BitConverter.IsLittleEndian"/> and <see cref="IntPtr.Size"/>.</remarks>
    public static QuickSerializeMode CanQuickSerializeType(ITypeSymbol? type)
    {
        if (type == null)
            return QuickSerializeMode.Never;

        string name = FixNativeIntName(type.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat), type);
        if (string.Equals(name, "global::System.Byte", StringComparison.Ordinal)
            || string.Equals(name, "global::System.SByte", StringComparison.Ordinal)
            || string.Equals(name, "global::System.Boolean", StringComparison.Ordinal))
        {
            return QuickSerializeMode.Always;
        }

        if (string.Equals(name, "global::System.IntPtr", StringComparison.Ordinal)
            || string.Equals(name, "global::System.UIntPtr", StringComparison.Ordinal))
        {
            return QuickSerializeMode.If64Bit;
        }

        if (type is INamedTypeSymbol { EnumUnderlyingType: { } underlying })
        {
            string underlyingName = FixNativeIntName(underlying.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat), underlying);

            if (string.Equals(underlyingName, "global::System.Byte", StringComparison.Ordinal)
                || string.Equals(underlyingName, "global::System.SByte", StringComparison.Ordinal)
                || string.Equals(name, "global::System.Boolean", StringComparison.Ordinal))
            {
                return QuickSerializeMode.Always;
            }

            if (string.Equals(underlyingName, "global::System.IntPtr", StringComparison.Ordinal)
                || string.Equals(underlyingName, "global::System.UIntPtr", StringComparison.Ordinal))
            {
                return QuickSerializeMode.If64Bit;
            }
        }

        return GetPrimitiveType(type) == PrimitiveLikeType.None
            ? QuickSerializeMode.Never
            : QuickSerializeMode.IfLittleEndian;
    }

    public static string FixNativeIntName(string dispString, ITypeSymbol type)
    {
        // for some reason it ignores keyword preferences for these two types
        if (dispString.Equals("nint", StringComparison.Ordinal) && type is { Name: "IntPtr", ContainingNamespace.Name: "System" })
        {
            dispString = "global::System.IntPtr";
        }
        else if (dispString.Equals("nuint", StringComparison.Ordinal) && type is { Name: "UIntPtr", ContainingNamespace.Name: "System" })
        {
            dispString = "global::System.UIntPtr";
        }

        return dispString;
    }

    [Flags]
    public enum QuickSerializeMode
    {
        Never,
        If64Bit = 1,
        IfLittleEndian = 2,
        If64BitLittleEndian = If64Bit | IfLittleEndian,
        Always = 4
    }

    private static readonly string[] TypeCodes =
    [
        "global::System.DBNull",
        "global::System.Boolean",
        "global::System.Char",
        "global::System.SByte",
        "global::System.Byte",
        "global::System.Int16",
        "global::System.UInt16",
        "global::System.Int32",
        "global::System.UInt32",
        "global::System.Int64",
        "global::System.UInt64",
        "global::System.Single",
        "global::System.Double",
        "global::System.Decimal",
        "global::System.DateTime",
        "global::System.TimeSpan",
        "global::System.String",
        "global::System.Guid",
        "global::System.DateTimeOffset",
        "global::System.IntPtr",
        "global::System.UIntPtr"
    ];

    public static TypeCode GetTypeCode(ITypeSymbol? idType)
    {
        if (idType == null) return TypeCode.Empty;

        while (true)
        {
            if (idType is INamedTypeSymbol { EnumUnderlyingType: { } underlying })
            {
                idType = underlying;
                continue;
            }

            string name = FixNativeIntName(idType.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat), idType);

            int index = Array.IndexOf(TypeCodes, name);
            if (index < 0)
                return TypeCode.Object;

            return (TypeCode)(index + 2);
        }
    }

    private static string GetMetadataNameStr(Compilation compilation, ITypeSymbol type, bool byRef)
    {
        if (type.ContainingType != null)
        {
            return GetMetadataNameSlow(compilation, type, byRef);
        }

        string metaName = EscapeAssemblyQualifiedName(type.MetadataName);
        IAssemblySymbol asm = GetAssembly(type);

        if (type.ContainingNamespace != null)
        {
            string ns = EscapeAssemblyQualifiedName(type.ContainingNamespace.ToDisplayString(CustomFormats.FullTypeNameFormat));
            metaName = ns + "." + metaName;
        }

        if (byRef)
            metaName += "&";
        
        if (IsAssemblyMscorlib(compilation, type))
        {
            return metaName;
        }

        string asmName = EscapeAssemblyQualifiedName(asm.Name);
        return metaName + ", " + asmName;
    }

    public static string GetAssemblyQualifiedNameNoVersion(Compilation compilation, ITypeSymbol type, bool byRef = false)
    {
        if (type is not INamedTypeSymbol n || !n.IsGenericType || n.IsUnboundGenericType)
            return GetMetadataNameStr(compilation, type, byRef);

        if (n.TypeArguments.Any(x => x is IErrorTypeSymbol))
            return GetMetadataNameStr(compilation, n, byRef);

        return GetMetadataNameSlow(compilation, type, byRef);
    }

    private static string GetMetadataNameSlow(Compilation compilation, ITypeSymbol type, bool byRef)
    {
        StringBuilder sb = new StringBuilder(32 * (((type as INamedTypeSymbol)?.TypeArguments.Length ?? 0) + 1));
        GetMetadataName(compilation, type, sb, byRef: byRef);
        return sb.ToString();
    }

    private static readonly string[] MscorlibAssemblies =
    [
        "mscorlib", "System.Private.CoreLib"
    ];

    private static readonly string[] MaybeMscorlibAssemblies =
    [
        "netstandard", "System.Runtime"
    ];

    public static IAssemblySymbol GetAssembly(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return array.ElementType.ContainingAssembly;

            case IPointerTypeSymbol ptr:
                return ptr.PointedAtType.ContainingAssembly;

            default:
                return type.ContainingAssembly;
        }
    }

    private static bool IsAssemblyMscorlib(Compilation compilation, ITypeSymbol type)
    {
        type = type.OriginalDefinition;
        IAssemblySymbol asm = GetAssembly(type);
        if (Array.IndexOf(MscorlibAssemblies, asm.Name) >= 0)
            return true;

        if (Array.IndexOf(MaybeMscorlibAssemblies, asm.Name) < 0)
            return false;

        if (type is INamedTypeSymbol { IsGenericType: true } n)
            type = n.ConstructedFrom;

        while (type.ContainingType != null)
            type = type.ContainingType;

        string metaName = type.MetadataName;
        if (type.ContainingNamespace != null)
            metaName = type.ContainingNamespace.ToDisplayString(CustomFormats.FullTypeNameFormat) + "." + metaName;

        INamedTypeSymbol? foundType = compilation.GetTypeByMetadataName(metaName);
        return foundType != null;
    }

    private static void GetMetadataName(Compilation compilation, ITypeSymbol type, StringBuilder bldr, bool nested = false, bool byRef = false)
    {
        ITypeSymbol elementType = type;
        while (true)
        {
            if (elementType is IArrayTypeSymbol arr)
                elementType = arr.ElementType;
            else if (elementType is IPointerTypeSymbol ptr)
                elementType = ptr.PointedAtType;
            else
                break;
        }

        if (!nested && elementType.ContainingNamespace != null)
        {
            bldr.Append(EscapeAssemblyQualifiedName(elementType.ContainingNamespace.ToDisplayString(CustomFormats.FullTypeNameFormat)))
                .Append('.');
        }
        
        if (elementType.ContainingType != null)
        {
            GetMetadataName(compilation, elementType.ContainingType, bldr, nested: true);
            bldr.Append('+');
        }
        
        bldr.Append(EscapeAssemblyQualifiedName(elementType.MetadataName));

        if (nested)
            return;

        if (elementType is INamedTypeSymbol n && n.IsGenericType && !n.IsUnboundGenericType)
        {
            ImmutableArray<ITypeSymbol> typeArgs = n.TypeArguments;
            if (!typeArgs.Any(x => x is IErrorTypeSymbol))
            {
                bldr.Append('[');

                int ct = 0;
                // nested types
                if (elementType.ContainingType != null)
                {
                    Stack<INamedTypeSymbol> stack = new Stack<INamedTypeSymbol>(4);
                    for (INamedTypeSymbol c = elementType.ContainingType; c != null; c = c.ContainingType)
                        stack.Push(c);

                    while (stack.Count > 0)
                    {
                        INamedTypeSymbol nestedType = stack.Pop();

                        ImmutableArray<ITypeSymbol> nestedTypeArgs = nestedType.TypeArguments;

                        foreach (ITypeSymbol s in nestedTypeArgs)
                        {
                            if (ct != 0)
                                bldr.Append(',');

                            bldr.Append('[');
                            GetMetadataName(compilation, s, bldr);
                            bldr.Append(']');
                            ++ct;
                        }
                    }
                }

                foreach (ITypeSymbol s in typeArgs)
                {
                    if (ct != 0)
                        bldr.Append(',');

                    bldr.Append('[');
                    GetMetadataName(compilation, s, bldr);
                    bldr.Append(']');
                    ++ct;
                }

                bldr.Append(']');
            }
        }

        if (byRef)
            bldr.Append('&');

        elementType = type;
        while (true)
        {
            if (elementType is IArrayTypeSymbol arr)
            {
                switch (arr.Rank)
                {
                    case 0:
                        break;

                    case 1:
                        if (arr.LowerBounds.IsDefaultOrEmpty || arr.LowerBounds.Length == 1 && arr.LowerBounds[0] == 0)
                            bldr.Append("[]");
                        else
                            bldr.Append("[*]");
                        break;

                    default:
                        bldr.Append('[').Append(',', arr.Rank - 1).Append(']');
                        break;
                }

                elementType = arr.ElementType;
            }
            else if (elementType is IPointerTypeSymbol ptr)
            {
                bldr.Append('*');
                elementType = ptr.PointedAtType;
            }
            else
                break;
        }

        if (IsAssemblyMscorlib(compilation, elementType) || elementType.ContainingAssembly == null)
            return;

        bldr.Append(", ")
            .Append(EscapeAssemblyQualifiedName(elementType.ContainingAssembly.Name));
    }

    private static readonly char[] Escapables = [ '\n', '\r', '\t', '\v', '\\', '\"', '\'' ];
    private static readonly char[] AssemblyQualifiedEscapables = [ '\n', '\r', '\t', '\v', '\\', ',', '+', '[', ']', '*', '&' ];
    public static string Escape(string value)
    {
        int c = 0;
        string s = value;
        for (int i = 0; i < s.Length; ++i)
        {
            if (s[i] is <= '\r' and ('\n' or '\r' or '\t' or '\v') or '\\' or '\"' or '\'')
                ++c;
        }

        if (c <= 0)
        {
            return s;
        }

        unsafe
        {
            char* newValue = stackalloc char[s.Length + c];

            int prevIndex = -1;
            int writeIndex = 0;
            while (true)
            {
                int index = s.IndexOfAny(Escapables, prevIndex + 1);
                if (index == -1)
                {
                    for (int i = prevIndex + 1; i < s.Length; ++i)
                    {
                        newValue[writeIndex] = s[i];
                        ++writeIndex;
                    }
                    break;
                }

                for (int i = prevIndex + 1; i < index; ++i)
                {
                    newValue[writeIndex] = s[i];
                    ++writeIndex;
                }

                char self = s[index];
                newValue[writeIndex] = '\\';
                newValue[writeIndex + 1] = self switch
                {
                    '\n' => 'n',
                    '\r' => 'r',
                    '\t' => 't',
                    '\v' => 'v',
                    _ => self
                };

                writeIndex += 2;

                prevIndex = index;
            }

            return new string(newValue, 0, writeIndex);
        }
    }
    
    private static string EscapeAssemblyQualifiedName(string value)
    {
        int c = 0;
        string s = value;
        for (int i = 0; i < s.Length; ++i)
        {
            if (s[i] is <= '\r' and ('\n' or '\r' or '\t' or '\v') or '\\' or ',' or '+' or '[' or ']' or '*' or '&')
                ++c;
        }

        if (c <= 0)
        {
            return s;
        }

        unsafe
        {
            char* newValue = stackalloc char[s.Length + c];

            int prevIndex = -1;
            int writeIndex = 0;
            while (true)
            {
                int index = s.IndexOfAny(AssemblyQualifiedEscapables, prevIndex + 1);
                if (index == -1)
                {
                    for (int i = prevIndex + 1; i < s.Length; ++i)
                    {
                        newValue[writeIndex] = s[i];
                        ++writeIndex;
                    }
                    break;
                }

                for (int i = prevIndex + 1; i < index; ++i)
                {
                    newValue[writeIndex] = s[i];
                    ++writeIndex;
                }

                char self = s[index];
                newValue[writeIndex] = '\\';
                newValue[writeIndex + 1] = self switch
                {
                    '\n' => 'n',
                    '\r' => 'r',
                    '\t' => 't',
                    '\v' => 'v',
                    _ => self
                };

                writeIndex += 2;

                prevIndex = index;
            }

            return new string(newValue, 0, writeIndex);
        }
    }


    /// <summary>
    /// Compare two assembly-qualified type names.
    /// </summary>
    /// <remarks>Stolen from <see href="https://github.com/DanielWillett/unturned-asset-file-vscode/blob/master/UnturnedAssetSpec/QualifiedType.cs"/>.</remarks>
    public static bool TypesEqual(ReadOnlySpan<char> left, ReadOnlySpan<char> right, bool caseInsensitive = false)
    {
        StringComparison c = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (left.Equals(right, c))
            return true;

        if (left.IsEmpty || right.IsEmpty)
            return false;

        if (!ExtractParts(left, out ReadOnlySpan<char> typeNameLeft, out ReadOnlySpan<char> assemblyNameLeft)
            || !ExtractParts(right, out ReadOnlySpan<char> typeNameRight, out ReadOnlySpan<char> assemblyNameRight))
        {
            return false;
        }

        return typeNameLeft.Equals(typeNameRight, c) && assemblyNameLeft.Equals(assemblyNameRight, c);
    }


    /// <summary>
    /// Extract the type name and assembly name from an assembly-qualified full type name.
    /// </summary>
    public static bool ExtractParts(ReadOnlySpan<char> assemblyQualifiedTypeName, out ReadOnlySpan<char> fullTypeName, out ReadOnlySpan<char> assemblyName)
    {
        bool isLookingForAssemblyName = false;
        if (assemblyQualifiedTypeName.IndexOfAny('[', ']') != -1)
        {
            fullTypeName = default;
            assemblyName = default;

            int genericDepth = 0;
            int escapeDepth = 0;

            // more advanced check taking generic types into consideration
            // ex: System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]], mscorlib
            for (int i = 0; i < assemblyQualifiedTypeName.Length; ++i)
            {
                char c = assemblyQualifiedTypeName[i];

                if (c is '[' or ']')
                {
                    if (escapeDepth > 0 && escapeDepth % 2 == 1)
                    {
                        escapeDepth = 0;
                        continue;
                    }

                    escapeDepth = 0;
                    if (!fullTypeName.IsEmpty)
                    {
                        fullTypeName = default;
                        return false;
                    }

                    if (c == '[')
                    {
                        ++genericDepth;
                        continue;
                    }

                    --genericDepth;
                    if (genericDepth < 0)
                    {
                        fullTypeName = default;
                        assemblyName = default;
                        return false;
                    }
                }

                if (c == '\\')
                {
                    ++escapeDepth;
                    continue;
                }

                if (genericDepth != 0 || c != ',')
                {
                    escapeDepth = 0;
                    continue;
                }

                if (escapeDepth > 0 && escapeDepth % 2 == 1)
                {
                    escapeDepth = 0;
                    continue;
                }

                escapeDepth = 0;

                if (isLookingForAssemblyName)
                {
                    assemblyName = assemblyName.Slice(0, i).TrimEnd();
                    return !assemblyName.IsEmpty;
                }

                fullTypeName = assemblyQualifiedTypeName.Slice(0, i).Trim();
                if (fullTypeName.IsEmpty)
                    return false;
                assemblyQualifiedTypeName = assemblyQualifiedTypeName.Slice(i + 1).Trim();
                assemblyName = assemblyQualifiedTypeName;
                isLookingForAssemblyName = true;
            }

            if (genericDepth > 0)
            {
                fullTypeName = default;
                assemblyName = default;
            }
        }
        else
        {
            fullTypeName = default;
            assemblyName = default;
            if (assemblyQualifiedTypeName.Length == 0)
                return false;

            int lastIndex = -1;
            while (true)
            {
                ++lastIndex;
                if (lastIndex >= assemblyQualifiedTypeName.Length)
                    break;
                int nextInd = assemblyQualifiedTypeName.Slice(lastIndex).IndexOf(',');
                if (nextInd <= 0)
                    break;

                nextInd += lastIndex;

                if (IsEscaped(assemblyQualifiedTypeName, nextInd))
                {
                    lastIndex = nextInd;
                    continue;
                }

                if (isLookingForAssemblyName)
                {
                    assemblyName = assemblyName.Slice(0, nextInd).TrimEnd();
                    return !assemblyName.IsEmpty;
                }

                fullTypeName = assemblyQualifiedTypeName.Slice(0, nextInd).Trim();
                if (fullTypeName.IsEmpty)
                    return false;
                assemblyQualifiedTypeName = assemblyQualifiedTypeName.Slice(nextInd + 1).Trim();
                assemblyName = assemblyQualifiedTypeName;
                isLookingForAssemblyName = true;
                lastIndex = 0;
            }
        }

        return !assemblyName.IsEmpty;
    }

    private static bool IsEscaped(ReadOnlySpan<char> text, int index)
    {
        if (index == 0)
            return false;

        int slashCt = 0;
        while (index > 0 && text[index - 1] == '\\')
        {
            ++slashCt;
            --index;
        }

        return slashCt % 2 == 1;
    }
}
