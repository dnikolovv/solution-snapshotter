module CLI

open Argu
open Types

type Arguments =
    | [<Mandatory>] Template_Name of name:string
    | [<Mandatory>] Template_Description of description:string
    | [<Mandatory>] Template_Wizard_Assembly of assemblyName:string
    | [<Mandatory>] Root_Project_Namespace of ``namespace``:string
    | [<Mandatory>] Path_To_Sln of path:string
    | [<Mandatory>] Vsix_Version of version:string
    | [<Mandatory>] Vsix_Publisher_Full_Name of publisher:string
    | [<Mandatory>] Vsix_Publisher_Username of publisher:string
    | [<Mandatory>] Vsix_Display_Name of displayName:string
    | [<Mandatory>] Vsix_Description of description:string
    | [<Mandatory>] Vsix_More_Info of info:string
    | [<Mandatory>] Vsix_Getting_Started of gettingStarted:string
    | [<Mandatory>] Vsix_Overview_Md_Path of path:string
    | [<Mandatory>] Vsix_Price_Category of VsixPriceCategory
    | [<Mandatory>] Vsix_Qna_Enable of qnaEnabled:bool
    | [<Mandatory>] Vsix_Internal_Name of name:string
    | [<EqualsAssignment>] Vsix_Repo of repo:string
    | [<EqualsAssignment>] Vsix_Custom_Icon of iconPath:string
    | [<EqualsAssignment>] Vsix_Custom_Package_Guid of guid:string
    | [<EqualsAssignment>] Vsix_Custom_Project_Guid of guid:string
    | [<EqualsAssignment>] Vsix_Custom_Id of id:string
    | [<EqualsAssignment>] Custom_Template_Icon of iconPath:string
    | [<EqualsAssignment>] Destination of destination:string
    | [<EqualsAssignment>] Vsix_Tags of tags: string
    | Vsix_Categories of categories:string list
    | Folders_To_Ignore of folderNames:string list
    | File_Extensions_To_Ignore of extensions: string list
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Template_Name _ -> "Sets the name of the template. This will be shown in Visual Studio."
            | Template_Description _ -> "Sets the description of the template. This will be shown in Visual Studio."
            | Template_Wizard_Assembly _ -> "Sets the name of the generated VSIX assembly."
            | Root_Project_Namespace _ -> "Sets the project root namespace (e.g. MyProject)."
            | Path_To_Sln _ -> "Sets the path to the source project .sln file."
            | Vsix_Version _ -> "Sets the version of the generated VSIX assembly."
            | Vsix_Publisher_Full_Name _ -> "The publisher's full name on the marketplace (e.g. John Smith)."
            | Vsix_Publisher_Username _ -> "The publisher's username on the marketplace (e.g. jsmith12)."
            | Vsix_Display_Name _ -> "Sets the display name of the generated VSIX assembly."
            | Vsix_Description _ -> "Sets the description of the generated VSIX assembly."
            | Vsix_More_Info _ -> "Sets the 'More Info' tag of the generated VSIX assembly."
            | Vsix_Getting_Started _ -> "Sets the 'Getting Started' tag of the generated VSIX assembly."
            | Vsix_Custom_Icon _ -> "Sets a custom icon for the VSIX. This appears in the Extensions tab."
            | Vsix_Custom_Package_Guid _ -> "Optionally sets a custom package GUID for the generated VSIX assembly."
            | Vsix_Custom_Project_Guid _ -> "Optionally sets a custom project GUID for the generated VSIX assembly."
            | Vsix_Custom_Id _ -> "Optionally sets a custom id for the generated VSIX assembly."
            | Custom_Template_Icon _ -> "Optionally sets a custom template icon."
            | Folders_To_Ignore _ -> "Which folders in the source project folder to ignore (e.g. bin, obj, lib, etc.)"
            | Destination _ -> "Sets a custom destination folder."
            | File_Extensions_To_Ignore _ -> "Which file extensions in the source folder to ignore (e.g. .min.js)"
            | Vsix_Categories _ -> "The categories for the VSIX in the marketplace."
            | Vsix_Repo _ -> "The repository url for the extension."
            | Vsix_Internal_Name _ -> "The extension's marketplace internal name."
            | Vsix_Qna_Enable _ -> "Whether or not your extension should have a Q&A section."
            | Vsix_Price_Category _ -> "The price category of the extension."
            | Vsix_Tags _ -> "The tags for the VSIX."
            | Vsix_Overview_Md_Path _ -> "The path to the overview.md file that will be shown in the marketplace."

and InputOptionsArguments =
    | [<CliPrefix(CliPrefix.None)>] Inline of ParseResults<Arguments>
    | [<CliPrefix(CliPrefix.None)>] From_File of filePath:string
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Inline _ -> "Provide the arguments inline (as CLI arguments)."
            | From_File _ -> "Provide a .config file that holds the arguments."