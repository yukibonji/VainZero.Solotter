namespace VainZero.Solotter.Desktop

open System
open System.Threading
open Reactive.Bindings

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
