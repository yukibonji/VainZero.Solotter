namespace VainZero.Solotter.Desktop

open System.Reactive.Linq
open System.Reactive.Threading.Tasks
open Reactive.Bindings

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
