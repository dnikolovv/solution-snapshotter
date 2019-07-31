module MultiProjectTemplateGenerator

open Utils
open Types
open System.IO
open UtilTypes

let private toProjectLink (destination:string) (projectTemplateInfo:ProjectTemplateInfo) : ProjectTemplateLink =
    let vsTemplatePath =
        projectTemplateInfo.VsTemplatePath
        |> ExistingFilePath.value
        |> cutStart (destination.TrimEnd('\\'))
        |> trimStart '\\'

    let safeProjectName = projectTemplateInfo.OriginalProjectName.Replace(projectTemplateInfo.RootNamespace, Constants.SafeProjectName)

    { VsTemplatePath = vsTemplatePath
      OriginalProjectName = projectTemplateInfo.OriginalProjectName
      OriginalProjFilePath = ExistingFilePath.value projectTemplateInfo.OriginalProjFilePath
      SafeProjectName = safeProjectName }

let private generateProjectLinkElements projectTemplates destination =
    projectTemplates
    |> List.map (toProjectLink destination)

let private generateSingleTemplate generator (args:MultiProjectTemplateArgs) projectInfo =
    let destination = args.Destination |> ExistingDirPath.value
    let pathToProjFile =
        projectInfo.ProjectFile
        |> ExistingFile.value
        |> fun f -> f.FullName
        |> ExistingFilePath.create

    let projFileName =
        pathToProjFile
        |> ExistingFilePath.getFileName
        |> cutEnd ".csproj"
        |> cutEnd ".fsproj"

    let newTemplateDestination =
        Path.Combine(destination, projFileName)
        |> ExistingDirPath.createFromNonExistingSafe

    generator
        { ProjFilePath = pathToProjFile
          PhysicalDestination = newTemplateDestination
          RootProjectNamespace = args.RootProjectNamespace
          FoldersToIgnore = args.FoldersToIgnore
          SolutionDestinationPath = RelativePath.value projectInfo.SolutionDestinationPath }

let private generateIndividualTemplates generator args =
    args.ProjectsDestinationInfo
    |> List.map (generateSingleTemplate generator args)

let private generateTemplateXml singleProjectTemplateGenerator (args:MultiProjectTemplateArgs) =
    let destination = args.Destination |> ExistingDirPath.value
    let templates = generateIndividualTemplates singleProjectTemplateGenerator args
    let linkElements = generateProjectLinkElements templates destination
    let args = { args with Projects = linkElements }
    { MultiProjectVsTemplate.getTemplate args with InnerProjectsTemplateInfo = templates }

let generateVsTemplateFile singleProjectTemplateGenerator (args:MultiProjectTemplateArgs) : MultiProjectTemplateInfo =
    
    let destination = args.Destination |> ExistingDirPath.value

    let vsTemplateName = sprintf "%s.vstemplate" args.TemplateName
        
    let vsTemplateDestination =
        Path.Combine(
            destination,
            vsTemplateName)

    let multiProjectTemplate = generateTemplateXml singleProjectTemplateGenerator args
        
    File.WriteAllText(vsTemplateDestination, multiProjectTemplate.Xml)
        
    { VsTemplatePath = ExistingFilePath.create vsTemplateDestination
      RootDestinationPath = ExistingDirPath.create destination
      Projects = multiProjectTemplate.InnerProjectsTemplateInfo }