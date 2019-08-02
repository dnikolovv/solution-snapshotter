module Program

open System.IO
open Argu
open CLI
open Utils
open UtilTypes
open System

let getDefaultDestinationPath =
    let defaultDestination = Path.Combine(@"C:\", Constants.DefaultDestinationFolderName)
    
    let generateDestinationFolder counter =
        sprintf "%s-%d" defaultDestination counter
    
    if not <| Directory.Exists defaultDestination then
        defaultDestination
    else
        let mutable counter = 1
        let mutable destination = generateDestinationFolder counter
    
        while Directory.Exists destination do
            counter <- counter + 1
            destination <- generateDestinationFolder counter
    
        destination

let getDestinationPath customDestination =
    match customDestination with
    | Some path ->
        let path =
            path
            |> toRootedPath
            |> ExistingDirPath.createFromNonExistingOrFail
            |> ExistingDirPath.value

        path
    | None -> getDefaultDestinationPath

let getArgs inputTypeArgs =
    let parser = ArgumentParser.Create<Arguments>(programName = Constants.ProgramName)

    match inputTypeArgs with
    | Inline args::_ ->
        args
    | From_File path::_ ->
        let configFilePath =
            toRootedPath path 
            |> ExistingFilePath.checkIfExisting
        parser.ParseConfiguration(ConfigurationReader.FromAppSettingsFile(configFilePath))
    | [] -> raise (ArgumentException "Invalid subcommand.")

[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<InputOptionsArguments>(programName = Constants.ProgramName)
        let inputType = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
        let args = getArgs (inputType.GetAllResults())

        let templateName = args.GetResult Template_Name
        let templateDescription = args.GetResult Template_Description
        let templateWizardAssembly = args.GetResult Template_Wizard_Assembly
        let rootProjectNamespace = args.GetResult Root_Project_Namespace
        let foldersToIgnore = args.GetResult Folders_To_Ignore
        let pathToSln = toRootedPath (args.GetResult Path_To_Sln) |> ExistingFilePath.create
        let rootProjectPath = ExistingFilePath.getDirectoryName pathToSln |> ExistingDirPath.create
        let rootTemplateDestination = getDestinationPath (args.TryGetResult Destination)
        let fileExtensionsToIgnore = args.GetResult File_Extensions_To_Ignore
        
        let templateDestination =
            Path.Combine(rootTemplateDestination, "template")
            |> ExistingDirPath.createFromNonExistingSafe
        
        let templateIconName =
            CustomFileHandler.setCustomFileOrEmbeddedDefault
                (args.TryGetResult Custom_Template_Icon)
                Constants.DefaultTemplateIcon
                templateDestination
        
        let templateZipPath =
            ProjectToTemplateConverter.convertProject
                { PathToSln = pathToSln
                  TemplateName = templateName
                  TemplateDescription = templateDescription
                  IconName = templateIconName
                  TemplateWizardAssembly = templateWizardAssembly
                  TemplateDestination = templateDestination
                  RootProjectNamespace = rootProjectNamespace
                  FileExtensionsInExtraFoldersToIgnore = fileExtensionsToIgnore
                  FoldersToIgnore = foldersToIgnore
                  RootProjectPath = rootProjectPath }

        let vsixVersion = args.GetResult Vsix_Version
        let vsixPublisherFullName = args.GetResult Vsix_Publisher_Full_Name
        let vsixPublisherUsername = args.GetResult Vsix_Publisher_Username
        let vsixDisplayName = args.GetResult Vsix_Display_Name
        let vsixDescription = args.GetResult Vsix_Description
        let vsixMoreInfo = args.GetResult Vsix_More_Info
        let vsixGettingStartedGuide = args.GetResult Vsix_Getting_Started
        let vsixInternalName = args.GetResult Vsix_Internal_Name
        let vsixCategories = args.GetResult Vsix_Categories
        let vsixCustomPackageGuid = args.TryGetResult Vsix_Custom_Package_Guid
        let vsixCustomProjectGuid = args.TryGetResult Vsix_Custom_Project_Guid
        let vsixCustomId = args.TryGetResult Vsix_Custom_Id
        let vsixPriceCategory = args.GetResult Vsix_Price_Category
        let vsixQnaEnable = args.GetResult Vsix_Qna_Enable
        let vsixRepo = args.TryGetResult Vsix_Repo
        let vsixTags = args.TryGetResult Vsix_Tags |> Option.defaultValue "" |> String50.create

        let vsixProjectDestination =
            Path.Combine(rootTemplateDestination, "vsix")
            |> ExistingDirPath.createFromNonExistingSafe

        let vsixIconName =
            CustomFileHandler.setCustomFileOrEmbeddedDefault
                (args.TryGetResult Vsix_Custom_Icon)
                Constants.DefaultWizardIcon
                vsixProjectDestination

        let vsixOverviewMd =
            CustomFileHandler.setCustomFile
                (args.GetResult Vsix_Overview_Md_Path)
                vsixProjectDestination

        let vsixCsprojPath =
            TemplateWizardGenerator.generateWizard
                { WizardName = templateWizardAssembly
                  TemplateZipPath = templateZipPath
                  Destination = vsixProjectDestination
                  IconName = vsixIconName
                  Version = vsixVersion
                  PublisherFullName = vsixPublisherFullName
                  PublisherUsername = vsixPublisherUsername
                  PriceCategory = vsixPriceCategory
                  DisplayName = vsixDisplayName
                  Qna = vsixQnaEnable
                  Repo = vsixRepo
                  Description = vsixDescription
                  MoreInfo = vsixMoreInfo
                  GettingStartedGuide = vsixGettingStartedGuide
                  CustomPackageGuid = vsixCustomPackageGuid
                  CustomProjectGuid = vsixCustomProjectGuid
                  OverviewMdPath = vsixOverviewMd
                  CustomId = vsixCustomId
                  Categories = vsixCategories
                  InternalName = vsixInternalName
                  Tags = vsixTags }

        printfn "Successfully converted %s to a template!" (ExistingFilePath.getFileName pathToSln)
        printfn "You'll find the zipped template at '%s' and VSIX project at '%s'." (templateZipPath |> ExistingFilePath.value) vsixCsprojPath
        0
    with e ->
        eprintfn "%s" e.Message
        1
