[![Build Status](https://travis-ci.org/dnikolovv/solution-snapshotter.svg?branch=master)](https://travis-ci.org/dnikolovv/solution-snapshotter)

# solution-snapshotter

Take a snapshot of your current solution and instantly export it as a Visual Studio extension!

## snapshot?

In the context of this project, a snapshot means an exported Visual Studio template that represents your current project's state.

If you have your setup ready ([like this, for example](https://github.com/dnikolovv/devadventures-net-core-template/tree/master/source)), you can use this tool to export a ready-to-install Visual Studio extension instantly. The generated extension will contain a template that will initialize your project as you imported it.

## what this project does in pictures

given a project setup that you've put together

![1](example-pictures/step1.PNG)

call `solution-snapshotter`

![2](example-pictures/step2.PNG)
![3](example-pictures/step3.PNG)

and get a generated VSIX project

![4](example-pictures/step4.PNG)

![5](example-pictures/step5.PNG)

you can build it (or your CI agent)

![6](example-pictures/step6.png)

and install it to Visual Studio or ship to the VS Marketplace

![7](example-pictures/step7.PNG)
![8](example-pictures/step8.PNG)

after installing, your template will be available in Visual Studio under the name you've given it

![9](example-pictures/step9.PNG)
![10](example-pictures/step10.PNG)

the projects you create will have the same physical and solution structure as your initial "source"

![11](example-pictures/step11.PNG)

with any extra folders and files included (these could also be docker compose configuration files, for example)

![12](example-pictures/step12.PNG)

and all references being valid (given that your source project was in a good state)

![13](example-pictures/step13.PNG)

## example usage

The [Dev Adventures .NET Core project setup](https://marketplace.visualstudio.com/items?itemName=dnikolovv.dev-adventures-project-setup&ssr=false#overview) is generated using this tool.

The input is the [source folder](https://github.com/dnikolovv/devadventures-net-core-template/tree/master/source) and the output is this:

![https://devadventures.net/wp-content/uploads/2018/06/template-in-vs.png](https://devadventures.net/wp-content/uploads/2018/06/template-in-vs.png)

> [Click to check it out on the VS marketplace.](https://marketplace.visualstudio.com/items?itemName=dnikolovv.dev-adventures-project-setup)

## why?

Have you ever found yourself setting up the same structure, with the same stack, over and over again? I know I have.

Yes, you can create a Visual Studio template and have your structure built-in. However, if you attempt to do that yourself, you'll quickly find out that it is **way** more complicated than it should be.

### multi-project templates are not trivial

There's a built-in `Export Template` functionality inside Visual Studio. Sadly, it only works for solutions that contain a single project or projects inside a solution that don't reference anything else. Otherwise, the exported template will not compile due to broken references.

Most often you'll have multiple assemblies in your setup. Combining those into a single template means you'll be following [long tutorials](https://mentormate.com/blog/process-improvement-tools-create-multilayered-project-visual-studio-ide-seconds/) and getting intimately familiar with the [multi-project vstemplate format](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-create-multi-project-templates?view=vs-2019#).

### distributing your template is also not trivial

After you've successfully built your template, you'll quickly find out that distributing it is a pain. It's a `.zip` file that you have to copy into a very specific Visual Studio folder. If you want to be able to ship your setup as a Visual Studio extension, you'll have to spare a couple more hours setting up stuff.

### maintenance is a nightmare

Whether you chose the `.zip` or the Visual Studio extension path, maintenance is the same.

When you export a project as a template, it's no longer something that you can compile and run. Any changes you want to make mean that you'll be digging inside plain text files (or full of compile-time error files). If you want to test your changes, you'll have to start up a Visual Studio instance and run your extension inside it. Great if you want to spend 30 minutes updating a NuGet package version and a few variable names.

### physical folders? oh no

When setting up a new project, you probably use a clean and tidy physical folder structure. Perhaps something like this?

```
├───src
│   ├───MyProject.Api
│   ├───MyProject.Business
│   ├───MyProject.Core
│   ├───MyProject.Data
│   └───MyProject.Data.EntityFramework
└───tests
    └───MyProject.Business.Tests
```

Well, forget about it because **multi-project templates do not support custom physical folder structures**. Whether you want it or not, your project structure will look like this:

```
├───MyProject.Api
├───MyProject.Business
├───MyProject.Core
├───MyProject.Data
├───MyProject.Data.EntityFramework
├───MyProject.Business.Tests
```

Of course, there's a workaround. If you want a custom physical structure, you can plug in during the template creation using the [IWizard](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.templatewizard.iwizard?view=visualstudiosdk-2017) interface and his friendly cousins [\_Solution](https://docs.microsoft.com/en-us/dotnet/api/envdte._solution?view=visualstudiosdk-2017), [Solution2](https://docs.microsoft.com/en-us/dotnet/api/envdte80.solution2?view=visualstudiosdk-2017), [Solution3](https://docs.microsoft.com/en-us/dotnet/api/envdte90.solution3?view=visualstudiosdk-2017) and [Solution4](https://docs.microsoft.com/en-us/dotnet/api/envdte100.solution4?view=visualstudiosdk-2017)! F\*ck you, naming conventions!

### extra files? double oh no

Besides a tidy folder structure, you most likely have some extra files that are not included in your .NET projects. For example, I like to have a `configuration` folder that contains some shared configuration files ([like that one](https://github.com/dnikolovv/devadventures-net-core-template/tree/master/source/src/configuration)).

How do you include such a folder in your multi-project template?

You can't.

There's no built-in mechanism to do that. If you want to have extra files, you need to include them into the VSIX installer and extract them during the template creation using custom logic placed inside the [IWizard](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.templatewizard.iwizard?view=visualstudiosdk-2017) interface.

Don't squander your time with all this nonsense. Let `solution-snapshotter` do the heavy lifting.

## usage

`solution-snapshotter` supports two ways of providing arguments - inline or through a config file.

```console
> solution-snapshotter.exe --help

USAGE: solution-snapshotter.exe [--help] [from-file <filePath>] [<subcommand> [<options>]]

SUBCOMMANDS:

    inline <options>      Provide the arguments inline (as CLI arguments).

    Use 'solution-snapshotter.exe <subcommand> --help' for additional information.

OPTIONS:

    from-file <filePath>  Provide a .config file that holds the arguments.
    --help                display this list of options.
```

You can view the CLI documentation using the `--help` argument. Optional arguments are wrapped in square brackets.

```console
> solution-snapshotter.exe inline --help

USAGE: solution-snapshotter.exe inline [--help] --template-name <name> --template-description <description> --template-wizard-assembly <assemblyName> --root-project-namespace <namespace> --path-to-sln <path> --vsix-version <version>
                                       --vsix-publisher-full-name <publisher> --vsix-publisher-username <publisher> --vsix-display-name <displayName> --vsix-description <description> --vsix-more-info <info>
                                       --vsix-getting-started <gettingStarted> --vsix-overview-md-path <path> --vsix-price-category <free|trial|paid> --vsix-qna-enable <qnaEnabled> --vsix-internal-name <name> [--vsix-repo=<repo>]
                                       [--vsix-custom-icon=<iconPath>] [--vsix-custom-package-guid=<guid>] [--vsix-custom-project-guid=<guid>] [--vsix-custom-id=<id>] [--custom-template-icon=<iconPath>] [--destination=<destination>]
                                       [--vsix-tags=<tags>] [--vsix-categories [<categories>...]] [--folders-to-ignore [<folderNames>...]] [--file-extensions-to-ignore [<extensions>...]]

OPTIONS:

    --template-name <name>
                          Sets the name of the template. This will be shown in Visual Studio.
    --template-description <description>
                          Sets the description of the template. This will be shown in Visual Studio.
    --template-wizard-assembly <assemblyName>
                          Sets the name of the generated VSIX assembly.
    --root-project-namespace <namespace>
                          Sets the project root namespace (e.g. MyProject).
    --path-to-sln <path>  Sets the path to the source project .sln file.
    --vsix-version <version>
                          Sets the version of the generated VSIX assembly.
    --vsix-publisher-full-name <publisher>
                          The publisher's full name on the marketplace (e.g. John Smith).
    --vsix-publisher-username <publisher>
                          The publisher's username on the marketplace (e.g. jsmith12).
    --vsix-display-name <displayName>
                          Sets the display name of the generated VSIX assembly.
    --vsix-description <description>
                          Sets the description of the generated VSIX assembly.
    --vsix-more-info <info>
                          Sets the 'More Info' tag of the generated VSIX assembly.
    --vsix-getting-started <gettingStarted>
                          Sets the 'Getting Started' tag of the generated VSIX assembly.
    --vsix-overview-md-path <path>
                          The path to the overview.md file that will be shown in the marketplace.
    --vsix-price-category <free|trial|paid>
                          The price category of the extension.
    --vsix-qna-enable <qnaEnabled>
                          Whether or not your extension should have a Q&A section.
    --vsix-internal-name <name>
                          The extension's marketplace internal name.
    --vsix-repo=<repo>    The repository url for the extension.
    --vsix-custom-icon=<iconPath>
                          Sets a custom icon for the VSIX. This appears in the Extensions tab.
    --vsix-custom-package-guid=<guid>
                          Optionally sets a custom package GUID for the generated VSIX assembly.
    --vsix-custom-project-guid=<guid>
                          Optionally sets a custom project GUID for the generated VSIX assembly.
    --vsix-custom-id=<id> Optionally sets a custom id for the generated VSIX assembly.
    --custom-template-icon=<iconPath>
                          Optionally sets a custom template icon.
    --destination=<destination>
                          Sets a custom destination folder.
    --vsix-tags=<tags>    The tags for the VSIX.
    --vsix-categories [<categories>...]
                          The categories for the VSIX in the marketplace.
    --folders-to-ignore [<folderNames>...]
                          Which folders in the source project folder to ignore (e.g. bin, obj, lib, etc.)
    --file-extensions-to-ignore [<extensions>...]
                          Which file extensions in the source folder to ignore (e.g. .min.js)
    --help                display this list of options.
```

When supplying a `.config` file, use the same arguments, but instead of dashes, use spaces for the key values.

Example:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="path to sln" value=".\sample-app\MyProject.sln" />
    <add key="root project namespace" value="MyProject" />
    <add key="template name" value="A random template" />
    <add key="template description" value="Just testing stuff..." />
    <add key="custom template icon" value=".\template-icon.ico" />
    <add key="destination" value="template" />
    <add key="vsix custom icon" value="logo-only.png" />
    <add key="vsix version" value="0.1" />
    <add key="vsix publisher full name" value="John Smith" />
    <add key="vsix publisher username" value="jsmith12" />
    <add key="vsix display name" value="An irrelevant extension" />
    <add key="vsix description" value="It's an example" />
    <add key="template wizard assembly" value="SomeRandomWizard" />
    <add key="vsix more info" value="https://moreinfo.net" />
    <add key="folders to ignore" value="bin packages obj lib .git .vs node_modules" />
    <add key="vsix getting started" value="https://github.com/dnikolovv" />
    <add key="vsix repo" value="https://github.com/dnikolovv" />
    <add key="vsix qna enable" value="true" />
    <add key="vsix price category" value="free" />
    <add key="vsix internal name" value="unique-internal-name" />
    <add key="vsix categories" value="visual studio extensions,other templates" />
    <add key="vsix overview md path" value="overview.md" />
    <add key="file extensions to ignore" value=".min.js" />
    <add key="vsix tags" value="template,generation" />
  </appSettings>
</configuration>
```

```console
> solution-snapshotter.exe from-file input.config

Successfully converted <YourProject>.sln to a template!
You'll find the zipped template at 'C:\some-path\Template.zip' and VSIX project at 'C:\some-path\SomeRandomWizard.csproj'.
```
