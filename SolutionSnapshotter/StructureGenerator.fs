module StructureGenerator
    
open Types
open Utils
open System.IO
open Microsoft.Build.Construction
open UtilTypes

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
        // It is assumed that the project name matches the .csproj name
        // E.g. MyProject.Business.csproj matches the MyProject.Business project
        // which is located inside the MyProject.Business folder
        let originalCsprojPath = ExistingFilePath.value project.OriginalCsprojPath

        let destinationDirectory =
            originalCsprojPath
            |> cutStart rootProjectPath
            |> cutEnd (Path.GetFileName(originalCsprojPath))
            |> trimEnd '\\'
            |> cutEnd project.OriginalProjectName
            |> trimEnd '\\'

        { ProjectName = project.OriginalProjectName
          SafeProjectName = project.SafeProjectName
          DestinationDirectory = destinationDirectory
          DestinationSolutionDirectory = project.SolutionFolderDestinationPath |> RelativePath.value }
        
    projectTemplates
    |> List.map toProjectDestinationInfoDto

let generateStructureConfiguration rootProjectPath extraFolders projectTemplates solution : StructureConfigurationDto =
    let rootProjectPath = rootProjectPath |> ExistingDirPath.value
    let projectsDestinationInfo = getProjectsDestinationInfo rootProjectPath projectTemplates
    let solutionStructure = parseSolutionFolderStructure solution

    { Projects = projectsDestinationInfo
      ExtraFolders = extraFolders
      SolutionFolderStructure = solutionStructure }

