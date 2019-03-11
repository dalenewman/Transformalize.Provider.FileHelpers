nuget pack Transformalize.Provider.FileHelpers.nuspec -OutputDirectory "c:\temp\modules"
nuget pack Transformalize.Provider.FileHelpers.Autofac.nuspec -OutputDirectory "c:\temp\modules"

nuget push "c:\temp\modules\Transformalize.Provider.FileHelpers.0.3.31-beta.nupkg" -source https://api.nuget.org/v3/index.json
nuget push "c:\temp\modules\Transformalize.Provider.FileHelpers.Autofac.0.3.31-beta.nupkg" -source https://api.nuget.org/v3/index.json