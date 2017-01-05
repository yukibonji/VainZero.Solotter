namespace VainZero.Solotter.Desktop

open System
open VainZero.Solotter

type AuthenticationAction =
  | Login
    of UserAccessToken
  | Logout

type IPage =
  inherit IObservable<AuthenticationAction>
  inherit IDisposable
