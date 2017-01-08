namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Threading
open System.Windows
open DotNetKit.FSharp
open DotNetKit.Functional.Commands
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type Tweet(tweet: Tweetinvi.Models.ITweet) =
  member this.Id = tweet.Id
  member this.Text = tweet.Text
  member this.CreatorName = tweet.CreatedBy.Name
  member this.CreatorScreenName = tweet.CreatedBy.ScreenName
  member this.CreationDateTime = tweet.CreatedAt.ToLocalTime()

  member val CopyCommand =
    new ReactiveCommand()

  member val DeleteCommand =
    new ReactiveCommand()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module Tweet =
  let copyText (notifier: Notifier) (this: Tweet) =
    Clipboard.SetText(this.Text)
    notifier.NotifyInfo("Copied.")

  let deleteAsync twitter (notifier: Notifier) (tweet: Tweet) =
    async {
      let message =
        sprintf "Do you want to delete this tweet?\r\n\r\n%s" tweet.Text
      if notifier.Confirm(message) then
        let! result = Tweetinvi.TweetAsync.DestroyTweet(tweet.Id) |> Async.AwaitTask
        if result then
          notifier.NotifyInfo("Deleted successfully.")
          return true
        else
          notifier.NotifyInfo("Deletion failed.")
          return false
      else
        return false
    }

[<Sealed>]
type Timeline(tweets) =
  member this.Tweets: ReadOnlyReactiveCollection<Tweet> =
    tweets

[<Sealed>]
type SelfTimeline(twitter: Tweetinvi.Models.ITwitterCredentials, notifier) =
  let disposables =
    new CompositeDisposable()

  let items =
    new ReactiveCollection<_>()
    |> tap disposables.Add

  let cancellationTokenSource =
    new CancellationTokenSource()
    |> tap (fun cts -> disposables.Add(new CancellationDisposable(cts)))

  let userStream =
    Tweetinvi.Stream.CreateUserStream(twitter)

  let deleteTweet tweet =
    async {
      let! isDeleted = tweet |> Tweet.deleteAsync twitter notifier
      if isDeleted then
        items.RemoveOnScheduler(tweet)
    } |> Async.Start

  let selfTweet tweet =
    Tweet(tweet)
    |> tap
      (fun tweet ->
        tweet.CopyCommand |> Observable.subscribe (fun _ -> Tweet.copyText notifier tweet)
        |> disposables.Add
        tweet.DeleteCommand |> Observable.subscribe (fun _ -> deleteTweet tweet)
        |> disposables.Add
      )

  let collectAsync =
    async {
      let! selfUser =
        Tweetinvi.UserAsync.GetAuthenticatedUser(twitter) |> Async.AwaitTask
      let! oldTweets =
        Tweetinvi.TimelineAsync.GetUserTimeline(selfUser, 10) |> Async.AwaitTask
      let oldTweets =
        oldTweets |> Seq.map selfTweet
      items.AddRangeOnScheduler(oldTweets)
      return!
        userStream.StartStreamAsync() |> Async.AwaitTask
    }

  do
    userStream.TweetCreatedByMe.Subscribe(fun e -> items.InsertOnScheduler(0, selfTweet e.Tweet))
    |> disposables.Add

  do
    Async.Start(collectAsync, cancellationTokenSource.Token)

  let dispose () =
    disposables.Dispose()

  let timeline =
    Timeline(items.ToReadOnlyReactiveCollection())

  member this.Timeline =
    timeline

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
