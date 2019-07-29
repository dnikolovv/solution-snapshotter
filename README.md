[![Build Status](https://travis-ci.org/dnikolovv/solution-snapshotter.svg?branch=master)](https://travis-ci.org/dnikolovv/solution-snapshotter)

# solution-snapshotter

Take a snapshot of your current solution and instantly export it as a Visual Studio extension!

## snapshot?

In the context of this project, a snapshot means an exported Visual Studio template that represents your current project's state.

If you have your setup ready ([like this, for example](https://github.com/dnikolovv/devadventures-net-core-template/tree/master/source)), you can use this tool to export a ready-to-install Visual Studio extension instantly. The generated extension will contain a template that will initialize your project as you imported it.

## example

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

### physical folders, oh no

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
|───MyProject.Data.EntityFramework
|───MyProject.Business.Tests
```

Of course, there's a workaround. If you want a custom physical structure, you can plug in during the template creation using the [IWizard](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.templatewizard.iwizard?view=visualstudiosdk-2017) interface and his friendly cousins [_Solution](https://docs.microsoft.com/en-us/dotnet/api/envdte._solution?view=visualstudiosdk-2017), [Solution2](https://docs.microsoft.com/en-us/dotnet/api/envdte80.solution2?view=visualstudiosdk-2017), [Solution3](https://docs.microsoft.com/en-us/dotnet/api/envdte90.solution3?view=visualstudiosdk-2017) and [Solution4](https://docs.microsoft.com/en-us/dotnet/api/envdte100.solution4?view=visualstudiosdk-2017)! F*ck you, naming conventions!

### extra files? double oh no

Besides a tidy folder structure, you most likely have some extra files that are not included in your .NET projects. For example, I like to have a `configuration` folder that contains some shared configuration files ([like that one](https://github.com/dnikolovv/devadventures-net-core-template/tree/master/source/src/configuration)).

How do you include such a folder in your multi-project template?

You can't.

There's no built-in mechanism to do that. If you want to have extra files, you need to include them into the VSIX installer and extract them during the template creation using custom logic placed inside the [IWizard](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.templatewizard.iwizard?view=visualstudiosdk-2017) interface.
