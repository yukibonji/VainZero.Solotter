namespace VainZero.Solotter.Desktop

open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame() =
  let accessToken = AccessToken.Load()

  let loggedIn =
    new ReplaySubject<_>()

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
    page |> visit

  let visitMainPage userAccessToken =
    new MainPage(accessToken.ApplicationAccessToken, userAccessToken) |> visit

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

  member this.Content =
    content
