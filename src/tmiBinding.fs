module tmi

open Browser.Types
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS

type options =
    abstract debug: bool with get, set

type connection = 
    abstract secure: bool with get, set
    abstract reconnect: bool with get, set

type identity =
    abstract username: string with get, set
    abstract password: string with get, set

type IClientOptions =
    abstract options: options with get, set
    abstract connection: connection with get, set
    abstract identity: identity with get, set
    abstract channels: ResizeArray<string> with get, set

type IClient =
    abstract connect: unit -> Promise<string * int>

    [<Emit("$0.on('message', $1)")>]
    abstract onMessage: ( string -> obj -> string -> bool -> unit) -> unit

    [<Emit("$0.on('connected', $1)")>]
    abstract onConnected:  ( string -> float -> unit) -> unit

    abstract say: string -> string -> unit

    abstract client: IClientOptions -> IClient

[<ImportDefault("tmi.js")>]
let tmi: IClient = jsNative