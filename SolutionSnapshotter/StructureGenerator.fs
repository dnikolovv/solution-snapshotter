module StructureGenerator
    
open Types
open Utils
open System.IO
open Microsoft.Build.Construction
open UtilTypes
open System

let rec private buildNodeFromFolder (folder:ProjectInSolution) (allFolders:List<ProjectInSolution>) pathUntilNow =
    let node =
        { Name = folder.ProjectName
          FullPath = sprintf "%s%s" pathUntilNow folder.ProjectName
          Children = [] }

    let children =
        allFolders
        |> Seq.filter (fun f -> f.ParentProjectGuid = folder.ProjectGuid)

    let newChildren =
        children
        |> Seq.map (fun childFolder -> buildNodeFromFolder childFolder allFolders (sprintf "%s\\" node.FullPath))
        |> List.ofSeq

    { node with Children = newChildren }

let private parseSolutionFolderStructure (solution:SolutionFile) : SolutionFolderStructure =
    let allFolders =
        solution.ProjectsInOrder
        |> Seq.filter (fun p -> p.ProjectType = SolutionProjectType.SolutionFolder)
        |> List.ofSeq

    let rootFolders =
        allFolders
        |> Seq.filter (fun f -> f.ParentProjectGuid = null)
        |> List.ofSeq

    let nodes =
        rootFolders
        |> List.map (fun f -> buildNodeFromFolder f allFolders "")

    { Nodes = nodes }

let private getProjectsDestinationInfo rootProjectPath projectTemplates : List<ProjectDestinationInfoDto> =
    let toProjectDestinationInfoDto (project:ProjectTemplateInfo) =
        let trimSlashes str =
            str
            |> trimEnd ['\\'; '/']

        let originalProjFilePath =
            project.OriginalProjFilePath
            |> ExistingFilePath.value
            |> fun p -> Uri(p, UriKind.Absolute)

        let rootProjectPath =
            rootProjectPath
            |> trimSlashes
            // It must end with \ in order for MakeRelativeUri to work properly
            |> fun s -> s + "\\"
            |> fun p -> Uri(p, UriKind.Absolute)

        let destinationDirectory =
            rootProjectPath.MakeRelativeUri(originalProjFilePath).ToString()
            |> cutEnd (Path.GetFileName(originalProjFilePath.ToString()))
            |> trimSlashes
            |> cutEnd project.OriginalProjectName
            |> trimSlashes

        { ProjectName = project.OriginalProjectName
          SafeProjectName = project.SafeProjectName
          DestinationDirectory = destinationDirectory
          DestinationSolutionDirectory = project.SolutionFolderDestinationPath |> RelativePath.value
          ProjectFileExtension = originalProjFilePath.AbsolutePath |> FileInfo |> fun f -> f.Extension }
        
    projectTemplates
    |> List.map toProjectDestinationInfoDto

let generateStructureConfiguration rootProjectPath extraFolders projectTemplates solution : StructureConfigurationDto =
    let rootProjectPath = rootProjectPath |> ExistingDirPath.value
    let projectsDestinationInfo = getProjectsDestinationInfo rootProjectPath projectTemplates
    let solutionStructure = parseSolutionFolderStructure solution

    { Projects = projectsDestinationInfo
      ExtraFolders = extraFolders
      SolutionFolderStructure = solutionStructure }

