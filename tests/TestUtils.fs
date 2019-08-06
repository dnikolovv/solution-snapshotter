[<AutoOpen>]
module TestUtils

open UtilTypes
open System.Reflection
open System.IO.Compression
open System.IO
open Utils
open System
open Argu
open CLI
open Xunit

[<AutoOpen>]
module Directories =
    let shouldContainFileWithPredicate' filePattern (filePredicate:FileInfo -> bool) (assertion:string -> bool -> Unit) (dir:DirectoryInfo) =
        dir.EnumerateFiles(filePattern)
        |> Seq.exists filePredicate
        |> assertion filePattern
        dir

    let shouldContainFileWithPredicate filePattern filePredicate dir =
        dir
        |> shouldContainFileWithPredicate'
            filePattern
            filePredicate
            (fun pattern -> fun result -> Assert.True(result, (sprintf "'%s' either did not contain a file with pattern '%s' or the file did not meet the predicate." dir.FullName pattern)))
    
    let shouldContainFile filePattern dir =
        shouldContainFileWithPredicate filePattern (fun _ -> true) dir

    let containsFoldersWithPattern pattern (dir:DirectoryInfo) =
        dir.EnumerateDirectories(pattern)
        |> Seq.isEmpty

    let containsFoldersWithPatterns patterns resultAssertion (dir:DirectoryInfo) =
        patterns
        |> List.iter (fun pattern ->
            dir
            |> containsFoldersWithPattern pattern
            |> resultAssertion pattern)
        dir

    let shouldContainFolders folderPatterns (dir:DirectoryInfo) =
        dir
        |> containsFoldersWithPatterns
            folderPatterns
            (fun pattern -> fun result -> Assert.False(result, (sprintf "'%s' should've contained a folder with pattern '%s', but it didn't." dir.FullName pattern)))

    let shouldNotContainFolders folderPatterns (dir:DirectoryInfo) =
        dir
        |> containsFoldersWithPatterns
            folderPatterns
            (fun pattern -> fun result -> Assert.True(result, (sprintf "'%s' shouldn't have contained a folder with pattern '%s', but it did." dir.FullName pattern)))

[<AutoOpen>]
module Setup =
    type SourceSlnFile = string
    type DestinationFolder = string
    type TemplateFolder = string
    type VsixFolder = string
    type CliArgs = ParseResults<Arguments>
    type ConstructArguments = SourceSlnFile -> DestinationFolder -> CliArgs
    type OnSuccessfulGenerationCallback = TemplateFolder -> VsixFolder -> CliArgs -> Unit

    let assemblyName = "SolutionSnapshotter.Tests"
    let tempFolderName = "ss-temp"
    let getShortGuid = Guid.NewGuid().ToString().Substring(0, 7)

    // If we use the assembly's execution location the path becomes too long
    // But the OS's temp folder is in another drive in Azure Pipelines so :)
    let getTempFolder =
        let agentBuildDir = Environment.GetEnvironmentVariable("agentBuildDir")

        if not <| String.IsNullOrEmpty(agentBuildDir) then
            Path.Combine(agentBuildDir, tempFolderName)
        else Path.Combine(
                Path.GetTempPath(),
                tempFolderName)

    let deleteIfExists dirName =
        if Directory.Exists dirName then
            Directory.Delete(dirName, true)
        else ()

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
                getTempFolder,
                // Use a unique folder each time so we can execute the tests in parallel
                getShortGuid,
                projectSetupName)

        setupZipArchive.ExtractToDirectory(pathToExtractTo)

        let slnFileName =
            slnFileName
            |> cutEnd ".sln"
            |> fun f -> f + ".sln"

        Path.Combine(pathToExtractTo, slnFileName)
        |> ExistingFilePath.create

    let generateSetupAndDo setupName slnFileName (constructArguments:ConstructArguments) (onSuccessfulGeneration:OnSuccessfulGenerationCallback) =
        let sourceSlnFile = extractTestProjectSetup setupName slnFileName |> ExistingFilePath.value
    
        let destinationFolder =
            Path.Combine(
                getTempFolder,
                (sprintf "dev-adv-%s" getShortGuid))

        try
            let args = constructArguments sourceSlnFile destinationFolder
            let (_, templateZipPath, vsixCsprojPath) = Program.generateTemplate args
            onSuccessfulGeneration (Path.GetDirectoryName(templateZipPath |> ExistingFilePath.value)) (Path.GetDirectoryName(vsixCsprojPath)) args
        finally
            deleteIfExists (sourceSlnFile |> Path.GetDirectoryName |> Path.GetDirectoryName)
            deleteIfExists destinationFolder
            ()
            