
// To run the tests in this file:
//
// Technique 1: Compile VisualFSharp.Unittests.dll and run it as a set of unit tests
//
// Technique 2:
//
//   Enable some tests in the #if EXE section at the end of the file, 
//   then compile this file as an EXE that has InternalsVisibleTo access into the
//   appropriate DLLs.  This can be the quickest way to get turnaround on updating the tests
//   and capturing large amounts of structured output.
(*
    cd Debug\net40\bin
    .\fsc.exe --define:EXE -r:.\Microsoft.Build.Utilities.Core.dll -o VisualFSharp.Unittests.exe -g --optimize- -r .\FSharp.LanguageService.Compiler.dll  -r .\FSharp.Editor.dll -r nunit.framework.dll ..\..\..\tests\service\FsUnit.fs ..\..\..\tests\service\Common.fs /delaysign /keyfile:..\..\..\src\fsharp\msft.pubkey ..\..\..\vsintegration\tests\unittests\CompletionProviderTests.fs 
    .\VisualFSharp.Unittests.exe 
*)
// Technique 3: 
// 
//    Use F# Interactive.  This only works for FSharp.Compiler.Service.dll which has a public API

// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
module Microsoft.VisualStudio.FSharp.Editor.Tests.Roslyn.DocumentHighlightsServiceTests

open System
open System.Threading

open NUnit.Framework

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.VisualStudio.FSharp.Editor

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

let filePath = "C:\\test.fs"

let internal options = { 
    ProjectFileName = "C:\\test.fsproj"
    ProjectFileNames =  [| filePath |]
    ReferencedProjects = [| |]
    OtherOptions = [| |]
    IsIncompleteTypeCheckEnvironment = true
    UseScriptResolutionRules = false
    LoadTime = DateTime.MaxValue
    UnresolvedReferences = None
    ExtraProjectInfo = None
}

let private getSpans (sourceText: SourceText) (caretPosition: int) =
    let documentId = DocumentId.CreateNewId(ProjectId.CreateNewId())
    FSharpDocumentHighlightsService.GetDocumentHighlights(FSharpChecker.Instance, documentId, sourceText, filePath, caretPosition, [], options, 0)
    |> Async.RunSynchronously

let private span sourceText isDefinition (startLine, startCol) (endLine, endCol) =
    let range = Range.mkRange filePath (Range.mkPos startLine startCol) (Range.mkPos endLine endCol)
    { IsDefinition = isDefinition
      TextSpan = CommonRoslynHelpers.FSharpRangeToTextSpan(sourceText, range) }

[<Test>]
let ShouldHighlightAllSimpleLocalSymbolReferences() =
    let fileContents = """
    let foo x = 
        x + x
    let y = foo 2
    """
    let sourceText = SourceText.From(fileContents)
    let caretPosition = fileContents.IndexOf("foo") + 1
    let spans = getSpans sourceText caretPosition
    
    let expected =
        [| span sourceText true (2, 8) (2, 11)
           span sourceText false (4, 12) (4, 15) |]
    
    Assert.AreEqual(expected, spans)

[<Test>]
let ShouldHighlightAllQualifiedSymbolReferences() =
    let fileContents = """
    let x = System.DateTime.Now
    let y = System.DateTime.MaxValue
    """
    let sourceText = SourceText.From(fileContents)
    let caretPosition = fileContents.IndexOf("DateTime") + 1
    let documentId = DocumentId.CreateNewId(ProjectId.CreateNewId())
    
    let spans = getSpans sourceText caretPosition
    
    let expected =
        [| span sourceText false (2, 19) (2, 27)
           span sourceText false (3, 19) (3, 27) |]
    
    Assert.AreEqual(expected, spans)

    let caretPosition = fileContents.IndexOf("Now") + 1
    let documentId = DocumentId.CreateNewId(ProjectId.CreateNewId())
    let spans = getSpans sourceText caretPosition
    let expected = [| span sourceText false (2, 28) (2, 31) |]
    
    Assert.AreEqual(expected, spans)