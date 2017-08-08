namespace VainZero.Solotter

open System.Windows

[<AbstractClass>]
type Notifier() =
  abstract NotifyInfo: string -> unit
  abstract Confirm: string -> bool

[<Sealed>]
type MessageBoxNotifier(caption: string) =
  inherit Notifier()

  override this.NotifyInfo(message) =
    MessageBox.Show(message, caption) |> ignore

  override this.Confirm(message) =
    MessageBox.Show(message, caption, MessageBoxButton.YesNo) = MessageBoxResult.Yes
