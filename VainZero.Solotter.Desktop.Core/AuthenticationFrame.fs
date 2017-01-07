namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type AuthenticationFrame
  private
  ( content: ReactiveProperty<IAuthenticationPage>
  , disposable: IDisposable
  ) =
  let dispose () =
    content.Value.Dispose()
    disposable.Dispose()

  private new(applicationAccessToken, initialAction: AuthenticationAction) =
    let disposables =
      new CompositeDisposable()

    let content =
      let emptyPage =
        { new IAuthenticationPage with 
            override this.Authentication = None
            override this.Subscribe(_) = Disposable.Empty
            override this.Dispose() = ()
        }
      new ReactiveProperty<_>(initialValue = emptyPage)

    disposables.Add(content)

    let authenticationActions =
      content.SelectMany(fun actions -> actions :> IObservable<_>)

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

    let solve action =
      let nextPage =
        match action with
        | Login userAccessToken ->
          let authentication =
            Authentication.FromAccessToken(applicationAccessToken, userAccessToken)
          new AuthenticatedPage(authentication) :> IAuthenticationPage
        | Logout ->
          new AuthenticationPage(applicationAccessToken) :> IAuthenticationPage
      content.Value.Dispose()
      content.Value <- nextPage

    authenticationActions.StartWith(initialAction) |> Observable.subscribe solve
    |> disposables.Add

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
