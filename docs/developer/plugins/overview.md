<!--{"sort_order":1, "name": "overview", "label": "Overview"}-->
# What is a WebVella ERP Plugin

A plugin is in general a Razor Class Library that has some specific in order to fully utilized the platform's capabilities.

The purpose of the plugin is to provide:

- tag helpers
- page components
- pages or page routing overrides
- business logic with the help of Hooks
- extend the web api with its own controllers
- code based datasources 
- register background jobs to be run by the system
- register to your page

## Plugins in this repository

The solution includes the SDK, Next, Project, Mail, Crm, MicrosoftCDM and TravelERP plugins. A site registers a plugin with `app.UseErpPlugin<T>()` before calling `app.UseErp()`. For what each plugin adds and which site registers it, see [Architecture](../introduction/architecture.md).
