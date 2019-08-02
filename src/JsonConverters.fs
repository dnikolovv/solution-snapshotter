module JsonConverters

open Newtonsoft.Json
open System
open Microsoft.FSharp.Reflection

type UnionJsonConverter () =
    inherit JsonConverter()
    override this.CanConvert objectType = true;

    override this.WriteJson (writer: JsonWriter, value: obj, serializer: JsonSerializer): unit = 
        writer.WriteValue(value.ToString().ToLower())

    override this.CanRead = false

    override this.ReadJson (reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) : obj =
        raise (new NotImplementedException());

/// Courtesy of https://stackoverflow.com/questions/28150908/f-json-webapi-serialization-of-option-types
type OptionConverter() =
    inherit JsonConverter()

    override x.CanConvert(t) = 
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

    override x.WriteJson(writer, value, serializer) =
        let value = 
            if value = null then null
            else 
                let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                fields.[0]  
        serializer.Serialize(writer, value)

    override x.ReadJson(reader, t, existingValue, serializer) =        
        let innerType = t.GetGenericArguments().[0]
        let innerType = 
            if innerType.IsValueType then (typedefof<Nullable<_>>).MakeGenericType([|innerType|])
            else innerType        
        let value = serializer.Deserialize(reader, innerType)
        let cases = FSharpType.GetUnionCases(t)
        if value = null then FSharpValue.MakeUnion(cases.[0], [||])
        else FSharpValue.MakeUnion(cases.[1], [|value|])