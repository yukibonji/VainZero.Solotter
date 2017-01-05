namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame(accessToken) =
  let loggedIn =
    new ReplaySubject<_>()

  let loggedOut =
    new Subject<_>()

  let content =
    let emptyPage =
      { new IPage with 
          override this.Dispose() = ()
      }
    new ReactiveProperty<_>(initialValue = emptyPage)

  let visit nextPage =
    use previousPage = content.Value
    content.Value <- nextPage

  let visitAuthenticationPage () =
    let page = new AuthenticationPage(accessToken.ApplicationAccessToken)
    page.Authenticated.Subscribe
      (fun token ->
        loggedIn.OnNext(token)
      ) |> ignore
    visit page

  let visitMainPage userAccessToken =
    let page =
      new MainPage(accessToken.ApplicationAccessToken, userAccessToken)
    page.LogoutCommand |> Observable.subscribe
      (fun _ ->
        loggedOut.OnNext(())
      ) |> ignore
    visit page

  // Visit first page.
  do
    match accessToken.UserAccessToken with
    | Some userAccessToken ->
      visitMainPage userAccessToken
    | None ->
      visitAuthenticationPage ()

  let loggedInSubscription =
    loggedIn |> Observable.subscribe
      (fun userAccessToken ->
          let accessToken =
            { accessToken with UserAccessToken = Some userAccessToken }
          accessToken.Save()
          visitMainPage userAccessToken
      )

  let loggedOutSubscription =
    loggedOut |> Observable.subscribe
      (fun _ -> visitAuthenticationPage ())

  let dispose () =
    loggedInSubscription.Dispose()
    loggedOutSubscription.Dispose()
    content.Value.Dispose()
    content.Dispose()
    loggedIn.Dispose()
    loggedOut.Dispose()

  new() =
    new MainFrame(AccessToken.Load())

  member this.Content =
    content

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
