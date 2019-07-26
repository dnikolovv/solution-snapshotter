module SolutionInfoParser

open System.IO
open System.Collections.Generic
open Utils
open Microsoft.Build.Construction
open Types
open UtilTypes

let private toProjectInfo (solution:SolutionFile) (csproj:FileInfo) : ProjectInfo =
    let getProjectId csprojFullPath =
        (solution.ProjectsInOrder
        |> Seq.find (fun p -> p.AbsolutePath = csprojFullPath)).ProjectGuid

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
        
    let projectId = getProjectId csproj.FullName
    let solutionDestinationPath = getSolutionDestinationPath projectId
        
    { Csproj = ExistingFile.create csproj
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

    let rootProjectPath = Path.GetDirectoryName(pathToSln)
    let solution = SolutionFile.Parse(pathToSln)
    let csprojFiles = scanForFiles rootProjectPath "*.csproj"

    csprojFiles
    |> Seq.map (toProjectInfo solution)
    |> List.ofSeq