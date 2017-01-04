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

  let subscription =
    submitCommand |> Observable.subscribe
      (fun _ ->
        async {
          let! tweet =
            Tweetinvi.TweetAsync.PublishTweet(text.Value) |> Async.AwaitTask
          text.Value <- ""
        } |> Async.Start
      )

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

  let readOnlyItems =
    items.ToReadOnlyReactiveCollection()

  member this.Items =
    readOnlyItems

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

[<Sealed>]
type MainPage(applicationAccessToken, userAccessToken) =
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

  let dispose () =
    selfTimeline.Dispose()

  member this.TweetEditor =
    tweetEditor

  member this.SelfTimeline =
    selfTimeline

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface IPage
