using DanielWillett.ModularRpcs.Configuration;
using System.Text;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class Utf8Parser(SerializationConfiguration config) : StringParser(config, Encoding.UTF8)
{
    public new class Many(SerializationConfiguration config) : StringParser.Many(config, Encoding.UTF8);
}