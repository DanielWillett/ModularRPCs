extern alias JetBrains;
using System;
using System.Collections.Generic;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization;
internal static class UnitySerializationRegistrationHelper
{
    [JetBrains::JetBrains.Annotations.UsedImplicitly]
#pragma warning disable IDE0051
    private static void ApplyUnityParsers(Dictionary<Type, IBinaryTypeParser> primitiveParsers, Dictionary<Type, int> primitiveSizes, SerializationConfiguration config)
    {
        primitiveSizes.Add(typeof(Vector2), 8);
        primitiveSizes.Add(typeof(Vector3), 12);
        primitiveSizes.Add(typeof(Vector4), 16);
        primitiveSizes.Add(typeof(Quaternion), 16);
        primitiveSizes.Add(typeof(Color), 16);
        primitiveSizes.Add(typeof(Color32), 4);
        primitiveSizes.Add(typeof(Bounds), 24);
        primitiveSizes.Add(typeof(Matrix4x4), 64);
        primitiveSizes.Add(typeof(Plane), 16);
        primitiveSizes.Add(typeof(Ray), 24);
        primitiveSizes.Add(typeof(Ray2D), 16);
        primitiveSizes.Add(typeof(Rect), 16);
        primitiveSizes.Add(typeof(Resolution), 12);

        primitiveParsers.Add(typeof(Vector2), new UnityVector2Parser());
        primitiveParsers.AddManySerializer(new UnityVector2Parser.Many(config));

        primitiveParsers.Add(typeof(Vector3), new UnityVector3Parser());
        primitiveParsers.AddManySerializer(new UnityVector3Parser.Many(config));

        primitiveParsers.Add(typeof(Vector4), new UnityVector4Parser());
        primitiveParsers.AddManySerializer(new UnityVector4Parser.Many(config));

        primitiveParsers.Add(typeof(Quaternion), new UnityQuaternionParser());
        primitiveParsers.AddManySerializer(new UnityQuaternionParser.Many(config));

        primitiveParsers.Add(typeof(Color), new UnityColorParser());
        primitiveParsers.AddManySerializer(new UnityColorParser.Many(config));

        primitiveParsers.Add(typeof(Color32), new UnityColor32Parser());
        primitiveParsers.AddManySerializer(new UnityColor32Parser.Many(config));

        primitiveParsers.Add(typeof(Bounds), new UnityBoundsParser());
        primitiveParsers.AddManySerializer(new UnityBoundsParser.Many(config));

        primitiveParsers.Add(typeof(Matrix4x4), new UnityMatrix4x4Parser());
        primitiveParsers.AddManySerializer(new UnityMatrix4x4Parser.Many(config));
    }
#pragma warning restore IDE0051
}
