(*** hide ***)
#r "../../../bin/lib/netstandard2.0/FSharp.Data.SqlProvider.Common.dll"
#r "../../../bin/lib/netstandard2.0/FSharp.Data.SqlProvider.dll"
(*** hide ***)
let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"/../../files/sqlite"
(*** hide ***)
let [<Literal>] connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\..\northwindEF.db;Version=3;Read Only=false;FailIfMissing=True;"

(*** hide ***)
(*

# SQL Provider Basics

The SQL provider is an erasing type provider which enables you to instantly
connect to a variety of database sources in the IDE and explore them in a
type-safe manner without the inconvenience of a code-generation step.

SQL Provider supports the following database types:

* [MSSQL](mssql.html)
* [MSSQL SSDT](mssqlssdt.html)
* [Oracle](oracle.html)
* [SQLite](sqlite.html)
* [PostgreSQL](postgresql.html)
* [MySQL](mysql.html)
* [MsAccess](msaccess.html)
* [ODBC](odbc.html) (_Experimental_, only supports SELECT & MAKE)

After you have installed the nuget package or built the type provider assembly
from source, you should reference the assembly either as a project reference
or by using an F# interactive script file.

```
// when using the SQLProvider in a script file (.fsx), the file needs to be referenced
// using F#'s `#r` command:
#r "../../packages/SQLProvider.1.0.1/lib/net40/FSharp.Data.SqlProvider.dll"
// whether referencing in a script or added to a project as an assembly
// reference, the library needs to be opened
```
*)
open FSharp.Data.Sql

(**


To use the type provider, you must first create a type alias.

In this declaration, you can pass various pieces of information known
as static parameters to initialize properties such as the connection string
and database vendor type that you are connecting to.

In the following examples, a SQLite database will be used.  You can read in
more detail about the available static parameters in other areas of the
documentation.
*)

type sql  = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE,
                connectionString,
                SQLiteLibrary=Common.SQLiteLibrary.SystemDataSQLite,
                ResolutionPath = resolutionPath,
                CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >

(**
Now we have a type ``sql``representing the SQLite database provided in
the connectionString parameter.  To start exploring the database's
schema and reading its data, you create a *DataContext* value.
*)

let ctx = sql.GetDataContext()

(**
There is also a GetReadOnlyDataContext() if you want Command Query Separation (CQS),
to segregate your read queries and your read+write transaction commands on the type level.
The dataContext returned from that method is missing SubmitUpdates, so it doesn't
have a way to commit transactions. If you want to pass your normal dataContext
to a method using read-only dataContext, you can do it via .AsReadOnly()
which doesn't make your context immutable, it just gives you the compatible type.
For convenience, they both share the same database entity types.

If you want to use non-literal connectionString at runtime (e.g. encrypted production
passwords), you can pass your runtime connectionString parameter to GetDataContext:
*)

let connectionString2 = "(insert runtime connection here)"
let ctx2 = sql.GetDataContext connectionString2

(**

When you press ``.`` on ``ctx``, intellisense will display a list of properties
representing the available tables and views within the database.

In the simplest case, you can treat these properties as sequences that can
be enumerated.
*)

let customers = ctx.Main.Customers |> Seq.toArray
(**
This is the equivalent of executing a query that selects all rows and
columns from the ``[main].[customers]`` table.

Notice the resulting type is an array of ``[Main].[Customers]Entity``.  These
entities will contain properties relating to each column name from the table.
*)

let firstCustomer = customers.[0]
let name = firstCustomer.ContactName
(**
Each property is correctly typed depending on the database column
definitions.  In this example, ``firstCustomer.ContactName`` is a string.

Most databases support some comments/descriptions/remarks to
tables and columns for documentation purposes. These descriptions are fetched
to tooltips for the tables and columns.
*)

(**
## Constraints and Relationships

A typical relational database will have many connected tables and views
through foreign key constraints.  The SQL provider can show you these
constraints on entities.  They appear as properties named the same as the
constraint in the database.

You can gain access to these child or parent entities by simply enumerating
the property in question.
*)

let orders = firstCustomer.``main.Orders by CustomerID`` |> Seq.toArray

(**
``orders`` now contain all the orders belonging to firstCustomer. You will
see the orders type is an array of ``[Main].[Orders]Entity`` indicating the
resulting entities are from the ``[main].[Orders]`` table in the database.
If you hover over ``FK_Orders_0_0``, intellisense will display information
about the constraint in question, including the names of the tables involved
and the key names.

Behind the scenes, the SQL provider has automatically constructed and executed
a relevant query using the entity's primary key.


## Basic Querying
The SQL provider supports LINQ queries using F#'s *query expression* syntax.
*)

let customersQuery =
    query {
        for customer in ctx.Main.Customers do
            select customer
    }
    |> Seq.toArray

(**

Support also async queries

 *)

let customersQueryAsync =
    query {
        for customer in ctx.Main.Customers do
            select customer
    }
    |> Seq.executeQueryAsync


(**
The above example is identical to the query that was executed when
``ctx.[main].[Customers] |> Seq.toArray`` was evaluated.

You can extend this basic query to filter criteria by introducing
one or more *where* clauses
*)

let filteredQuery =
    query {
        for customer in ctx.Main.Customers do
            where (customer.ContactName = "John Smith")
            select customer
    }
    |> Seq.toArray

let multipleFilteredQuery =
    query {
        for customer in ctx.Main.Customers do
            where ((customer.ContactName = "John Smith" && customer.Country = "England") || customer.ContactName = "Joe Bloggs")
            select customer
    }
    |> Seq.toArray

(**
The SQL provider will accept any level of nested complex conditional logic
in the *where* clause.

To access related data, you can either enumerate directly over the constraint
property of an entity, or you can perform an explicit join.
*)

let automaticJoinQuery =
    query {
        for customer in ctx.Main.Customers do
            for order in customer.``main.Orders by CustomerID`` do
                where (customer.ContactName = "John Smith")
                select (customer, order)
    }
    |> Seq.toArray

let explicitJoinQuery =
    query {
        for customer in ctx.Main.Customers do
            join order in ctx.Main.Orders on (customer.CustomerId = order.CustomerId)
            where (customer.ContactName = "John Smith")
            select (customer, order)
    }
    |> Seq.toArray

(**
These queries have identical results; the only difference is that one
requires explicit knowledge of which tables join where and how, and the other doesn't.
You might have noticed the select expression has now changed to (customer, order).
As you may expect, this will return an array of tuples where the first item
is a ``[Main].[Customers]Entity`` and the second a ``[Main].[Orders]Entity``.
Often, you will not be interested in selecting entire entities from the database.
Changing the select expression to use the entities' properties will cause the
SQL provider to select only the columns you have asked for, which is an
important optimization.
*)

let ordersQuery =
    query {
        for customer in ctx.Main.Customers do
            for order in customer.``main.Orders by CustomerID`` do
                where (customer.ContactName = "John Smith")
                select (customer.ContactName, order.OrderDate, order.ShipAddress)
    }
    |> Seq.toArray

(**
The results of this query will return the name, order date and shipping address
only.  By doing this, you no longer have access to entity types.
The SQL provider supports various other query keywords and features that you
can read about elsewhere in this documentation.


## Individuals
The SQL provider has the ability via intellisense to navigate the actual data
held within a table or view. You can then bind that data as an entity to a value.
*)

let BERGS = ctx.Main.Customers.Individuals.BERGS
(**
Every table and view has an ``Individuals`` property. When you press the dot on
this property, intellisense will display a list of the data in that table,
using whatever the primary key is as the text for each one.
In this case, the primary key for ``[main].[Customers]`` is a string, and I
have selected one named BERGS. You will see the resulting type is
``[main].[Customers]Entity``.

The primary key is not usually very useful for identifying data, however, so
in addition to this, you will see a series of properties named "As X" where X
is the name of a column in the table.
When you press "." on one of these properties, the data is re-projected to you
using both the primary key and the text of the column you have selected.
*)

let christina = ctx.Main.Customers.Individuals.``As ContactName``.``BERGS, Christina Berglund``

(**
## DataContext
You should create and use one data context as long as it has the parameters you need.
An example of when to use multiple data contexts is when you need to pass different
connection strings to connect to different instances of the same database,
e.g. to copy data between them.

The connection itself is not stored and reused with an instance of the data context.
The data context creates a connection when you execute a query or when you call
`SubmitUpdates()`. In terms of transactions, the data context object tracks (full)
entities that were retrieved using it via queries or `Individuals` and manage their
states. Upon calling `SubmitUpdates()`, all entities modified/created that belong to
that data context are wrapped in a single transaction scope. Then a connection
is created and thus enlisted into the transaction.



**#Important**:
The database schema (SQLProvider's understanding of the structure of tables, columns, names, types, etc of your database
- a "snapshot" if you will) is cached **lazily** while you use it.

What does that entail?

A.  Once SQLProvider gets a "mental model" of your database (the schema),
    that is what is used for any intellisense/completion suggestions for the rest of your IDE session.

    This is a fantastic feature because it means you're not assaulting your database with a
    new "What are you like?" query on EVERY SINGLE KEYSTROKE.

    But what if the database changes? SQLProvider will NOT see your change because its source of truth is
    that locally cached schema snapshot it took at right when it started, and that snapshot will persist until
    one of 2 things happens:

    1.  A restart of your Editor/IDE.
        The database is queried right when SQLProvider starts up, so you
        could certainly force a refresh by restarting.

    2.  Forced clearing of the local database schema cache.
        If SQLProvider is currently able to communicate with the database,
        you can force the local cache to clear, to be invalidated and refreshed by
        by using what are called `Design Time Commands`, specifically the
        `ClearDatabaseSchemaCache` method.

        You're probably thinking: "Ok, fine, that sounds good! How do I do that though?"

        Just as SQLProvider can interact at compile time with the structure of data in your
        database through your editor's completion tooling
        (intellisense, language server protocol completion suggestions, etc),
        you can also interact with SQLProvider at compile time the exact same way.

        SQLProvider provides methods under the DataContext you get from your type alias,
        and they actually show up as ``Design Time Commands`` in the completion.

        Select that, and then "dot into" it afterwards, then under that is ClearDatabaseSchemaCache.
        Then, after that typing in a "." will actually RUN the command, thereby clearing the cache.

 B. LAZY evaluation means that where you save the database schema in your code matters.
    Do not call the "Design Time Command" SaveContextSchema at the top of your code. FSharp is evaluated
    from top to bottom, so if you call SaveContextSchema at the top before you ask for specific columns in your code,
    you will not get a schema that reflects your needs.

    sql.GetDataContext(cs).``Design Time Commands``.SaveContextSchema  // put a "." at the end to call the command at compile time.


How fast is SQLProvider?
------------------------

You may wonder if all this magic comes with a huge performance cost. However, when working with databases,
your network connection to SQL-database is typically the bottleneck, not your processor speed.
That's why SQLProvider short-circuits and optimises your queries as much as possible.

There is a performance-test project in this repo. Here is a sample run:

- BenchmarkDotNet v0.13.12
- .NET 8
- Laptop, Intel i9 13th Gen on Windows 11
- Microsoft SQL Server on local computer


| Method                | rowsReturned | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |------------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|-----------:|------------:|
| **FirstNamesToList**      | **25**           | **1.098 ms** | **0.0218 ms** | **0.0312 ms** |  **1.00** |    **0.00** |  **27.3438** |  **11.7188** |  **357.65 KB** |        **1.00** |
| FirstNamesToListAsync | 25           | 1.123 ms | 0.0223 ms | 0.0435 ms |  1.03 |    0.05 |  27.3438 |  11.7188 |  358.94 KB |        1.00 |
|                       |              |          |           |           |       |         |          |          |            |             |
| **FirstNamesToList**      | **2500**         | **2.678 ms** | **0.0531 ms** | **0.1073 ms** |  **1.00** |    **0.00** | **226.5625** | **164.0625** | **2812.34 KB** |        **1.00** |
| FirstNamesToListAsync | 2500         | 2.697 ms | 0.0608 ms | 0.1685 ms |  1.01 |    0.06 | 226.5625 | 171.8750 | 2813.27 KB |        1.00 |

*)
