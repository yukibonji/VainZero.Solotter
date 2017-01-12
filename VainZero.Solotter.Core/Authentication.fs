namespace VainZero.Solotter

type Authentication =
  {
    ApplicationAccessToken:
      ApplicationAccessToken
    UserAccessToken:
      UserAccessToken
    Twitter:
      Tweetinvi.Models.ITwitterCredentials
  }
with
  static member Create(applicationAccessToken, userAccessToken, twitter) =
    {
      ApplicationAccessToken =
        applicationAccessToken
      UserAccessToken =
        userAccessToken
      Twitter =
        twitter
    }

  static member FromAccessToken(applicationAccessToken, userAccessToken) =
    let (a: ApplicationAccessToken) = applicationAccessToken
    let (u: UserAccessToken) = userAccessToken
    let twitter =
      Tweetinvi.Auth.SetUserCredentials
        ( a.ConsumerKey
        , a.ConsumerSecret
        , u.AccessToken
        , u.AccessSecret
        )
    Authentication.Create(a, u, twitter)
