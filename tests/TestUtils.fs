[<AutoOpen>]
module TestUtils

open UtilTypes
open System.Reflection
open System.IO.Compression
open System.IO
open Utils
open System

let assemblyName = "SolutionSnapshotter.Tests"
let tempFolderName = "ss-temp"

let extractTestProjectSetup projectSetupName slnFileName : ExistingFilePath =
    use setupZipStream =
        Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(sprintf "%s.TestProjects.%s.zip" assemblyName projectSetupName)
    
    if isNull setupZipStream then
        raise (InvalidOperationException (sprintf "Tried to execute a test with a non-existing setup. (setup name: %s)" projectSetupName))

    use setupZipArchive = new ZipArchive(setupZipStream)

    let pathToExtractTo =
        Path.Combine(
            // If we use the assembly's execution location the path becomes too long
            Path.GetTempPath(),
            tempFolderName,
            // Use a unique folder each time so we can execute the tests in parallel
            Guid.NewGuid().ToString().Substring(0, 7),
            projectSetupName)

    setupZipArchive.ExtractToDirectory(pathToExtractTo)

    let slnFileName =
        slnFileName
        |> cutEnd ".sln"
        |> fun f -> f + ".sln"

    Path.Combine(pathToExtractTo, slnFileName)
    |> ExistingFilePath.create