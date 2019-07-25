/// <summary>
/// Holds logic for generating the content of a multi-project .vstemplate file.
/// </summary>
module MultiProjectVsTemplate

open System.Xml
open Types
open Utils
open System

let private rootXmlNamespace = "http://schemas.microsoft.com/developer/vstemplate/2005"

let private getInitialTemplateString (args:MultiProjectTemplateArgs) =
    sprintf @"
<VSTemplate Version=""2.0.0"" Type=""ProjectGroup"" xmlns=""%s"">
<TemplateData>
<Name>%s</Name>
<DefaultName>ProjectName</DefaultName>
<Description>%s</Description>
<ProjectType>CSharp</ProjectType>
<Icon>%s</Icon>
</TemplateData>
<TemplateContent>
<ProjectCollection>
</ProjectCollection>
</TemplateContent>
<WizardExtension>
<Assembly>%s, Version=1.0.0.0, Culture=Neutral, PublicKeyToken=null</Assembly>
<FullClassName>%s.Wizard</FullClassName>
</WizardExtension>
</VSTemplate>" rootXmlNamespace args.TemplateName args.TemplateDescription args.TemplateIcon args.TemplateWizardAssembly args.TemplateWizardAssembly

let private toLinkXmlNode document (link:ProjectTemplateLink) =
    let node =
        createNode
            document
            "ProjectTemplateLink"
            link.VsTemplatePath
            rootXmlNamespace
            [("ProjectName", link.SafeProjectName); ("CopyParameters", "true")]
    document, node

let private generateProjectXmlLinkNodes document links (projectCollectionNode:XmlNode) =
    List.iter (fun link ->
        let (_, node) = toLinkXmlNode document link
        projectCollectionNode.AppendChild(node) |> ignore
    ) links
    document, projectCollectionNode

/// <summary>
/// Generates a multi-project .vstemplate file contents.
/// Does not write it to disk.
/// </summary>
let getTemplate (args:MultiProjectTemplateArgs) : MultiProjectVsTemplate =
    let initialXmlString = getInitialTemplateString args
    let document = toXmlDocument initialXmlString
    let projectCollectionNode = document.ChildNodes |> findNode "ProjectCollection"

    match projectCollectionNode with
    | Some node ->
        let (document, _) =
            node
            |> generateProjectXmlLinkNodes document args.Projects
        { Xml = document.OuterXml
          LinkElements = args.Projects
          InnerProjectsTemplateInfo = [] }
    | None -> raise (InvalidOperationException "Could not find the ProjectCollection node. Most likely the initial template string is broken.")