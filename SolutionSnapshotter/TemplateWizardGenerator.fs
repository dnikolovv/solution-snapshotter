module TemplateWizardGenerator

open Types
open Utils
open System
open System.Xml
open System.IO
open System.IO.Compression
open System.Text.RegularExpressions
open UtilTypes

let vsixDefaultNamespace = "http://schemas.microsoft.com/developer/vsx-schema/2011"

let private findVsixManifestNode nodeName (vsixManifestXml:XmlDocument) =
    vsixManifestXml.ChildNodes
    |> findNode nodeName

let private getVsixManifestNode nodeName (vsixManifestXml:XmlDocument) =
    let node =
        vsixManifestXml.ChildNodes
        |> findNode nodeName

    match node with
    | Some n -> n
    | None ->
        raise
            (InvalidOperationException
                (sprintf "Expected to find node %s inside the template manifest, but it didn't exist. Most likely this means a broken WizardTemplate.zip." nodeName))

let private setVsixIdentityAttribute (attributeName:string) value (vsixManifest:XmlDocument) =
    let identityNode = vsixManifest |> getVsixManifestNode "Identity"
    identityNode.Attributes.[attributeName].Value <- value
    vsixManifest

let private setVsixManifestMetadataNode nodeName value (vsixManifest:XmlDocument) =
    let node = vsixManifest |> findVsixManifestNode nodeName
    match node with
    | Some n -> n.InnerText <- value
    | None ->
        let metadataNode = vsixManifest |> getVsixManifestNode "Metadata"
        let newNode = createNode vsixManifest nodeName value vsixDefaultNamespace []
        metadataNode.AppendChild(newNode) |> ignore
        ()
    vsixManifest

let private setGettingStartedGuide gettingStartedGuide vsixManifest =
    setVsixManifestMetadataNode "GettingStartedGuide" gettingStartedGuide vsixManifest

let private setMoreInfo moreInfo vsixManifestFile =
    setVsixManifestMetadataNode "MoreInfo" moreInfo vsixManifestFile

let private setDescription description vsixManifest =
    setVsixManifestMetadataNode "Description" description vsixManifest

let private setDisplayName displayName vsixManifest =
    setVsixManifestMetadataNode "DisplayName" displayName vsixManifest

let private setIcon iconName vsixManifest =
    setVsixManifestMetadataNode "Icon" iconName vsixManifest

let private setTags tags vsixManifest =
    setVsixManifestMetadataNode "Tags" (tags |> String50.value) vsixManifest

let private setPublisher publisher vsixManifest =
    setVsixIdentityAttribute "Publisher" publisher vsixManifest

let private setVersion version vsixManifest =
    setVsixIdentityAttribute "Version" version vsixManifest

let private setIdAttribute customId wizardName vsixManifest =
    setVsixIdentityAttribute
        "Id"
        (customId |> Option.defaultValue (sprintf "%s.%s" wizardName (Guid.NewGuid().ToString())))
        vsixManifest

let private getProjectFile (templateDirectory:DirectoryInfo) wizardName =
    templateDirectory.GetFiles(sprintf "%s.*sproj" wizardName).[0]

let private replaceProjectGuid customProjectGuid wizardName (templateDirectory:DirectoryInfo) =
    let projFile = getProjectFile templateDirectory wizardName
    let contents = File.ReadAllText(projFile.FullName)

    let id =
        customProjectGuid
        |> Option.defaultValue (Guid.NewGuid().ToString())
        
    contents.Replace("{$guid1$}", id) |> ignore
    templateDirectory

let private replaceIconName iconName wizardName (templateDirectory:DirectoryInfo) =
    let projFile = (getProjectFile templateDirectory wizardName).FullName
    replaceInFile projFile [(Constants.VsixIconPlaceholder, iconName)] |> ignore
    templateDirectory

let private replacePackageGuidString customPackageId wizardName (templateDirectory:DirectoryInfo) =
    let id =
        customPackageId
        |> Option.defaultValue (Guid.NewGuid().ToString().ToUpper())

    let packageFile = templateDirectory.GetFiles(sprintf "%sPackage.cs" wizardName).[0]
    let contents = File.ReadAllText(packageFile.FullName)

    Regex.Replace(
        contents,
        "public const string PackageGuidString = \"(.*)\";",
        sprintf "public const string PackageGuidString = \"%s\";" id)
    |> ignore

    templateDirectory

