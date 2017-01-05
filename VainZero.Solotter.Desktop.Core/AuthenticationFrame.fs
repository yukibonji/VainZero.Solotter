namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type AuthenticationFrame(accessToken: AccessToken) =
  let content =
    let emptyPage =
      { new IAuthenticationPage with 
          override this.UserAccessToken = None
          override this.Subscribe(_) = Disposable.Empty
          override this.Dispose() = ()
      }
    new ReactiveProperty<_>(initialValue = emptyPage)

  let applicationAccessToken =
    accessToken.ApplicationAccessToken

  let initialAction =
    match accessToken.UserAccessToken with
    | Some userAccessToken ->
      Login userAccessToken
    | None ->
      Logout

  let accessToken () =
    { accessToken with UserAccessToken = content.Value.UserAccessToken }

  let authenticationActions =
    content.SelectMany(fun actions -> actions :> IObservable<_>)

  let solve action =
    use previousPage = content.Value
    content.Value <-
      match action with
      | Login userAccessToken ->
        new AuthenticatedPage(applicationAccessToken, userAccessToken) :> IAuthenticationPage
      | Logout ->
        new AuthenticationPage(applicationAccessToken) :> IAuthenticationPage
    (accessToken ()).Save()

  let subscription =
    authenticationActions.StartWith(initialAction) |> Observable.subscribe solve

  let dispose () =
    subscription.Dispose()
    content.Value.Dispose()
    content.Dispose()

  new() =
    new AuthenticationFrame(AccessToken.Load())

  member this.Content =
    content

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
