module Chatbot.Extension

open Fable.Core
open Fable.Core.JS
open Fable.Import
open Fable.Core.JsInterop
open vscode.Vscode
open tmi
open System

type MaybeBuilder() =
    member __.Bind(value, binder) =
        Option.bind binder value
    
    member __.Return(value) =
        Some value

    member __.ReturnFrom(value: 'a option) =
        value

    member __.Zero() = None

let maybe = MaybeBuilder()

[<Import("*", from="dotenv" )>]
let dotenv: obj = jsNative

module Config =
    [<Emit("process.env[$0] ? process.env[$0] : ''")>] 
    let variable (key: string) : string =
        jsNative

let hoverCommand = "vscode.executeHoverProvider"

let executeHover line col =
    let activeEditor = window.activeTextEditor
    let uri = activeEditor |> Option.map (fun ae -> box ae.document.uri)
    let pos = Some(box (vscode.vscode.Position.Create(line, col)))
    let arguments = [|uri; pos |] |> ResizeArray
    promise {let! result = commands.executeCommand<Hover array>(hoverCommand, arguments)
             return result }

let tryGetHoverMarkdown (results: Hover array option) =
    maybe { let! hovers = results
            let! top = hovers |> Array.tryHead
            let! contents = top.contents |> Seq.tryHead
            let markdown =
                match contents with
                | U4.Case1 (mds: MarkdownString) -> mds.value
                | U4.Case2 two -> two
                | U4.Case3 three -> three.ToString()
                | U4.Case4 four -> four.ToString()
            return markdown
          }

let activate (context: ExtensionContext) =
    dotenv?config()
    console.log("Chatbot is alive!")

    let options =
        jsOptions(fun (o: IClientOptions) ->
            o.channels <- ResizeArray [Config.variable "channel"]
            o.connection <- jsOptions(fun (connection:connection) ->
                connection.reconnect <- true
                connection.secure <- true)
            o.identity <- jsOptions(fun (identity: identity) ->
                identity.password <- Config.variable "oauth"
                identity.username <- Config.variable "username")
            o.options <- jsOptions(fun (options: options) -> options.debug <- true))

    let (|Hover|_|) (input: string) =
        match input.Substring(0, 6) with
            | "!hover" ->
                let subs = input.Substring(6).Split([|" "|], StringSplitOptions.RemoveEmptyEntries)
                match subs with
                | [|line; column|] -> 
                    Some (float line, float column)
                | _ -> None
            | _ -> None

    let client = tmi.client options
    client.connect() |> ignore
    client.onMessage(fun host tags message self ->
        if not self then
            let messagePromise =
                match message with
                | Hover (line, column) ->
                    promise { let! results = executeHover line column
                              let results = tryGetHoverMarkdown results
                              results 
                              |> Option.iter (client.say (Config.variable "channel")) }
                    |> Some
                | _ -> None

            messagePromise
            |> Option.iter (fun mp -> promise.Run mp |> ignore)
)

    
    