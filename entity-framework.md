Update EF tools: 
    dotnet tool update --global dotnet-ef

Change to Fakemail.Data.EntityFramework dir

Initial create:
    dotnet ef migrations add InitialCreate

Remove last migration:
    dotnet ef migrations remove

Add new migration:
    dotnet ef migrations add MyCoolNewFeature [same as intial create]

Apply all migrations (to the database specified in the data context class):
    dotnet ef database update

Create migration script from empty database to latest version
    dotnet ef migrations script

Create migration script from specified version to latest version
    dotnet ef migrations script CurrentVersion NewVersion