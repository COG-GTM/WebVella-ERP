<!--{"sort_order":1, "name": "overview", "label": "Overview"}-->
# Overview

If it is needed to regularly execute a method in order to implement a functionality, the background jobs is the way to go. To do that you need to create a schedule plan and subscribe a method from you code, that needs to be executed. Two hosted services, registered by `AddErp()`, run the platform's managers. `ErpJobScheduleService` runs `ScheduleManager`, which checks the schedule plans about every 10 seconds and creates jobs that are due. `ErpJobProcessService` runs `JobManager`, which starts pending jobs on the `JobPool`, up to 20 at a time. All such jobs are logged and you are able to review when they were scheduled and what was the result of the execution.

All such jobs are executed in the background in a separate threads, which limits the impact on the system's performance.

The functionality is provided by methods decorated with special attributes and the `ScheduleManager` service.

Definitions of the used terms are:

* background job type - the background job definition method that should be executed 
* background job - the instance of the background job type that was or will be run in a specific time.
* schedule plan - the scheduling plan of the background job type, upon which will be decided when to schedule a background job type to be executed.

## Turning background jobs on and off

Background jobs are on unless `Settings:EnableBackgroundJobs` is `false` in the site's `Config.json`. The committed `Config.json` files set it to `false`, so set it to `true` when you want jobs to run. The misspelled key `EnableBackgroungJobs` still works for older configs.

## Jobs shipped with the plugins

| Job type | Plugin | Schedule plan created by the plugin |
|----------|--------|-------------------------------------|
| Clear job and error logs job | SDK | every 1440 minutes (daily) |
| Sample job | SDK | none (the sample plan is commented out) |
| Start tasks on start_date | Project | "Start tasks on start_date", every 1440 minutes |
| Process SMTP queue | Mail | "Start tasks to process SMTP email queue", every 10 minutes |

The Next, Crm, MicrosoftCDM and TravelERP plugins have no jobs. `WebVella.Erp.ConsoleApp` does not start the hosted services, so no jobs run in it.