let private replaceUsings wizardName (tempZipTemplateDirectory:DirectoryInfo) =
    tempZipTemplateDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
    |> Seq.map (fun f -> f.FullName)
    // We don't want to be changing text inside dlls
    |> Seq.filter (fun fPath -> not <| fPath.EndsWith(".dll"))
    |> Seq.map (fun filePath ->
        replaceInFile
            filePath
            [(Constants.SafeProjectName, wizardName)
             // Revert back if we've incidentally renamed a "$safeprojectname$" string literal
             // that is required in the code as-is
             (sprintf "\"%s\"" wizardName, sprintf "\"%s\"" Constants.SafeProjectName)]
    )
    |> List.ofSeq
    |> ignore

    replaceInFileNames tempZipTemplateDirectory Constants.SafeProjectName wizardName |> ignore
    tempZipTemplateDirectory

let private extractWizardZipToTempDirectory tempDestination =
    use wizardTemplateStream = getEmbeddedResourceStream Constants.WizardTemplateZip
    use wizardTemplateZip = new ZipArchive(wizardTemplateStream)
    wizardTemplateZip.ExtractToDirectory tempDestination
    DirectoryInfo tempDestination

let private addTemplateToWizard templateZip (templateDirectory:ExistingDirPath) =
    let tempZipProjectTemplatesFolder = templateDirectory |> ExistingDirPath.combineWith Constants.ProjectTemplatesFolder
    Directory.CreateDirectory(tempZipProjectTemplatesFolder) |> ignore
    let templateZipPath = templateZip |> ExistingFilePath.value
    File.Copy(templateZipPath, Path.Combine(tempZipProjectTemplatesFolder, Constants.TemplateZip))
    templateDirectory

let private addPublishManifestFile (args:GenerateTemplateWizardArgs) templateDirectory =
    let tags =
        args.Tags
        |> String50.value
        |> fun s -> s.Split [|','|]
        |> Seq.map (fun s -> s.Trim(' '))
        |> List.ofSeq
        
    let publishManifest =
        { Categories = args.Categories
          Overview = args.OverviewMdPath
          PriceCategory = args.PriceCategory
          Publisher = args.PublisherUsername
          Qna = args.Qna
          Repo = args.Repo
          Identity =
              { InternalName = args.InternalName
                Version = args.Version
                DisplayName = args.DisplayName
                Description = args.Description
                Tags = tags }}

    let publishManifestJson = PublishManifestGenerator.generatePublishManifestJson publishManifest
    let manifestJsonPath = templateDirectory |> ExistingDirPath.combineWith "publishManifest.json"
    File.WriteAllText(manifestJsonPath, publishManifestJson)

    templateDirectory

let generateWizard (args:GenerateTemplateWizardArgs) =
    // We cannot use the OS's temp folder because it may be in a different drive
    let tempDestination = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString())
    
    let generatedWizardDirectory =
        extractWizardZipToTempDirectory tempDestination
        |> replaceUsings args.WizardName
        |> replacePackageGuidString args.CustomPackageGuid args.WizardName
        |> replaceProjectGuid args.CustomProjectGuid args.WizardName
        |> replaceIconName args.IconName args.WizardName
        |> fun d -> d.FullName |> ExistingDirPath.create
        |> addTemplateToWizard args.TemplateZipPath
        |> addPublishManifestFile args
        |> moveFolderContents args.Destination

    let vsixManifestFile =
        generatedWizardDirectory
        |> ExistingDirPath.getFirstFile "*.vsixmanifest"

    let newVsixManifest =
        vsixManifestFile
        |> fun f -> File.ReadAllText(f.FullName)
        |> toXmlDocument
        |> setIdAttribute args.CustomId args.WizardName
        |> setIcon args.IconName
        |> setVersion args.Version
        |> setPublisher args.PublisherFullName
        |> setDisplayName args.DisplayName
        |> setDescription args.Description
        |> setMoreInfo args.MoreInfo
        |> setGettingStartedGuide args.GettingStartedGuide
        |> setTags args.Tags

    newVsixManifest.Save(vsixManifestFile.FullName)

    let vsixCsprojPath = args.Destination |> ExistingDirPath.combineWith (sprintf "%s.csproj" args.WizardName)
    vsixCsprojPath
    

