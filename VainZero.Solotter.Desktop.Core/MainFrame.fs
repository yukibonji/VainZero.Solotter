namespace VainZero.Solotter.Desktop

open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type MainFrame() =
  let accessToken = AccessToken.Load()

  let initialPage =
    match accessToken.UserAccessToken with
    | Some token ->
      MainPage(accessToken.ApplicationAccessToken, token) :> IPage
    | None ->
      AuthenticationPage(accessToken.ApplicationAccessToken) :> IPage

  let content =
    new ReactiveProperty<_>(initialValue = initialPage)

  member this.Content =
    content
