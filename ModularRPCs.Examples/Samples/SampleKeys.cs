using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Protocol;

namespace DanielWillett.ModularRpcs.Examples.Samples;
public class SampleKeysNullable : IRpcObject<int?>
{
    int? IRpcObject<int?>.Identifier { get; } = 0;
    public SampleKeysNullable()
    {

    }
}
public class SampleKeysNullableOtherField : IRpcObject<int?>
{
    [RpcIdentifierBackingField]
    private int? _crazyId;

    public int? Identifier => _crazyId;
    public SampleKeysNullableOtherField()
    {
        _crazyId = 1;
    }
}

public class SampleKeysNullableNoField : IRpcObject<int?>
{
    [RpcDontUseBackingField]
    public int? Identifier { get; set; }

    public SampleKeysNullableNoField()
    {
        Identifier = 1;
    }
}

public class SampleKeysString : IRpcObject<string>
{
    public string Identifier { get; set; }

    public SampleKeysString()
    {
        Identifier = "test";
    }
}