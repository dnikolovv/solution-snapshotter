module Tests

open Xunit
open System.IO
open Argu
open CLI
open Types

let setupName = "DevAdventures"
let solutionFileName = "MyProject.sln"
let templateWizardName = "MyAmazingWizard"

let extractSetup =
    extractTestProjectSetup
        setupName
        solutionFileName

[<Fact>]
let ``Dev Adventures Project Setup`` () =
    let constructArgs sourceSlnFile destinationFolder =
        let argsList =
            [ Template_Name "MyAmazingSetup";
              Template_Description "It's truly amazing.";
              Root_Project_Namespace "MyProject";
              Folders_To_Ignore ["toBeIgnored"];
              File_Extensions_To_Ignore [".csproj.user"];
              Vsix_Display_Name "MyAmazingSetup Extension";
              Vsix_Description "MyAmazingSetup's description";
              Vsix_More_Info "https://moreinfo.net";
              Vsix_Getting_Started "https://gettingstarted.net";
              Vsix_Price_Category VsixPriceCategory.Free;
              Vsix_Qna_Enable true;
              Vsix_Categories ["other templates"];
              Vsix_Internal_Name "unique-internal-name";
              Template_Wizard_Assembly templateWizardName;
              Vsix_Version "1.0";
              Vsix_Publisher_Full_Name "Dobromir Nikolov";
              Vsix_Publisher_Username "dnikolovv";
              Path_To_Sln (sprintf "%s" sourceSlnFile);
              Destination destinationFolder;
              Vsix_Overview_Md_Path (sprintf "%s" (Path.Combine(Path.GetDirectoryName(sourceSlnFile), "overview.md")))]
        ArgumentParser.Create<Arguments>().ToParseResults(argsList)

    let assertGeneratedTemplateIsCorrect templateFolder vsixFolder args =
        let checkVsTemplate vsTemplate = true
        let checkStructureJson structure = true

        let checkTemplateFolderContents =
            templateFolder
            |> DirectoryInfo
            |> shouldContainFolders
                ["MyProject.Api";
                 "MyProject.Core";
                 "MyProject.Business";
                 "MyProject.Data";
                 "MyProject.Data.EntityFramework";
                 "configuration*"]
            |> shouldNotContainFolders
                ["toBeIgnored*";
                 "toBeAlsoIgnored*"]
            |> shouldContainFileWithPredicate "*.vstemplate" checkVsTemplate
            |> shouldContainFileWithPredicate "structure.json" checkStructureJson
            |> shouldContainFile "Template.zip"
            |> shouldContainFile "*.ico"
            |> ignore

        checkTemplateFolderContents

    generateSetupAndDo
        setupName
        solutionFileName
        constructArgs
        assertGeneratedTemplateIsCorrect