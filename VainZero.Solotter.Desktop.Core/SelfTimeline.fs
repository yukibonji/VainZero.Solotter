namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Disposables
open System.Threading
open DotNetKit.FSharp
open Reactive.Bindings
open System.Windows

[<Sealed>]
type Tweet(tweet: Tweetinvi.Models.ITweet) =
  member this.Id = tweet.Id
  member this.Text = tweet.Text
  member this.CreatorName = tweet.CreatedBy.Name
  member this.CreatorScreenName = tweet.CreatedBy.ScreenName
  member this.CreationDateTime = tweet.CreatedAt.ToLocalTime()

  member val DeleteCommand =
    new ReactiveCommand()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module Tweet =
  let deleteAsync twitter (tweet: Tweet) =
    async {
      let message =
        sprintf "Do you want to delete this tweet?\r\n\r\n%s" tweet.Text
      if
        MessageBox.Show(message, "Solotter", MessageBoxButton.YesNo)
        = MessageBoxResult.Yes
      then
        let! result = Tweetinvi.TweetAsync.DestroyTweet(tweet.Id) |> Async.AwaitTask
        if result then
          MessageBox.Show("Deleted successfully.") |> ignore
          return true
        else
          MessageBox.Show("Deletion failed.") |> ignore
          return false
      else
        return false
    }

[<Sealed>]
type Timeline(tweets) =
  member this.Tweets: ReadOnlyReactiveCollection<Tweet> =
    tweets

[<Sealed>]
type SelfTimeline(twitter: Tweetinvi.Models.ITwitterCredentials) =
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
      let! isDeleted = tweet |> Tweet.deleteAsync twitter
      if isDeleted then
        items.RemoveOnScheduler(tweet)
    } |> Async.Start

  let selfTweet tweet =
    Tweet(tweet)
    |> tap
      (fun tweet ->
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
