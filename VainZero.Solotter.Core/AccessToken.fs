namespace VainZero.Solotter

open System.IO
open System.Runtime.Serialization

[<DataContract>]
type ApplicationAccessToken =
  {
    [<field: DataMember>]
    ConsumerKey:
      string
    [<field: DataMember>]
    ConsumerSecret:
      string
  }

[<DataContract>]
type UserAccessToken =
  {
    [<field: DataMember>]
    AccessToken:
      string
    [<field: DataMember>]
    AccessSecret:
      string
  }

[<DataContract>]
type AccessToken =
  {
    [<field: DataMember>]
    ApplicationAccessToken:
      ApplicationAccessToken
    [<field: DataMember>]
    UserAccessToken:
      option<UserAccessToken>
  }
with
  static member private FilePath =
    @"VainZero.Solotter.AccessToken.xml"
    
  static member Load() =
    use stream = File.OpenRead(AccessToken.FilePath)
    let serializer = DataContractSerializer(typeof<AccessToken>)
    serializer.ReadObject(stream) :?> AccessToken

  member this.Save() =
    use stream = File.OpenWrite(AccessToken.FilePath)
    let serializer = DataContractSerializer(typeof<AccessToken>)
    serializer.WriteObject(stream, this)

  member this.Login(userAccessToken) =
    { this with UserAccessToken = Some userAccessToken }

  member this.Logout() =
    { this with UserAccessToken = None }
