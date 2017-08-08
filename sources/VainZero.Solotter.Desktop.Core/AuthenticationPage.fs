namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open System.Windows.Input
open DotNetKit.Functional.Commands
open DotNetKit.FSharp
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type AuthenticationPage(accessToken: ApplicationAccessToken, notifier: Notifier) =
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
      notifier.NotifyInfo("Incorrect PinCode.")
      Observable.Never()
    else
      {
        AccessToken =
          credential.AccessToken
        AccessSecret =
          credential.AccessTokenSecret
      } |> Login |> Observable.Return

  let authenticated =
    authenticateCommand
      .SelectMany(fun _ -> authenticate ())
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
    authenticateCommand :> ICommand

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

  interface IAuthenticationPage with
    override this.Authentication =
      None
