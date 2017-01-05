namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame(accessToken) =
  let content =
    let emptyPage =
      { new IPage with 
          override this.Subscribe(_) = Disposable.Empty
          override this.Dispose() = ()
      }
    new ReactiveProperty<_>(initialValue = emptyPage)

  let authenticationActions =
    content.SelectMany(fun actions -> actions :> IObservable<_>)

  let visit nextPage =
    use previousPage = content.Value
    content.Value <- nextPage

  let visitAuthenticationPage () =
    new AuthenticationPage(accessToken.ApplicationAccessToken) |> visit

  let visitMainPage userAccessToken =
    new MainPage(accessToken.ApplicationAccessToken, userAccessToken) |> visit

  let subscription =
    authenticationActions |> Observable.subscribe
      (function
        | Login userAccessToken ->
          visitMainPage userAccessToken
        | Logout ->
          visitAuthenticationPage ()
      )

  let dispose () =
    subscription.Dispose()
    content.Value.Dispose()
    content.Dispose()

  // Visit first page.
  do
    match accessToken.UserAccessToken with
    | Some userAccessToken ->
      visitMainPage userAccessToken
    | None ->
      visitAuthenticationPage ()

  new() =
    new MainFrame(AccessToken.Load())

  member this.Content =
    content

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
