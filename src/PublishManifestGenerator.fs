module PublishManifestGenerator

open Types
open Newtonsoft.Json.Linq
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

let generatePublishManifestJson (args:WizardPublishManifest) =
    let jObject = JObject.FromObject(args)
    jObject.Add("$schema", JToken.FromObject(Constants.PublishManifestSchema))
    let serializerSettings = new JsonSerializerSettings()
    serializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
    let converters = serializerSettings.Converters |> Seq.toArray
    let json = jObject.ToString(Formatting.Indented, converters)
    json