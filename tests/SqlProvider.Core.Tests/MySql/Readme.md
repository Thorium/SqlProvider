# MySQL - Getting started

There are two ways of using SQLProvider: Either via dynamic package SQLProvider or with pre-defined dependency SQLProvider.MySql or SQLProvider.MySqlConnector.
The dynamic package needs more configuration but has more flexibility if you want manual control over all reference dlls.

# Using SQLProvider.MySql or SQLProvider.MySqlConnector

The SQLProvider.MySql works with official MySQL meanwhile SQLProvider.MySqlConnector works with MariaDB as well.

## Part 1: Create project

```
dotnet new console --language f#
dotnet add package SQLProvider.MySqlConnector
``` 

## Part 2: Source code, build and run

Replace content of Program.fs with this:

```fsharp
open System
open FSharp.Data
open FSharp.Data.Sql
//open FSharp.Data.Sql.MySql
open FSharp.Data.Sql.MySqlConnector

[<Literal>]
let connStr = "Server=localhost;Database=HR;Uid=admin;Pwd=password;Auto Enlist=false; Convert Zero Datetime=true;" // SslMode=none;

type TypeProviderConnection =
    //FSharp.Data.Sql.MySql.SqlDataProvider< 
    FSharp.Data.Sql.MySqlConnector.SqlDataProvider< 
        ConnectionString = connStr,
        DatabaseVendor = Common.DatabaseProviderTypes.MYSQL,
        IndividualsAmount=100,
        UseOptionTypes=FSharp.Data.Sql.Common.NullableColumnType.VALUE_OPTION>

let ctx = TypeProviderConnection.GetDataContext()
let fsts =
    query {
        for c in ctx.Hr.Countries do
        where (c.CountryName.IsSome)
        select c.CountryName.Value
    } |> Seq.head

printfn "%s" fsts
//Console.ReadLine()
```

Run:

```
dotnet build
dotnet run
```

That's it.


# Using the dynamic SQLProvider

This is the other, more complex way.
You can either clone this repository and observe the more complex 
multi-environment version of
SqlProvider.Core.Tests.fsproj and Program.fs (and database scripts at /src/DatabaseScripts/MySql)
or you can start with these tutorials, for Windows, Linux and Mac:

## Part 1: Create project

```
dotnet new console --language f#
dotnet add package SQLProvider
dotnet add package MySqlConnector
dotnet add package System.Buffers
dotnet add package System.Threading.Tasks.Extensions
dotnet add package System.Runtime.InteropServices.RuntimeInformation
dotnet add package System.Data.Common
dotnet restore
code .
```

Create a new folder `libraries` under the root folder of your project.

## Part 2: Reference libraries and project file - On Windows

Locate the corresponding dll files of the packages
(MySqlConnector.dll, System.Buffers.dll, System.Threading.Tasks.Extensions.dll,
System.Runtime.InteropServices.RuntimeInformation.dll and System.Data.Common.dll)
from your NuGet cache `C:\Users\(user)\.nuget\packages\` (or `%USERPROFILE%\.nuget\packages\`)
the corresponding packages, versions `lib\netstandard2.0`, 
or download those from NuGet.org under Manual Download, nuget-packages are just renamed zip-files.

Copy the dlls to the `libraries` folder.

If you use Paket package manager you could simply add a pre-build task for copying dlls, but let's skip it for now.

Add this to the .fsproj-file:

```xml
  <PropertyGroup>
    <FscToolPath>C:\Program Files (x86)\Microsoft SDKs\F#\4.1\Framework\v4.0</FscToolPath>
    <FscToolExe>fsc.exe</FscToolExe>
  </PropertyGroup>
```

## Part 2: Reference libraries and project file - On Linux

Locate the corresponding dll files of the packages
(MySqlConnector.dll, System.Buffers.dll, System.Threading.Tasks.Extensions.dll,
System.Runtime.InteropServices.RuntimeInformation.dll and System.Data.Common.dll)
from your NuGet cache `~/.nuget/packages/`, 
or download those from NuGet.org under Manual Download, nuget-packages are just renamed zip-files.

Copy the dlls to the `libraries` folder.

If you use Paket package manager you could simply add a pre-build task for copying dlls, but let's skip it for now.

Add this to the .fsproj-file:

```xml
  <PropertyGroup>
    <FscToolPath>/usr/bin</FscToolPath>
    <FscToolExe>fsharpc</FscToolExe>
  </PropertyGroup>
```

## Part 2: Reference libraries and project file - On Mac

Locate the corresponding dll files of the packages
(MySqlConnector.dll, System.Buffers.dll, System.Threading.Tasks.Extensions.dll,
System.Runtime.InteropServices.RuntimeInformation.dll and System.Data.Common.dll)
from your NuGet cache `~/.nuget/packages/`, 
or download those from NuGet.org under Manual Download, nuget-packages are just renamed zip-files.

Copy the dlls to the `libraries` folder.

If you use Paket package manager you could simply add a pre-build task for copying dlls, but let's skip it for now.

Add this to the .fsproj-file:

```xml
  <PropertyGroup>
    <FscToolPath>/Library/Frameworks/Mono.framework/Versions/Current/Commands</FscToolPath>
    <FscToolExe>fsharpc</FscToolExe>
  </PropertyGroup>
```

## Part 3: Source code, build and run

Replace content of Program.fs with this:

```fsharp
open System
open FSharp.Data.Sql

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + "/libraries"

[<Literal>]
let connStr =  "Server=localhost;Database=HR;Uid=admin;Pwd=password;Convert Zero Datetime=true;"

type HR = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL, connStr, Owner = "HR", ResolutionPath = resolutionPath>

[<EntryPoint>]
let main argv =
    let ctx = HR.GetDataContext()
    let employeesFirstName = 
        query {
            for emp in ctx.Hr.Employees do
            select (emp.FirstName)
        } |> Seq.head

    printfn "Hello %s!" employeesFirstName
    0 // return an integer exit code
```

Run:

```
dotnet build
dotnet run
```
