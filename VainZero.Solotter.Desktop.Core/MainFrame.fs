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
      MainPage(accessToken.ApplicationAccessToken, token) :> IPage
    | None ->
      let page = AuthenticationPage(accessToken.ApplicationAccessToken)
      page.Authenticated.Subscribe
        (fun token ->
          loggedIn.OnNext(token)
        ) |> ignore
      page :> IPage

  let content =
    new ReactiveProperty<_>(initialValue = initialPage)

  do
    loggedIn |> Observable.subscribe
      (fun userAccessToken ->
          let accessToken =
            { accessToken with UserAccessToken = Some userAccessToken }
          accessToken.Save()
          content.Value <-
            MainPage(accessToken.ApplicationAccessToken, userAccessToken) :> IPage
      )
      |> ignore

  member this.Content =
    content
