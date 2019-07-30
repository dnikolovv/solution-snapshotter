module SolutionInfoParser

open System.IO
open System.Collections.Generic
open Utils
open Microsoft.Build.Construction
open Types
open UtilTypes

let private toProjectInfo (solution:SolutionFile) (projectId, projectFile) : ProjectInfo =
    let getByGuid projectGuid =
        if projectGuid = null
            then null
        else
            let (_, value) =
                solution
                    .ProjectsByGuid
                    .TryGetValue(projectGuid)
            value

    let getSolutionDestinationPath projectId =
        let project = solution.ProjectsByGuid.[projectId]
        let mutable parent = getByGuid project.ParentProjectGuid
        let parentsQueue = new Queue<_>()
            
        while parent <> null do
            parentsQueue.Enqueue(parent)
            parent <- getByGuid parent.ParentProjectGuid
            
        let parentNames =
            parentsQueue
            |> Seq.map (fun p -> p.ProjectName)
            |> reverse

        parentNames
        |> String.concat "\\"

    let solutionDestinationPath = getSolutionDestinationPath projectId
        
    { ProjectFile = projectFile
      SolutionDestinationPath = Path.createRelative solutionDestinationPath }

/// <summary>
/// Retrieves information about projects inside a .sln file.
/// Works under the assumption that the solution will not reference projects
/// in folders higher in the folder hierarchy.
/// Only includes projects for which there is a .csproj file.
/// Does not include solution folders.
/// </summary>
let parseProjectInfo pathToSln = 
    let pathToSln = pathToSln |> ExistingFilePath.value

    let solution = SolutionFile.Parse(pathToSln)

    let projectFiles =
        solution.ProjectsInOrder
        |> Seq.filter (fun p -> p.ProjectType <> SolutionProjectType.SolutionFolder)
        |> Seq.map (fun p -> (p.ProjectGuid, p.AbsolutePath |> FileInfo |> ExistingFile.wrap))

    // TODO: Maybe we can get those csproj files from the parsed .sln
    //let rootProjectPath = Path.GetDirectoryName(pathToSln)
    // Yes, we can, and with it provide the project id
    //let projectFiles = scanForFiles rootProjectPath ["*.csproj"; "*.fsproj"]

    projectFiles
    |> Seq.map (toProjectInfo solution)
    |> List.ofSeq