[<AutoOpen>]
module Attributes

open Xunit.Sdk
open System.Reflection
open UtilTypes
open System.IO

type UseTestSetupAttribute(projectSetupName:string, slnFileName:string) =
    inherit BeforeAfterTestAttribute()
    let mutable extractedFolder = None

    override this.Before(methodUnderTest:MethodInfo) =
        let slnPath = extractTestProjectSetup projectSetupName slnFileName |> ExistingFilePath.value
        extractedFolder <- Some (Path.GetDirectoryName(slnPath))
        ()

    override this.After(methodUnderTest:MethodInfo) =
        match extractedFolder with
        | Some path ->
            Directory.Delete(Directory.GetParent(path).FullName, true)
        | None -> ()