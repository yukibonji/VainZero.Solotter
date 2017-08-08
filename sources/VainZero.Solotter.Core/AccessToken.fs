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
  member this.Login(userAccessToken) =
    { this with UserAccessToken = Some userAccessToken }

  member this.Logout() =
    { this with UserAccessToken = None }

  static member Deserialize(stream: Stream) =
    let serializer = DataContractSerializer(typeof<AccessToken>)
    serializer.ReadObject(stream) :?> AccessToken

  member this.Serialize(stream: Stream) =
    let serializer = DataContractSerializer(typeof<AccessToken>)
    serializer.WriteObject(stream, this)

  static member private FilePath =
    @"VainZero.Solotter.AccessToken.xml"

  static member Load() =
    use stream = File.OpenRead(AccessToken.FilePath)
    AccessToken.Deserialize(stream)

  member this.Save() =
    let file = FileInfo(AccessToken.FilePath)
    if file.Exists then file.Delete()
    use stream = file.Create()
    this.Serialize(stream)
