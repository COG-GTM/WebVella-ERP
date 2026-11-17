<!--{"sort_order":1, "name": "overview", "label": "Overview"}-->
# Overview

An Application in WebVella ERP is the preferred way to target a specific process or selection of processes in a business. In example the Customer Relationship Management (CRM) makes sense to be bundled in one application, while the Project management is in another. This provides a very flexible way to limit the functionality to exactly what is needed for a specific department or even a job position.

From technical perspective, the application is like a lense selecting a specific parts of the platform for presentation like pages or components, which could be dedicated to a single application or shared between many.

All available applications are presented on the platform's home page:

![sdk home](/doc-images/sdk-home.png)

## Applications shipped in this repository

Plugins create applications in their schema patches. The applications you see depend on which plugins the host site registers (see [Architecture](../introduction/architecture.md)).

| Application (`name`) | Created by | Available in sites |
|----------------------|------------|--------------------|
| `sdk` | `WebVella.Erp.Plugins.SDK` | every site that registers the SDK plugin (all except `WebVella.Erp.Site.Next`) |
| `projects` | `WebVella.Erp.Plugins.Project` | `WebVella.Erp.Site.Project` |
| `mail` | `WebVella.Erp.Plugins.Mail` | `WebVella.Erp.Site.Mail` |
| `travelerp` | `WebVella.Erp.Plugins.TravelERP` | `WebVella.Erp.Site.TravelERP` |

`WebVella.Erp.Plugins.Next` adds shared entities but no application. `WebVella.Erp.Plugins.Crm` and `WebVella.Erp.Plugins.MicrosoftCDM` have their patches commented out, so they create no application yet. `WebVella.Erp.ConsoleApp` has no UI, so it shows no applications.
