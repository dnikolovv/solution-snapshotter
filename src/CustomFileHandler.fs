module CustomFileHandler

open Utils
open UtilTypes
open System.IO

let private saveFileToDestination fileName (stream:Stream) destination =
    let pathToSaveTo = Path.Combine(destination, fileName)
    use saveFileStream = File.OpenWrite(pathToSaveTo)
    stream.CopyTo(saveFileStream)

let private saveCustomFile customFilePath destination =
    let path =
        toRootedPath customFilePath
        |> ExistingFilePath.create
        |> ExistingFilePath.value

    let fileName = Path.GetFileName(path)
    let fileAsStream = File.OpenRead(path)
    saveFileToDestination fileName fileAsStream destination
    fileName

/// <summary>
/// Copies a custom file to a destination and returns its name. (not path)
/// </summary>
let setCustomFile customFilePath (destination:ExistingDirPath) =
    let destination = destination |> ExistingDirPath.value
    saveCustomFile customFilePath destination

/// <summary>
/// Copies a given file to a destination and returns its name or a default one.
/// Assumes that the default file given is embedded in the assembly under the same name.
/// Useful for stuff like custom icons, logos, readme files, etc.
/// </summary>
let setCustomFileOrEmbeddedDefault customFilePath defaultFileName (destination:ExistingDirPath) =
    
    let destination = destination |> ExistingDirPath.value
    
    match customFilePath with
    | Some path -> saveCustomFile path destination
    | None ->
        let fileName = defaultFileName
        use fileAsStream = getEmbeddedResourceStream fileName
        saveFileToDestination fileName fileAsStream destination
        fileName