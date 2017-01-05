namespace VainZero.Solotter.Desktop

open System
open VainZero.Solotter

type AuthenticationAction =
  | Login
    of UserAccessToken
  | Logout

type IAuthenticationPage =
  inherit IObservable<AuthenticationAction>
  inherit IDisposable

  abstract UserAccessToken: option<UserAccessToken>
