module Types

open System.IO
open Paths
open Newtonsoft.Json
open JsonConverters

type SingleProjectVsTemplate =
    { Xml: string }

type ProjectTemplateLink = 
    { VsTemplatePath: string
      SafeProjectName: string
      OriginalProjectName: string
      OriginalCsprojPath: string }

type ProjectInfo =
    { Csproj: ExistingFile
      SolutionDestinationPath: RelativePath }

type ProjectTemplateInfo =
    { VsTemplatePath: ExistingFilePath
      OriginalCsprojPath: ExistingFilePath
      OriginalProjectName: string
      SafeProjectName: string
      RootNamespace: string
      SolutionFolderDestinationPath: RelativePath }

type MultiProjectVsTemplate =
    { Xml: string
      LinkElements: List<ProjectTemplateLink>
      InnerProjectsTemplateInfo: List<ProjectTemplateInfo> }

type MultiProjectTemplateInfo =
    { VsTemplatePath: ExistingFilePath
      RootDestinationPath: ExistingDirPath
      Projects: List<ProjectTemplateInfo> }

type MultiProjectTemplateArgs =
    { TemplateName: string
      TemplateDescription: string
      TemplateIcon: string
      TemplateWizardAssembly: string
      Destination: ExistingDirPath
      RootProjectNamespace: string
      FoldersToIgnore: List<string>
      ProjectsDestinationInfo: List<ProjectInfo>
      Projects: List<ProjectTemplateLink> }

type ConvertProjectToTemplateArgs =
    { PathToSln: ExistingFilePath
      TemplateName: string
      TemplateDescription: string
      IconName: string
      TemplateWizardAssembly: string
      TemplateDestination: ExistingDirPath
      RootProjectNamespace: string
      FileExtensionsInExtraFoldersToIgnore: List<string>
      FoldersToIgnore: List<string>
      RootProjectPath: ExistingDirPath }

type ExtraFolderMapping =
    { FolderPath: string
      ContentPath: string }

type ProjectDestinationInfo =
    { SafeProjectName: string 
      ProjectName: string
      DestinationDirectory: string
      DestinationSolutionDirectory: RelativePath }

type SolutionFolderNode =
    { FullPath: string
      Name: string
      Children: List<SolutionFolderNode> }

type SolutionFolderStructure =
    { Nodes: List<SolutionFolderNode> }

type StructureConfiguration =
    { ProjectDestinationInfo: List<ProjectDestinationInfo>
      ExtraFolders: List<ExtraFolderMapping> 
      SolutionFolderStructure: SolutionFolderStructure }

type ProjectDestinationInfoDto =
    { SafeProjectName: string 
      ProjectName: string
      DestinationDirectory: string
      DestinationSolutionDirectory: string }

type StructureConfigurationDto =
    { Projects: List<ProjectDestinationInfoDto>
      ExtraFolders: List<ExtraFolderMapping> 
      SolutionFolderStructure: SolutionFolderStructure }

type SingleProjectTemplateArgs =
    { CsprojPath: ExistingFilePath
      PhysicalDestination: ExistingDirPath
      RootProjectNamespace: string
      FoldersToIgnore: List<string>
      SolutionDestinationPath: string }

type FolderNode =
    { Name: string
      Files: List<FileInfo>
      FullPath: string
      ChildFolders: List<FolderNode> }

type PublishManifestIdentity =
    { InternalName: string
      Version: string
      DisplayName: string
      Description: string
      Tags: List<string> }

[<JsonConverter(typeof<UnionJsonConverter>)>]
type VsixPriceCategory =
    | Free
    | Trial
    | Paid

type GenerateTemplateWizardArgs =
    { WizardName: string
      TemplateZipPath: ExistingFilePath
      Destination: ExistingDirPath
      Version: string
      IconName: string
      Publisher: string
      DisplayName: string
      Description: string
      MoreInfo: string
      GettingStartedGuide: string
      InternalName: string
      Categories: List<string>
      OverviewMdPath: string
      PriceCategory: VsixPriceCategory
      Qna: bool
      Repo: string option
      CustomPackageGuid: string option
      CustomProjectGuid: string option
      CustomId: string option
      Tags: List<string> }

type WizardPublishManifest =
    { Categories: List<string>
      Identity: PublishManifestIdentity
      Overview: string
      PriceCategory: VsixPriceCategory
      Publisher: string
      Qna: bool
      [<JsonConverter(typeof<OptionConverter>)>]
      Repo: string option }