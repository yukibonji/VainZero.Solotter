namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open Reactive.Bindings
open VainZero.Solotter

type TabItem =
  {
    Header:
      string
    Content:
      obj
  }
with
  static member Create(name, content) =
    {
      Header =
        name
      Content =
        content
    }

[<Sealed>]
type AuthenticatedPage(applicationAccessToken, userAccessToken) =
  let twitter =
    let (a: ApplicationAccessToken) = applicationAccessToken
    let (u: UserAccessToken) = userAccessToken
    Tweetinvi.Auth.SetUserCredentials
      ( a.ConsumerKey
      , a.ConsumerSecret
      , u.AccessToken
      , u.AccessSecret
      )

  let tweetEditor =
    new TweetEditor(twitter)

  let selfTimeline =
    new SelfTimeline(twitter)

  let tabItems =
    [|
      TabItem.Create("Empty", null)
      TabItem.Create("Self", selfTimeline)
    |]

  let selectedTabItem =
    new ReactiveProperty<_>(initialValue = tabItems.[0])

  let logoutCommand =
    new ReactiveCommand()

  let dispose () =
    logoutCommand.Dispose()
    selfTimeline.Dispose()

  member this.TweetEditor =
    tweetEditor

  member this.SelfTimeline =
    selfTimeline

  member this.TabItems =
    tabItems

  member this.SelectedTabItem =
    selectedTabItem

  member this.LogoutCommand =
    logoutCommand

  member this.Dispose() =
    dispose ()

  interface IObservable<AuthenticationAction> with
    override this.Subscribe(observer) =
      logoutCommand.Select(fun _ -> Logout).Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface IAuthenticationPage
