namespace VainZero.Solotter.Desktop

open System
open System.Reactive.Linq
open System.Reactive.Threading.Tasks
open Reactive.Bindings
open VainZero.Solotter

[<Sealed>]
type TweetEditor(twitter: Tweetinvi.Models.ITwitterCredentials, notifier: Notifier) =
  [<Literal>]
  let TweetLength = 140

  let text =
    new ReactiveProperty<_>(initialValue = "")

  let textRemainingLength =
    text
      .Select(fun text -> TweetLength - text.Length)
      .ToReadOnlyReactiveProperty()

  let submitCommand =
    text
      .Select(String.IsNullOrWhiteSpace >> not)
      .ToReactiveCommand()

  let submit () =
    Tweetinvi.TweetAsync.PublishTweet(text.Value).ToObservable()
    |> Observable.subscribe
      (function
        | null ->
          notifier.NotifyInfo("Submission failed.") |> ignore
        | tweet ->
          text.Value <- ""
      )
    |> ignore

  let subscription =
    submitCommand
      .Select(fun _ -> submit ())
      .Publish()
      .Connect()

  member this.Text =
    text

  member this.TextRemainingLength =
    textRemainingLength

  member this.SubmitCommand =
    submitCommand
