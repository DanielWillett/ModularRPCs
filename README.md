# ModularRPCs

Work in progress...

## Supported Primitive Types
All the following types can be serialized/parsed individually or in an enumerable.

*Enum and nullable arrays are not supported by default.*

### Base
* bool, char, double, float, int, long, uint, ulong, short, ushort, sbyte, byte, nint, nuint
* Half (.NET 5+)
* Int128, UInt128 (.NET 7+)
* decimal
* DateTimeOffset
* DateTime
* TimeSpan
* Guid
* String (any encoding, UTF8 by default)
* All enums
* Nullable value types of any supported value type

### ModularRPCs.Unity
* Vector2, Vector3, Vector4
* Bounds
* Color, Color32
* Quaternion
* Matrix4x4
* Plane
* Ray, Ray2D
* Rect
* Resolution



### MSBuild Properties

| Property                          | Description                          | Default |
| --------------------------------- | ------------------------------------ | ------- |
| DisableModularRPCsSourceGenerator | Disables source generation features. | False   |