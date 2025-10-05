using Microsoft.CodeAnalysis;

namespace ModularRPCs.Util;

public static class CustomFormats
{
    public static readonly SymbolDisplayFormat TypeDeclarationFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        localOptions: SymbolDisplayLocalOptions.IncludeType
#if ROSLYN_4_5_OR_GREATER
                        | SymbolDisplayLocalOptions.IncludeModifiers
#endif
        ,
        kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat MethodDeclarationFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeConstantValue | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeExtensionThis
#if ROSLYN_4_5_OR_GREATER
                                                                    | SymbolDisplayParameterOptions.IncludeModifiers
#endif
                                                                    | SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        localOptions: SymbolDisplayLocalOptions.IncludeType
#if ROSLYN_4_5_OR_GREATER
                        | SymbolDisplayLocalOptions.IncludeModifiers
#endif
        ,
        kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable
#if ROSLYN_4_6_OR_GREATER
                                | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat MethodDisplayFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.IncludeExtensionThis
#if ROSLYN_4_5_OR_GREATER
                          | SymbolDisplayParameterOptions.IncludeModifiers
#endif
                          | SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        localOptions: SymbolDisplayLocalOptions.IncludeType
#if ROSLYN_4_5_OR_GREATER
                        | SymbolDisplayLocalOptions.IncludeModifiers
#endif
        ,
        kindOptions: SymbolDisplayKindOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat XmlDocsFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef | SymbolDisplayMemberOptions.IncludeParameters,
        delegateStyle: SymbolDisplayDelegateStyle.NameAndParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
#if ROSLYN_4_5_OR_GREATER
                        | SymbolDisplayParameterOptions.IncludeModifiers
#endif
        ,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions:  SymbolDisplayKindOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat NamespaceDeclarationFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat FullTypeNameFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.None,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions: SymbolDisplayKindOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.ExpandNullable
#if ROSLYN_4_6_OR_GREATER
                                | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat FullTypeNameWithGlobalFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.None,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions: SymbolDisplayKindOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.ExpandNullable
#if ROSLYN_4_6_OR_GREATER
                                | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat NamespaceWithoutGlobalFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeRef | SymbolDisplayMemberOptions.IncludeContainingType,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions: SymbolDisplayKindOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static string InsertPartial(string toDisplayString)
    {
        int firstSpace = toDisplayString.IndexOf(' ');
        if (firstSpace == -1)
            return "partial " + toDisplayString;

        return $"{toDisplayString.Substring(0, firstSpace)} partial{toDisplayString.Substring(firstSpace)}";
    }

    public static string GetXmlDocReference(ISymbol symbol)
    {
        return symbol.ToDisplayString(XmlDocsFormat).Replace('<', '{').Replace('>', '}');
    }
}
