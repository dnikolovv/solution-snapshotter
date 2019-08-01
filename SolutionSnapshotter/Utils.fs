module Utils

open System.IO
open System.Xml
open System.Linq
open System.Reflection
open System.Collections.Generic
open System
open Types

[<AutoOpen>]
module Collections =
    
    /// <summary>
    /// Reverses a seq.
    /// </summary>
    let reverse = System.Linq.Enumerable.Reverse

[<AutoOpen>]
module Directories =
    open UtilTypes
    
    let rec private getDirectoryParentHierarchyRec (directory:DirectoryInfo) (parentUpperBoundaryPath:string option) (hierarchy:List<DirectoryInfo>) =
        if directory = null || directory.Parent = null
            then []
        else
            let directoryParent = directory.Parent
    
            if (parentUpperBoundaryPath.IsSome && directoryParent <> null && directoryParent.FullName = parentUpperBoundaryPath.Value)
                then []
            else
                hierarchy.Add(directoryParent)
                // TODO: This may use some immutability
                ignore <| getDirectoryParentHierarchyRec directoryParent parentUpperBoundaryPath hierarchy
                List.ofSeq hierarchy
    
    let private getDirectoryParentHierarchy directory parentUpperBoundaryPath =
        List.ofSeq (getDirectoryParentHierarchyRec directory parentUpperBoundaryPath (new List<_>()))

    let private shouldBeIgnored foldersToIgnore (directory:DirectoryInfo) =
        if List.contains directory.Name foldersToIgnore
            then true
        else
            let parentHierarchy = getDirectoryParentHierarchy directory None

            parentHierarchy
            |> List.exists (fun d -> foldersToIgnore.Contains(d.Name))
    
    let rec private getChildFolders (directory:DirectoryInfo) = 
        seq {
            for childDirectory in directory.EnumerateDirectories() do
                // TODO: Duplication with the buildHierarchies function
                yield {
                    FullPath = childDirectory.FullName
                    ChildFolders = getChildFolders childDirectory |> List.ofSeq
                    Files = childDirectory.GetFiles() |> List.ofSeq
                    Name = childDirectory.Name
                }
        }

    let private buildHierarchiesFromDirs directories : FolderNode seq =
        seq {
            for (directory:DirectoryInfo) in directories do
                yield {
                    FullPath = directory.FullName
                    Files = directory.GetFiles() |> List.ofSeq
                    ChildFolders = getChildFolders directory |> List.ofSeq
                    Name = directory.Name
                }
        }

    let replaceInFile filePath phrasesToReplace =
        let mutable text = File.ReadAllText filePath

        List.iter (fun (toSearch:string, newText:string) ->
            text <- text.Replace(toSearch, newText)
        ) phrasesToReplace

        File.WriteAllText(filePath, text)
        FileInfo filePath
        

    let buildHierarchies (rootPath:ExistingDirPath) foldersToIgnore =
        let directory =
            ExistingDir.fromPath rootPath
            |> ExistingDir.value
            
        let subDirectories =
            directory.GetDirectories()
            |> Seq.filter (shouldBeIgnored foldersToIgnore >> not)
            |> List.ofSeq

        buildHierarchiesFromDirs ([directory] @ subDirectories)

    let scanForFiles rootPath (patterns:string list) =
        if (not <| Directory.Exists rootPath) then
            raise (new InvalidOperationException "Tried to scan an invalid directory.")

        let directory = DirectoryInfo rootPath

        patterns
        |> List.collect (fun pattern ->
            directory.EnumerateFiles(pattern, SearchOption.AllDirectories)
            |> List.ofSeq)

    let moveFolderContents (targetPath:ExistingDirPath) (source:ExistingDirPath) =
        let source = source |> ExistingDirPath.value
        let target = targetPath |> ExistingDirPath.value
        
        Directory.EnumerateFiles(source)
        |> Seq.iter (fun file ->
            let dest = Path.Combine(target, Path.GetFileName(file))
            File.Move(file, dest))

        Directory.EnumerateDirectories(source)
        |> Seq.iter (fun dir ->
            let dest = Path.Combine(target, Path.GetFileName(dir))
            Directory.Move(dir, dest))

        Directory.Delete(source)
        targetPath

    /// <summary>
    /// Scans the folder structure recursively.
    /// The bottom is a folder that contains a file with the specified pattern.
    /// Also scans the root folder you give it.
    /// </summary>
    let scanForDirectoriesThatDoNotContain rootPath patterns foldersToIgnore =
        if not <| Directory.Exists(rootPath) then
            raise (new InvalidOperationException "Tried to scan an invalid directory.")

        let directoriesToIgnore =
            scanForFiles rootPath patterns
            |> Seq.map (fun f -> f.DirectoryName)
            |> List.ofSeq

        if directoriesToIgnore |> List.contains rootPath then
            []
        else

        let rootDirectoryInfo = DirectoryInfo rootPath

        let subdirectories =
            rootDirectoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories)
            |> List.ofSeq

        (rootDirectoryInfo :: subdirectories)
        |> Seq.filter (fun directory ->
            // First we filter by the directory itself
            if shouldBeIgnored foldersToIgnore directory then
                false
            else
                // Then we check whether its parent hierarchy contains a folder that should be ignored
                let parentHierarchy = getDirectoryParentHierarchy directory (Some rootPath)

                let isOneOfIgnored =
                    parentHierarchy
                    |> List.exists (fun x -> directoriesToIgnore.Contains(x.FullName))

                let containsUnwantedFiles (dir:DirectoryInfo) =
                    patterns
                    |> Seq.collect (fun p ->
                        dir.GetFiles(p))
                    |> Seq.isEmpty
                    |> not

                let shouldBeSelected =
                    not <| isOneOfIgnored &&
                    not <| containsUnwantedFiles directory

                shouldBeSelected
        )
        |> List.ofSeq

    /// <summary>
    /// Replaces the given phrases inside the file names of the given directory.
    /// Also includes files contained in subfolders.
    /// </summary>
    let replaceInFileNames (directory:DirectoryInfo) textToSearch newText =
        directory.EnumerateFiles(sprintf "*%s*.*" textToSearch, SearchOption.AllDirectories)
        |> Seq.map (fun file ->
            let destination = Path.Combine(file.DirectoryName, file.Name.Replace(textToSearch, newText))
            file.MoveTo(destination)
        )
        |> List.ofSeq

    /// <summary>
    /// Copies a directory's contents recursively.
    /// </summary>
    let rec copyDirectory sourceDirName (destDirName:ExistingDirPath) copySubDirs foldersToIgnore fileExtensionsToIgnore =
        let directoryInfo = DirectoryInfo sourceDirName
        
        if not <| directoryInfo.Exists then
            raise (DirectoryNotFoundException sourceDirName)

        let files =
            directoryInfo.EnumerateFiles()
            |> Seq.filter (fun f -> not <| List.contains f.Extension fileExtensionsToIgnore)

        Seq.iter (fun (file:FileInfo) ->
            let tempPath = destDirName |> ExistingDirPath.combineWith file.Name
            ignore <| file.CopyTo(tempPath, false)
        ) files

        if copySubDirs then
            let directories =
                directoryInfo.GetDirectories()
                |> Seq.filter (shouldBeIgnored foldersToIgnore >> not)

            Seq.iter (fun (subDirectory:DirectoryInfo) ->
                let tempPath =
                    destDirName
                    |> ExistingDirPath.combineWith subDirectory.Name
                    |> ExistingDirPath.createFromNonExistingSafe

                copyDirectory subDirectory.FullName tempPath copySubDirs foldersToIgnore fileExtensionsToIgnore
            ) directories

