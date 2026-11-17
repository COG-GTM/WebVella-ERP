<!--{"sort_order":2, "name": "getting-started", "label": "Getting started"}-->
# Getting started

## Setting up environment
 
1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). Every project in `WebVella.ERP3.sln` targets `net10.0`. You can build with Visual Studio 2026, JetBrains Rider or the `dotnet` CLI.

2. Install [PostgreSQL 16](https://www.postgresql.org/download/). [pgAdmin](https://www.pgadmin.org/download/) is optional, for managing the database.

## Start with seed project

1. Download or Clone the repository sources ($ git clone https://github.com/WebVella/WebVella-ERP-Seed.git ) .
This project contains ready to start web site project with all ERP libraries and resources installed from nuget packages.

2. Create an empty PostgreSQL database for the erp. When erp starts for first time, it will create 
necessary structure and data.

3. Edit the Config.json file and set the correct sql connection string. 

4. Run the project.

## Get, build and run the sources

1. Download or Clone the repository sources ($ git clone https://github.com/WebVella/WebVella-ERP.git) .

2. Create an empty PostgreSQL database for the erp. When erp starts for first time, it will create 
necessary structure and data.

3. Build the solution (`dotnet build WebVella.ERP3.sln`, or open it in your IDE).
Choose a host site as the startup project. `WebVella.Erp.Site` registers only the SDK plugin. The other sites, such as
`WebVella.Erp.Site.Project`, `WebVella.Erp.Site.Mail`, `WebVella.Erp.Site.Crm` and `WebVella.Erp.Site.TravelERP`, also register their plugin
(see [Architecture](architecture.md)). Edit that site's `Config.json` and set `Settings:ConnectionString`.
You may also want to change other values, like `TimeZoneName` or `EnableBackgroundJobs`, but you don't have to.

4. Run the application. The project is configured to open browser open on login screen when started. 
All erp functionality requires authentication. Use the default account email "**erp@webvella.com**" and password "**erp**"
to authenticate. 

5. If you want to develop your own components or applications, be sure that the WebVella.Erp.Plugins.SDK is included in your solution.
This application will help you greatly to create and manage ERP objects like Entities and Relations, although its not required because
ERP API can used directly to create and manage these objects.

