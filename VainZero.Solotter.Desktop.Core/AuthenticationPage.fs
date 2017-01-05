namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open System.Reactive.Subjects
open DotNetKit.Functional.Commands
open DotNetKit.FSharp
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
    pinCode.Select(String.IsNullOrEmpty >> not).ToReactiveCommand()

  let authenticate () =
    let pinCode = pinCode.Value
    let context = authenticationContext
    let credential =
      Tweetinvi.AuthFlow.CreateCredentialsFromVerifierCode(pinCode, context)
    if credential |> isNull then
      todo "invalid pincode?"
    else
      {
        AccessToken =
          credential.AccessToken
        AccessSecret =
          credential.AccessTokenSecret
      } |> Login

  let authenticated =
    authenticateCommand
      .Select(fun _ -> authenticate ())
      .Replay()

  let subscription =
    authenticated.Connect()

  let dispose () =
    subscription.Dispose()
    pinCode.Dispose()

  member this.GetPinCodeCommand =
    getPinCodeCommand

  member this.PinCode =
    pinCode

  member this.AuthenticateCommand =
    authenticateCommand

  member this.Authenticated =
    authenticated :> IObservable<_>

  member this.Dispose() =
    dispose ()

  interface IObservable<AuthenticationAction> with
    override this.Subscribe(observer) =
      authenticated.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface IAuthenticationPage
