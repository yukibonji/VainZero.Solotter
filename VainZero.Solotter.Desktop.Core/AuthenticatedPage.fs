namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Threading.Tasks
open System.Threading
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type TweetEditor(twitter: Tweetinvi.Models.ITwitterCredentials) =
  [<Literal>]
  let TweetLength = 140

  let text =
    new ReactiveProperty<_>(initialValue = "")

  let textRemainingLength =
    text
      .Select(fun text -> TweetLength - text.Length)
      .ToReadOnlyReactiveProperty()

  let submitCommand =
    textRemainingLength
      .Select(fun length -> 0 <= length && length < TweetLength)
      .ToReactiveCommand()

  let submit () =
    Tweetinvi.TweetAsync.PublishTweet(text.Value)
      .ToObservable()
      .Do(fun _ -> text.Value <- "")

  let subscription =
    submitCommand
      .Select(fun _ -> submit())
      .Concat()
      .Publish()
      .Connect()

  member this.Text =
    text

  member this.TextRemainingLength =
    textRemainingLength

  member this.SubmitCommand =
    submitCommand

[<Sealed>]
type Tweet(tweet: Tweetinvi.Models.ITweet) =
  member this.Id = tweet.Id
  member this.Text = tweet.Text
  member this.CreatorName = tweet.CreatedBy.Name
  member this.CreatorScreenName = tweet.CreatedBy.ScreenName
  member this.CreationDateTime = tweet.CreatedAt.ToLocalTime()

[<Sealed>]
type Timeline(tweets) =
  member this.Tweets: ReadOnlyReactiveCollection<Tweet> =
    tweets

[<Sealed>]
type SelfTimeline(twitter: Tweetinvi.Models.ITwitterCredentials) =
  let items =
    new ReactiveCollection<_>()

  let cancellationTokenSource =
    new CancellationTokenSource()

  let userStream =
    Tweetinvi.Stream.CreateUserStream(twitter)

  let collectAsync =
    async {
      let! selfUser =
        Tweetinvi.UserAsync.GetAuthenticatedUser(twitter) |> Async.AwaitTask
      let! oldTweets =
        Tweetinvi.TimelineAsync.GetUserTimeline(selfUser, 10) |> Async.AwaitTask
      let oldTweets =
        oldTweets |> Seq.map Tweet
      items.AddRangeOnScheduler(oldTweets)
      return!
        userStream.StartStreamAsync() |> Async.AwaitTask
    }

  let subscription =
    userStream.TweetCreatedByMe.Subscribe(fun e -> items.InsertOnScheduler(0, Tweet(e.Tweet)))

  do
    Async.Start(collectAsync, cancellationTokenSource.Token)

  let dispose () =
    subscription.Dispose()
    cancellationTokenSource.Cancel()
    cancellationTokenSource.Dispose()
    items.Dispose()

  let timeline =
    Timeline(items.ToReadOnlyReactiveCollection())

  member this.Timeline =
    timeline

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

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
