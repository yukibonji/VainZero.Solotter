namespace VainZero.Solotter.Desktop

open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame() =
  let accessToken = AccessToken.Load()

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

  do
    loggedIn |> Observable.subscribe
      (fun userAccessToken ->
          let accessToken =
            { accessToken with UserAccessToken = Some userAccessToken }
          accessToken.Save()
          visitMainPage userAccessToken
      )
      |> ignore

  do
    loggedOut |> Observable.subscribe
      (fun _ ->
        visitAuthenticationPage ()
      ) |> ignore

  member this.Content =
    content
