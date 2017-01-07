namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open DotNetKit.FSharp
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type AuthenticationFrame
  private
  ( content: IReadOnlyReactiveProperty<IAuthenticationPage>
  , disposable: IDisposable
  ) =
  let dispose () =
    disposable.Dispose()

  private new(applicationAccessToken, initialAction: AuthenticationAction) =
    let disposables =
      new CompositeDisposable()

    let pageDisposable =
      new SerialDisposable()
      |> tap disposables.Add

    let authenticationActions =
      new Subject<AuthenticationAction>()
      |> tap disposables.Add

    let saveAccessToken action =
      let userAccessToken =
        match action with
        | Login userAccessToken ->
          Some userAccessToken
        | Logout ->
          None
      let accessToken =
        {
          ApplicationAccessToken =
            applicationAccessToken
          UserAccessToken =
            userAccessToken
        }
      accessToken.Save()

    authenticationActions |> Observable.subscribe saveAccessToken
    |> disposables.Add

    let pageFromAction action =
      match action with
      | Login userAccessToken ->
        let authentication =
          Authentication.FromAccessToken(applicationAccessToken, userAccessToken)
        new AuthenticatedPage(authentication) :> IAuthenticationPage
      | Logout ->
        new AuthenticationPage(applicationAccessToken) :> IAuthenticationPage

    let content =
      authenticationActions
        .StartWith(initialAction)
        .Select(pageFromAction)
        .Do(fun page ->
          pageDisposable.Disposable <-
            StableCompositeDisposable.Create
              ( page
              , page |> Observable.subscribe authenticationActions.OnNext
              )
          )
        .ToReadOnlyReactiveProperty()
      |> tap disposables.Add

    new AuthenticationFrame(content, disposables)

  new(accessToken: AccessToken) =
    let applicationAccessToken =
      accessToken.ApplicationAccessToken
    let initialAction =
      match accessToken.UserAccessToken with
      | Some userAccessToken ->
        Login userAccessToken
      | None ->
        Logout
    new AuthenticationFrame(applicationAccessToken, initialAction)

  new() =
    new AuthenticationFrame(AccessToken.Load())

  member this.Content =
    content

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
