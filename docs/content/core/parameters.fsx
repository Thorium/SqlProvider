(*** hide ***)
#I @"../../files/sqlite"
(*** hide ***)
#I "../../../bin/lib/netstandard2.0"
(*** hide ***)
#r "../../../packages/scripts/MySqlConnector/lib/net45/MySqlConnector.dll"
(*** hide ***)
#r "FSharp.Data.SqlProvider.Common.dll"
#r "FSharp.Data.SqlProvider.dll"
open FSharp.Data.Sql

(*** hide ***)
[<Literal>]
let connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"/../../../tests/SqlProvider.Tests/scripts/northwindEF.db;Version=3"

(*** hide ***)
[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + @"/../../../tests/SqlProvider.Tests/libs"

(*** hide ***)
type sqlType =
    SqlDataProvider<
        Common.DatabaseProviderTypes.SQLITE,
        connectionString,
        ResolutionPath = resolutionPath
    >

(**


# SQL Provider Static Parameters

## Global parameters

These are the "common" parameters used by all SqlProviders.

All static parameters must be known at compile time. For strings, this can be
achieved by adding the `[<Literal>]` attribute if you are not passing it inline.

### ConnectionString

This is the connection string commonly used to connect to a database server
instance. Please review the documentation on your desired database type to learn
more.
*)

[<Literal>]
let sqliteConnectionString =
    "Data Source=" + __SOURCE_DIRECTORY__ + @"\northwindEF.db;Version=3"

(**
### ConnectionStringName

Instead of storing the connection string in the source code / `fsx` script, you
can store values in the `App.config` file:

```xml
<connectionStrings>
  <add name="MyConnectionString"
   providerName="System.Data.ProviderName"
   connectionString="Valid Connection String;" />
</connectionStrings>
```

Another usually easier option is to give a runtime connection string as a parameter for the `.GetDataContext(...)` method.

In your source file:
*)

let connexStringName = "MyConnectionString"

(**
### DatabaseVendor

Select enumeration from `Common.DatabaseProviderTypes` to specify which database
type the provider will be connecting to.
*)

[<Literal>]
let dbVendor = Common.DatabaseProviderTypes.SQLITE

(**
### ResolutionPath

A third-party driver is required when using database vendors other than SQL Server, Access and ODBC.
This parameter should point to an absolute or relative directory where the
relevant assemblies are located. Please review at the database vendor-specific page for more details.
*)

[<Literal>]
let resolutionPath =
    __SOURCE_DIRECTORY__ + @"..\..\..\files\sqlite"

(**

The resolution path(s) (as can be semicolon separated if any) should point to the
database driver files and their reference assemblies) that work on design-time.
So, depending on your IDE, you probably want there .NET Standard 2.0 (or 2.1) versions
and not the latest .NET runtime, even when you'd target your final product to the latest .NET.

#### Note on .NET 5 PublishSingleFile and ResolutionPath

If you are publishing your app using .NET 5's PublishSingleFile mode, the driver will
be loaded from the bundle itself rather than from a separate file on the drive.
The ResolutionPath parameter will not work for the published app, nor will the automatic
assembly resolution implemented within SQLProvider.

SQLProvider attempts to load the assembly from the AppDomain in such a case. This means
that your driver's assembly must be loaded by your application for SQLProvider to find
it. To do so, use the types of your driver before calling the `.GetDataContext(...)`
method, such as in this example, using MySqlConnector. The specific type you refer
to does not matter.
*)

typeof<MySqlConnector.Logging.MySqlConnectorLogLevel>.Assembly |> ignore
let ctx = sqlType.GetDataContext()

(**
### IndividualsAmount

Number of instances to retrieve using the [individuals](individuals.html) feature.
Default is 1000.
*)

let indivAmt = 500


(**
### UseOptionTypes

If set to FSharp.Data.Sql.Common.NullableColumnType.OPTION, all nullable fields will be represented by F# option types.  If NO_OPTION, nullable
fields will be represented by the default value of the column type - this is important because
the provider will return 0 instead of null, which might cause problems in some scenarios.

The third option is VALUE_OPTION, where nullable fields are represented by ValueOption struct.

*)
[<Literal>]
let useOptionTypes = FSharp.Data.Sql.Common.NullableColumnType.OPTION

(**
### ContextSchemaPath

Defining `ContextSchemaPath` and placing a file with schema information according to the definition
enables offline mode that can be useful when the database is unavailable or slow to connect or access.
Schema information file can be generated by calling design-time method `SaveContextSchema` under `Design Time Commands`:

```fsharp
ctx.``Design Time Commands``.SaveContextSchema
```

This method doesn't affect runtime execution. Since SQLProvider loads schema information lazily,
calling `SaveContextSchema` only saves the portion of the database schema sufficient to compile
queries referenced in the scope of the current solution or script. Therefore, it is recommended that
it be executed after the successful build of the whole solution. Type the method name with parentheses. If you then
type a dot (.), you should see a tooltip with information about when the schema was last saved. Once the schema
is saved, the outcome of the method execution is stored in memory so the file will not be overwritten.
In case the database schema changes and the schema file must be updated, remove the outdated file, reload
the solution and retype or uncomment a call to `SaveContextSchema` to regenerate the schema file.

There is a tool method FSharp.Data.Sql.Common.OfflineTools.mergeCacheFiles to merge multiple files together.

*)

[<Literal>]
let contextSchemaPath =
    __SOURCE_DIRECTORY__ + @".\sqlite.schema"


(**
## Platform Considerations

### MSSQL

TableNames to filter the amount of tables.

### Oracle

TableNames to filter the number of tables and an Owner.

#### Owner (Used by Oracle, MySQL and PostgreSQL)

This has different meanings when running queries against different database vendors.

For PostgreSQL, this sets the schema name to which the target tables belong. It can also be a list separated by spaces, newlines, commas or semicolons.

For MySQL, this sets the database name (Or schema name, which is the same thing for MySQL). It can also be a list separated by spaces, newlines, commas or semicolons.

For Oracle, this sets the owner of the scheme.



### SQLite

The additional [SQLiteLibrary parameter](sqlite.html#SQLiteLibrary) can be used to specify
which SQLite library to load.

### PostgreSQL

There are no extra parameters.

### MySQL

There are no extra parameters.

### ODBC

There are no extra parameters.
*)


(**
###Example
It is recommended to use named static parameters in your type provider definition so

*)
type sql = SqlDataProvider<
            ConnectionString = sqliteConnectionString,
            DatabaseVendor = dbVendor,
            ResolutionPath = resolutionPath,
            UseOptionTypes = useOptionTypes
          >

(**

# SQL Provider Data Context Parameters

Besides the static parameters the `.GetDataContext(...)` method has optional parameters:

* connectionString - The database connection string on runtime.
* resolutionPath - The location to look for dynamically loaded assemblies containing database vendor-specific connections and custom types
* transactionOptions - TransactionOptions for the transaction created on SubmitChanges.
* commandTimeout - SQL command timeout. Maximum time for single SQL-command in seconds.
* selectOperations - Execute select-clause operations in SQL database rather than .NET-side.

*)
