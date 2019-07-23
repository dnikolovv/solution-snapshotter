module SingleProjectTemplateGenerator

open Types
open System.IO
open Utils
open Paths

let private copyProjectToTemplateDestination (csprojPath:ExistingFilePath) (templateDestination:ExistingDirPath) (foldersToIgnore:List<string>) =
    let projectDirectory = ExistingFilePath.getDirectoryName csprojPath
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
        |> List.map (replaceNamespacesAndUsingsInFile rootProjectNamespace)

    let replacedFolders =
        node.ChildFolders
        |> List.map (replaceNamespacesAndUsingsInNode rootProjectNamespace)

    { node with
        Files = replacedFiles
        ChildFolders = replacedFolders }

let private replaceNamespacesAndUsingsInHierarchy nodes rootProjectNamespace =
    List.ofSeq nodes
    |> List.map (replaceNamespacesAndUsingsInNode rootProjectNamespace)

let private generateTemplateXml (templateXmlBuilder: string -> SingleProjectVsTemplate) csprojPath (destination:ExistingDirPath) rootProjectNamespace solutionFolderDestination =
    let csprojFileName = ExistingFilePath.getFileName csprojPath

    let template = templateXmlBuilder csprojFileName

    let projectName = csprojFileName |> cutEnd ".csproj"
    let templateFileName = sprintf "%s.vstemplate" projectName
    
    let destination = ExistingDirPath.value destination
    let templateDestination =
        Path.Combine(destination, templateFileName)

    File.WriteAllText(templateDestination, template.Xml)

    { VsTemplatePath = ExistingFilePath.create templateDestination
      OriginalCsprojPath = csprojPath
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
        // We won't include the original .csproj in the generated project
        |> Seq.filter (fun name -> not <| name.EndsWith("csproj"))
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
    copyProjectToTemplateDestination args.CsprojPath args.PhysicalDestination args.FoldersToIgnore

    let hierarchies =
        buildRelativeHierarchies args.PhysicalDestination args.FoldersToIgnore
        |> List.ofSeq

    replaceNamespacesAndUsingsInHierarchy hierarchies args.RootProjectNamespace |> ignore

    let templateXmlBuilder = getTemplateXmlBuilder hierarchies

    generateTemplateXml
        templateXmlBuilder
        args.CsprojPath
        args.PhysicalDestination
        args.RootProjectNamespace
        (RelativePath.create args.SolutionDestinationPath)