/// <summary>
/// Holds logic for generating the content of a single project .vstemplate file.
/// </summary>
module SingleProjectVsTemplate

open Types
open Utils
open System.IO
open System.Xml

let private rootXmlNamespace = "http://schemas.microsoft.com/developer/vstemplate/2005"

let private initialRootTemplate =
    sprintf @"
<VSTemplate Version=""3.0.0"" xmlns=""%s"" Type=""Project"">
<TemplateData>
<Hidden>true</Hidden>
<SortOrder>1000</SortOrder>
<CreateNewFolder>true</CreateNewFolder>
<LocationField>Enabled</LocationField>
<EnableLocationBrowseButton>true</EnableLocationBrowseButton>
<CreateInPlace>true</CreateInPlace>
</TemplateData>
<TemplateContent>
</TemplateContent>
</VSTemplate>" rootXmlNamespace

let private setProjectRootNode csprojFileName document=
    let projectRootNode =
        createNode document "Project" "" rootXmlNamespace [
            ("TargetFileName", csprojFileName);
            ("File", csprojFileName);
            ("ReplaceParameters", "true") ]
    
    let templateContentNode = document.ChildNodes |> findNode "TemplateContent" |> Option.defaultValue null
    let projectRootNode = templateContentNode.AppendChild(projectRootNode) :?> XmlElement
    projectRootNode


let private setProjectItemNodes (rootProjectFiles:string list) (folderHierarchies:FolderNode list) document (projectRootNode:XmlElement) =
    let getProjectItemNode fileName = createNode document "ProjectItem" fileName rootXmlNamespace [
        ("ReplaceParameters", "true");
        ("TargetFileName", fileName) ]

    let rec toXmlFolderNode (folderNode:FolderNode) : XmlElement =
        let folderXmlNode = createNode document "Folder" "" rootXmlNamespace [
            ("Name", folderNode.Name);
            ("TargetFolderName", folderNode.Name) ]

        List.iter (fun (file:FileInfo) ->
            folderXmlNode.AppendChild(getProjectItemNode file.Name)
            |> ignore
        ) folderNode.Files

        List.iter (fun childFolder ->
            folderXmlNode.AppendChild(toXmlFolderNode childFolder)
            |> ignore
        ) folderNode.ChildFolders

        folderXmlNode

    let projectItemNodes =
        rootProjectFiles
        |> List.map (fun fileName ->
            let projectItemNode = getProjectItemNode fileName
            projectRootNode.AppendChild(projectItemNode)
        )

    let folderNodes =
        folderHierarchies
        |> List.map (fun folderNode ->
            let folderXmlNode = toXmlFolderNode folderNode
            projectRootNode.AppendChild(folderXmlNode)
        )

    document

/// <summary>
/// Generates a single-project .vstemplate contents.
/// Does not write it to disk.
/// </summary>
let getTemplate rootProjectFiles folderHierarchies csprojFileName =
    
    let xmlDocument = toXmlDocument initialRootTemplate

    let documentXml =
        xmlDocument
        |> setProjectRootNode csprojFileName
        |> setProjectItemNodes rootProjectFiles folderHierarchies xmlDocument
        |> fun x -> x.OuterXml
    
    { Xml = documentXml }


