module ExtraFoldersParser

open System
open System.IO
open Utils
open Types
open Paths

let private shouldIgnoreExtraFolder (directory:DirectoryInfo) fileExtensionsToIgnore =
    let files = directory.GetFiles()
    // We don't care about empty extra folders
    files.Length = 0 ||
    // Nor do we care about extra folders that contain only ignored files
    files |> Seq.forall (fun f -> fileExtensionsToIgnore |> List.contains f.Extension)

/// <summary>
/// Retrieves extra (non-project) folders.
/// An extra folder is also the "root" if it contains files other than the ignored extensions (e.g. the .gitignore, .dockerignore next to the .sln).
/// </summary>
let findAndCopyExtraFolders rootProjectPath destination (foldersToIgnore:string list) (fileExtensionsToIgnore:string list) =
    let rootProjectPath = rootProjectPath |> ExistingDirPath.value
    
    let destination = destination |> ExistingDirPath.value

    scanForDirectoriesThatDoNotContain rootProjectPath "*.csproj" foldersToIgnore
    |> Seq.filter (fun d -> not <| shouldIgnoreExtraFolder d fileExtensionsToIgnore)
    |> Seq.map (fun d ->
        // To avoid name collisions (e.g. multiple "configuration" folders throughout the structure)
        // we need the name to be unique, thus we append a substring of a random GUID to it
        let extraContentsFolderName = sprintf "%s_%s" d.Name (Guid.NewGuid().ToString().Substring(0, 5))
        
        let destinationToCopyContentsTo =
            Path.Combine(destination, extraContentsFolderName)
            |> ExistingDirPath.createFromNonExistingSafe

        copyDirectory
            d.FullName
            destinationToCopyContentsTo
            false
            foldersToIgnore
            fileExtensionsToIgnore

        let folderPath = d.FullName |> cutStart rootProjectPath

        { FolderPath = folderPath
          ContentPath = extraContentsFolderName }
    )
    |> List.ofSeq

