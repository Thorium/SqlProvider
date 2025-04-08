module MsSqlSsdt.Tests.TypeAnnotationTests
open NUnit.Framework
open FSharp.Data.Sql
open FSharp.Data.Sql.Ssdt
open FSharp.Data.Sql.Ssdt.DacpacParser

let sql = """SELECT dbo.Projects.Name AS ProjectName, dbo.Projects.[ProjectNumber] /* int null */, dbo.Projects.ProjectType,
dbo.Projects.LOD, dbo.Projects.[Division], dbo.Projects.IsActive, dbo.ProjectTaskCategories.Name AS Category, 
dbo.ProjectTasks.Name AS TaskName, dbo.ProjectTasks.CostCode, dbo.ProjectTasks.Sort, dbo.TimeEntries.Username, dbo.TimeEntries.Email, dbo.TimeEntries.Date, 
COALESCE (dbo.TimeEntries.Hours, 0) AS Hours /*FLOAT NOT NULL*/,
dbo.TimeEntries.Created/* DatetimeOffset Not Null*/, dbo.TimeEntries.Updated, dbo.Users.EmployeeId"""

[<Test>]
let ``Should find view annotation``() =    
    let results = RegexParsers.parseViewAnnotations sql
    printfn "Results: %A" results

    Assert.AreEqual(3, results.Length, "Annotation result count.")

    // #1: dbo.Projects.[ProjectNumber] /* int null */
    // #2: Hours /*FLOAT NOT NULL*/
    // #3: Created/* DatetimeOffset Not Null*/

    let expected = [
        { Column = "ProjectNumber"; DataType = "int"; Nullability = ValueSome "null" }
        { Column = "Hours"; DataType = "FLOAT"; Nullability = ValueSome "NOT NULL"  }
        { Column = "Created"; DataType = "DatetimeOffset"; Nullability = ValueSome "Not Null" }
    ]
    Assert.AreEqual(expected, results)

[<Test>]
let ``Should find table column annotation``() =
    let colExpr = "[LineTotal] AS (isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0)) /* MONEY NOT NULL */ ),"
    let result = RegexParsers.parseTableColumnAnnotation "LineTotal" colExpr
    let expected = { Column = "LineTotal"; DataType = "MONEY"; Nullability = ValueSome "NOT NULL" }
    Assert.AreEqual(expected, result.Value)

[<Test>]
let ``Verify int annotation in vProductAndDescription Three``() =
    let schema = 
        UnzipTests.dacPacPath
        |> UnzipTests.extractModelXml
        |> DacpacParser.parseXml

    let view = schema.Tables |> Array.find (fun t -> t.Name = "vProductAndDescription")
    let c = view.Columns |> Array.find (fun c -> c.Name = "Three")
    Assert.AreEqual("INT", c.DataType)

