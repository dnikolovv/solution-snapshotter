namespace UtilTypes

open System
open System.IO
    
type ExistingDirPath = private ExistingDirPath of string

type ExistingDir = private ExistingDir of DirectoryInfo

type UnexistingPath = private UnexistingPath of string

type ExistingFilePath = private ExistingFilePath of string

type RelativePath = private RelativePath of string

type ExistingFile = private ExistingFile of FileInfo

type String50 = private String50 of string

[<RequireQualifiedAccess>]
module String50 =

    let create (str:string) =
        if str.Length > 50 then
            raise (ArgumentException (sprintf "Expected '%s' to be less than 50 characters, but was %d." str str.Length))
        String50 str

    let value (String50 str) = str

[<RequireQualifiedAccess>]
module ExistingDir =

    let create (dir:DirectoryInfo) =
        if not <| dir.Exists then
            raise (new ArgumentException (sprintf "%s should've been an existing directory, but wasn't." dir.FullName))
        else
            ExistingDir dir

    let fromString str = create (DirectoryInfo str)

    let fromPath (ExistingDirPath path) = fromString path

    let fromUnexistingPath path =
        Directory.CreateDirectory(path) |> ignore
        fromString path

    let value (ExistingDir dir) = dir

    let map f (ExistingDir dir) = f dir

[<RequireQualifiedAccess>]
module ExistingDirPath =

    let create path =
        if not <| Directory.Exists(path) then
            raise (new ArgumentException (sprintf "%s should've been an existing path, but wasn't." path))
        else
            ExistingDirPath path

    let length (ExistingDirPath path) = path.Length

    let createFromNonExistingSafe path =
        Directory.CreateDirectory(path) |> ignore
        create path

    let createFromNonExistingOrFail path =
        if Directory.Exists(path) then
            raise (new ArgumentException (sprintf "%s should've been an unexisting path, but wasn't." path))
        createFromNonExistingSafe path

    let combineWith pathOrFileName (ExistingDirPath path) =
        Path.Combine(path, pathOrFileName)

    let getFirstFile pattern (ExistingDirPath path) =
        let dirInfo = DirectoryInfo path
        dirInfo.GetFiles(pattern).[0]

    let value (ExistingDirPath path) = path

    let map f (ExistingDirPath path) = f path

[<RequireQualifiedAccess>]
module ExistingFile =
    
    let create (file:FileInfo) =
        if not <| file.Exists then
            raise (new ArgumentException (sprintf "%s should've been an existing file, but wasn't." file.FullName))
        else
            ExistingFile file

    let value (ExistingFile file) = file

[<RequireQualifiedAccess>]
module ExistingFilePath =
    
    let create str =
        if not <| File.Exists(str) then
            raise (new ArgumentException (sprintf "%s should've led to an existing file, but didn't." str))
        else
            ExistingFilePath str

    let getFileName (ExistingFilePath path) = Path.GetFileName(path)

    let getDirectoryName (ExistingFilePath path) = Path.GetDirectoryName(path)

    let value (ExistingFilePath path) = path

    let checkIfExisting path = path |> create |> value

[<RequireQualifiedAccess>]
module RelativePath =
    
    let create (str:string) =
        if Path.IsPathRooted str then
            raise (new ArgumentException (sprintf "%s should've been a relative path, but wasn't." str))
        else
            RelativePath str

    let isATopLevelPath (RelativePath path) =
        let trimmed = path.TrimStart('\\', '/')
        trimmed.Contains("\\") ||
        trimmed.Contains("/")

    let isRoot (RelativePath path) = path.Length = 0

    let value (RelativePath path) = path

[<RequireQualifiedAccess>]
module Path =
    
    let createExisting str =
        if not <| Directory.Exists(str) then
            raise (new ArgumentException (sprintf "%s should've been a valid path, but wasn't." str))
        else
            ExistingDirPath str

    let createUnexisting str = 
        if Directory.Exists(str) then
            raise (new ArgumentException (sprintf "%s should've been an unexisting path, but wasn't." str))
        else
            UnexistingPath str

    let asExistingFilePath str =
        if not <| File.Exists(str) then
            raise (new ArgumentException (sprintf "%s should've been an existing file, but wasn't." str))
        else
            ExistingFilePath str

    let createRelative (str:string) =
        if Path.IsPathRooted str then
            raise (new ArgumentException (sprintf "%s should've been a relative path, but wasn't." str))
        else
            RelativePath str