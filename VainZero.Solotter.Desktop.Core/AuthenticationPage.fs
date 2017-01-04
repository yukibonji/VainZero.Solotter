namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open System.Reactive.Subjects
open DotNetKit.Functional.Commands
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type AuthenticationPage(accessToken: ApplicationAccessToken) =
  let twitterCredential =
    let t = accessToken
    Tweetinvi.Models.TwitterCredentials(t.ConsumerKey, t.ConsumerSecret)

  let authenticationContext =
    Tweetinvi.AuthFlow.InitAuthentication(twitterCredential)

  let getPinCodeCommand =
    UnitCommand
      (fun () ->
        let url = authenticationContext.AuthorizationURL
        System.Diagnostics.Process.Start(url) |> ignore
      )

  let pinCode =
    new ReactiveProperty<string>("")

  let authenticateCommand =
    pinCode.Select(fun pinCode -> pinCode.Length = 7).ToReactiveCommand()

  let authenticated =
    new ReplaySubject<_>()

  do
    authenticateCommand.Subscribe
      (fun _ ->
        let pinCode = pinCode.Value
        let credential =
          Tweetinvi.AuthFlow.CreateCredentialsFromVerifierCode(pinCode, authenticationContext)
        authenticated.OnNext(credential)
      ) |> ignore

  member this.GetPinCodeCommand =
    getPinCodeCommand

  member this.PinCode =
    pinCode

  member this.AuthenticateCommand =
    authenticateCommand

  member this.Authenticated =
    authenticated :> IObservable<_>

  interface IPage
