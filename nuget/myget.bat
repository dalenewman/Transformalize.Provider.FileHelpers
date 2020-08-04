nuget pack Transformalize.Provider.FileHelpers.nuspec -OutputDirectory "c:\temp\modules"
nuget pack Transformalize.Provider.FileHelpers.Autofac.nuspec -OutputDirectory "c:\temp\modules"

nuget push "c:\temp\modules\Transformalize.Provider.FileHelpers.0.8.22-beta.nupkg" -source https://www.myget.org/F/transformalize/api/v3/index.json
nuget push "c:\temp\modules\Transformalize.Provider.FileHelpers.Autofac.0.8.22-beta.nupkg" -source https://www.myget.org/F/transformalize/api/v3/index.json