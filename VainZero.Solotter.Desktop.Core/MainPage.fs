namespace VainZero.Solotter.Desktop

open VainZero.Solotter

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

  interface IPage
