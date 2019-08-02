module SingleProjectTemplateGenerator

open Types
open System.IO
open Utils
open UtilTypes

let private copyProjectToTemplateDestination (projFilePath:ExistingFilePath) (templateDestination:ExistingDirPath) (foldersToIgnore:List<string>) =
    let projectDirectory = ExistingFilePath.getDirectoryName projFilePath
    copyDirectory projectDirectory templateDestination true foldersToIgnore []

let private buildRelativeHierarchies destination foldersToIgnore =
    let hierarchies = buildHierarchies destination foldersToIgnore

    let destinationLength =
        destination
        |> ExistingDirPath.map (fun d -> d.Length)

    let cutPath (directory:FolderNode) =
        let newFullPath =
            directory.FullPath.Substring(destinationLength).Trim('\\', '/')

        { directory with FullPath = newFullPath }

    hierarchies
    |> Seq.map cutPath
    |> Seq.filter (fun n -> not <| n.FullPath.TrimStart('\\').Contains("\\"))

let private replaceNamespacesAndUsingsInFile rootProjectNamespace (file:FileInfo) =
    let toReplacePairs = [
        (sprintf "namespace %s" rootProjectNamespace, sprintf "namespace %s" Constants.ExtSafeProjectName);
        (sprintf "using %s" rootProjectNamespace, sprintf "using %s" Constants.ExtSafeProjectName);
        (rootProjectNamespace, Constants.ExtSafeProjectName) ]

    replaceInFile file.FullName toReplacePairs

let rec private replaceNamespacesAndUsingsInNode rootProjectNamespace node =
    let replacedFiles =
        node.Files
        // TODO: Some more thought needs to be put into this
        // We don't want to be replacing phrases in *every* file because
        // we could accidently corrupt something
        |> List.filter (fun f -> f.Extension <> ".dll" && f.Extension <> ".zip")
        |> List.map (replaceNamespacesAndUsingsInFile rootProjectNamespace)

    let replacedFolders =
        node.ChildFolders
        |> List.map (replaceNamespacesAndUsingsInNode rootProjectNamespace)

    { node with
        Files = replacedFiles
        ChildFolders = replacedFolders }

let private replaceNamespacesAndUsingsInHierarchy nodes rootProjectNamespace =
    nodes
    |> List.ofSeq
    |> List.map (replaceNamespacesAndUsingsInNode rootProjectNamespace)

let private generateTemplateXml (templateXmlBuilder: string -> SingleProjectVsTemplate) projFilePath (destination:ExistingDirPath) rootProjectNamespace solutionFolderDestination =
    let projFileName = ExistingFilePath.getFileName projFilePath

    let template = templateXmlBuilder projFileName

    let projectName = projFileName |> cutEnd ".csproj" |> cutEnd ".fsproj"
    let templateFileName = sprintf "%s.vstemplate" projectName
    
    let destination = ExistingDirPath.value destination
    let templateDestination =
        Path.Combine(destination, templateFileName)

    File.WriteAllText(templateDestination, template.Xml)

    { VsTemplatePath = ExistingFilePath.create templateDestination
      OriginalProjFilePath = projFilePath
      OriginalProjectName = projectName
      // TODO: This is duplicated somewhere
      SafeProjectName = projectName.Replace(rootProjectNamespace, Constants.SafeProjectName)
      RootNamespace = rootProjectNamespace
      SolutionFolderDestinationPath = solutionFolderDestination }

let private getTemplateXmlBuilder hierarchies =
    let rootNode =
        hierarchies
        |> Seq.find (fun h -> h.FullPath.Length = 0)

    // We need to differentiate between the files located
    // inside the root folder and those further down in the hierarchy
    // because there are differences when you generate the XML
    let rootProjectFiles =
        rootNode.Files
        |> Seq.map (fun f -> f.Name)
        // We won't include the original .csproj or .fsproj in the generated project
        |> Seq.filter (fun name -> not <| name.EndsWith("sproj"))
        |> List.ofSeq

    let hierarchies =
        hierarchies
        |> Seq.except [rootNode]
        |> List.ofSeq

    let templateXmlBuilder =
        SingleProjectVsTemplate.getTemplate
            rootProjectFiles
            hierarchies

    templateXmlBuilder

/// <summary>
/// Copies an existing project to a new destination and converts the project into a template.
/// Also generates a .vstemplate file and writes it to the given destination.
/// </summary>
let generateTemplate args =
    copyProjectToTemplateDestination args.ProjFilePath args.PhysicalDestination args.FoldersToIgnore

    let hierarchies =
        buildRelativeHierarchies args.PhysicalDestination args.FoldersToIgnore
        |> List.ofSeq

    replaceNamespacesAndUsingsInHierarchy hierarchies args.RootProjectNamespace |> ignore

    let templateXmlBuilder = getTemplateXmlBuilder hierarchies

    generateTemplateXml
        templateXmlBuilder
        args.ProjFilePath
        args.PhysicalDestination
        args.RootProjectNamespace
        (RelativePath.create args.SolutionDestinationPath)