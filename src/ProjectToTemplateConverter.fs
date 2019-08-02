module ProjectToTemplateConverter

open UtilTypes
open Newtonsoft.Json
open Microsoft.Build.Construction
open System.IO
open Types
open System
open System.IO.Compression

let private zipTemplate (templateFolder:ExistingDirPath) =
    let templateFolder = templateFolder |> ExistingDirPath.value
    let tempPath = Path.Combine(Directory.GetCurrentDirectory(), sprintf "temp-%s" (Guid.NewGuid().ToString().Substring(0, 5)))
    Directory.CreateDirectory(tempPath) |> ignore
    let tempZipPath = Path.Combine(tempPath, (sprintf "Template-%s.zip" (Guid.NewGuid().ToString().Substring(0, 5))))
    ZipFile.CreateFromDirectory(templateFolder, tempZipPath)
    let zipDestination = Path.Combine(templateFolder, Constants.TemplateZip)
    File.Move(tempZipPath, zipDestination)
    Directory.Delete(tempPath)
    zipDestination

let private writeStructureFile (templateDestination:ExistingDirPath) structureConfiguration =
    let templateDestination = templateDestination |> ExistingDirPath.value
    let structureJson =
        JsonConvert.SerializeObject(
            structureConfiguration,
            Formatting.Indented)

    File.WriteAllText(Path.Combine(templateDestination, Constants.StructureJson), structureJson)

let convertProject args =
    let projectsDestinationInfo =
        SolutionInfoParser
            .parseProjectInfo args.PathToSln

    let template =
        MultiProjectTemplateGenerator.generateVsTemplateFile
            SingleProjectTemplateGenerator.generateTemplate
            { TemplateName = args.TemplateName
              TemplateDescription = args.TemplateDescription
              TemplateIcon = args.IconName
              TemplateWizardAssembly = args.TemplateWizardAssembly
              Destination = args.TemplateDestination
              RootProjectNamespace = args.RootProjectNamespace
              FoldersToIgnore = args.FoldersToIgnore
              ProjectsDestinationInfo = projectsDestinationInfo
              Projects = [] }

    // We don't need the original .sln
    // and since the root folder is treated as an "extra"
    // we need to explicitly ignore it
    let fileExtensionsInExtraFoldersToIgnore = ".sln" :: args.FileExtensionsInExtraFoldersToIgnore

    let extraFolders =
        ExtraFoldersParser.findAndCopyExtraFolders
            args.RootProjectPath
            args.TemplateDestination
            args.FoldersToIgnore
            fileExtensionsInExtraFoldersToIgnore

    let solution = SolutionFile.Parse(args.PathToSln |> ExistingFilePath.value)

    let structureConfiguration =
        StructureGenerator.generateStructureConfiguration
            args.RootProjectPath
            extraFolders
            template.Projects
            solution

    writeStructureFile args.TemplateDestination structureConfiguration |> ignore
    
    zipTemplate args.TemplateDestination
    |> ExistingFilePath.create