[<AutoOpen>]
module String =

    /// <summary>
    /// If the string ends with the given piece, cuts it off.
    /// Else returns the string as it was.
    /// </summary>
    let cutEnd (toCut:string) (string:string) =
        if not <| string.EndsWith(toCut) then string
        else string.Substring(0, string.Length - toCut.Length)
    
    /// <summary>
    /// If the string starts with the given piece, cuts it off.
    /// Else returns the string as it was.
    /// </summary>
    let cutStart (toCut:string) (string:string) =
        if not <| string.StartsWith(toCut) then string
        else string.Substring(toCut.Length, string.Length - toCut.Length)
    
    /// <summary>
    /// Trims a given char off the end of a string.
    /// </summary>
    let trimEnd (char:char) (string:string) =
        string.TrimEnd(char)

    /// <summary>
    /// Trims a given char off the start of a string.
    /// </summary>
    let trimStart (char:char) (string:string) =
        string.TrimStart(char)

    /// <summary>
    /// Converts a string to a stream.
    /// </summary>
    let toStream (string: string) =
        let stream = new MemoryStream()
        let writer = new StreamWriter(stream)
        writer.Write(string)
        writer.Flush()
        stream.Position <- int64(0)
        stream

[<AutoOpen>]
module Xml =

    /// <summary>
    /// Searches for a node by name.
    /// </summary>
    let rec findNode name (nodes:XmlNodeList) : XmlNode option =
        let mutable nodeFound = None
        for node in nodes do
            if node.Name = name then nodeFound <- Some node
            elif node.HasChildNodes then
                let node = node.ChildNodes |> findNode name
                if node.IsSome then nodeFound <- node
        nodeFound
    
    /// <summary>
    /// Converts a string to System.Xml.XmlDocument.
    /// </summary>
    let toXmlDocument (string: string) =
        let document = XmlDocument()
        use stream = toStream string
        document.Load(stream)
        document
    
    /// <summary>
    /// Creates a node with the given parameters.
    /// Requires an xml document.
    /// </summary>
    let createNode (document:XmlDocument) name innerText xmlNamespace attributes =
        let node = document.CreateNode(XmlNodeType.Element, name, xmlNamespace) :?> XmlElement
        List.iter (fun (key, value) -> node.SetAttribute(key, value)) attributes
        node.InnerText <- innerText
        node

    /// <summary>
    /// Converts a relative path to a rooted.
    /// </summary>
    let toRootedPath (path:string) =
        if not <| Path.IsPathRooted path then
            Path.GetFullPath(path)
        else path

    /// <summary>
    /// Retrieves an embedded resource stream.
    /// </summary>
    let getEmbeddedResourceStream resourceName =
        let stream =
            Assembly
             .GetExecutingAssembly()
             .GetManifestResourceStream(sprintf "%s.%s" Constants.AssemblyName resourceName)

        if isNull stream then
            raise (InvalidOperationException (sprintf "Tried to retrieve an unexisting embedded resource with name '%s'" resourceName))

        stream