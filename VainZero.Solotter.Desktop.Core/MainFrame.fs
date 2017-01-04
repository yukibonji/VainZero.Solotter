namespace VainZero.Solotter.Desktop

open System.Reactive.Subjects
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame() =
  let accessToken = AccessToken.Load()

  let loggedIn =
    new ReplaySubject<_>()

  let initialPage =
    match accessToken.UserAccessToken with
    | Some token ->
      new MainPage(accessToken.ApplicationAccessToken, token) :> IPage
    | None ->
      let page = new AuthenticationPage(accessToken.ApplicationAccessToken)
      page.Authenticated.Subscribe
        (fun token ->
          loggedIn.OnNext(token)
        ) |> ignore
      page :> IPage

  let content =
    new ReactiveProperty<_>(initialValue = initialPage)

  let visit nextPage =
    use previousPage = content.Value
    content.Value <- nextPage

  do
    loggedIn |> Observable.subscribe
      (fun userAccessToken ->
          let accessToken =
            { accessToken with UserAccessToken = Some userAccessToken }
          accessToken.Save()
          new MainPage(accessToken.ApplicationAccessToken, userAccessToken) |> visit
      )
      |> ignore

  member this.Content =
    content
