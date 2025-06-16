using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Linq;
using System.Reflection;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class RpcObjectHelper
{
    private static readonly string[] IdentifierFieldNamesToSearch =
    [
        "_identifier",
        "m_identifier",
        "identifier",
        "_id",
        "id",
        "m_id"
    ];

    internal static void GetIdentifierLocation(
        Type interfaceType,
        Type idType,
        Type type,
        bool typeGivesInternalAccess,
        bool isIdNullable,
        Type? elementType,
        IRefSafeLoggable? loggable,
        out PropertyInfo identifierProperty,
        out FieldInfo? identifierBackingField
    )
    {
        PropertyInfo intxIdProperty = interfaceType.GetProperty(nameof(IRpcObject<int>.Identifier), BindingFlags.Public | BindingFlags.Instance)
                                          ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(IRpcObject<int>.Identifier))
                                              .DeclaredIn(interfaceType, isStatic: false)
                                              .WithPropertyType(idType)
                                              .WithNoSetter()
                                          );

        PropertyInfo? property = TypeUtility.GetImplementedProperty(type, intxIdProperty);

        identifierBackingField = null;

        bool backingFieldIsExplicit = false;

        // try to identify the backing field for the property, if it exists.
        // this is not necessary but can reduce data copying by referencing the address of the field instead of getting from property
        if ((property == null || !property.IsDefinedSafe<RpcDontUseBackingFieldAttribute>()) && intxIdProperty.DeclaringType is { IsInterface: false })
        {
            // [RpcIdentifierBackingField]
            FieldInfo[] fields = intxIdProperty.DeclaringType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            identifierBackingField = fields.FirstOrDefault(field => field.IsDefinedSafe<RpcIdentifierBackingFieldAttribute>());
            backingFieldIsExplicit = true;
            if (identifierBackingField != null && (identifierBackingField.IsStatic || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored()))
            {
                loggable?.LogWarning(string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                identifierBackingField = null;
            }

            if (identifierBackingField == null)
            {
                backingFieldIsExplicit = false;

                // public int Identifier { get; set; }
                identifierBackingField = fields.FirstOrDefault(x => x.Name.Equals("<Identifier>k__BackingField", StringComparison.Ordinal));

                if (identifierBackingField == null || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored())
                {
                    // int IRpcObject<int>.Identifier { get; set; }
                    string explName = "<DanielWillett.ModularRpcs.Protocol.IRpcObject<" + (isIdNullable ? elementType! : idType) + (isIdNullable ? "?" : string.Empty) + ">.Identifier>k__BackingField";
                    identifierBackingField = intxIdProperty.DeclaringType.GetField(explName, BindingFlags.NonPublic | BindingFlags.Instance);

                    if (identifierBackingField == null || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored())
                    {
                        // predefined field names
                        identifierBackingField = null;
                        for (int i = 0; i < IdentifierFieldNamesToSearch.Length; ++i)
                        {
                            identifierBackingField = intxIdProperty.DeclaringType.GetField("_identifier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

                            if (identifierBackingField != null && identifierBackingField.FieldType == idType && !identifierBackingField.IsIgnored())
                                break;

                            identifierBackingField = null;
                        }
                    }
                }
            }

            if (identifierBackingField == null)
            {
                loggable?.LogDebug(string.Format(Properties.Logging.BackingFieldNotFound, Accessor.Formatter.Format(type)));
            }
        }

        if (identifierBackingField != null && identifierBackingField.IsDefinedSafe<RpcDontUseBackingFieldAttribute>())
            identifierBackingField = null;

        if (identifierBackingField != null && Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
        {
            MemberVisibility vis = identifierBackingField.GetVisibility();
            if (!VisibilityUtility.IsAccessibleFromParentType(vis, typeGivesInternalAccess))
            {
                identifierBackingField = null;
                if (backingFieldIsExplicit)
                {
                    loggable?.LogWarning(string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                }
            }
        }

        identifierProperty = property ?? intxIdProperty;
    }
}
