# Arrangement Service

An F# service for collecting and maintaining data about events.

## Environments

- Development = https://skjer-dev.bekk.no/

## Requirements

- .NET Core SDK 6.0
- SQL server

## Start local

### Your first F# project?

- Take a look at https://fsharp.org/use/mac/ to setup your develop environment.

### First time setup

- Open the project from the `src` folder in the terminal to avoid error messages in Visual Studio Code
- You will need to create the database `arrangement-db` on you local SQL server, if it does not already exist. Use the query `CREATE DATABASE [arrangement-db]`. If you dont have a local SQL server -> https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver15&pivots=cs1-cmd
- In `appsettings.json`, make sure your database is running on this address. If this doesn't work you can try to connect to the development database. You can find the address for the development database in the secrets manager on AWS.

### Run the app

- In the src folder run `$ dotnet watch run`
- The service runs at `http://localhost:5000/`
- If everything works, `http://localhost:5000/health` should return 200 OK.

## Deploy app

### Deploy to development

Push to the master branch will automatically deploy the app to development.
If you want a preview of the service then append `-preview` to the title of any PR you make.

### Deployment to production

Create a release from the master branch, and it should deploy to production.

## Technologies we use
- [Giraffe](https://github.com/giraffe-fsharp/Giraffe) is the framework we use to setup the web application. It is an easy to use library which builds functional components on top of Kestrel. It also has extensive documentation.
- [Dapper](https://github.com/DapperLib/Dapper) is the ORM we are using. We went with plain Dapper here as we want to write SQL and escape any heavier ORM. This has some pros and cons, but has been working well for us.
- [FsToolkit.ErrorHandling](https://github.com/demystifyfp/FsToolkit.ErrorHandling) Is a library we use to better deal with Task and Result types. It allows us to remove some rightward drift (match case pyramids) and also simplify our error handling.

## Tests
The testing framework we use is [Expecto](https://github.com/haf/expecto).

Mock data is created using [Bogus](https://github.com/bchavez/Bogus).

As this system has some endpoints under authentication, some which are open and others which can only be accessed with special tokens or based on certain event characteristics (if it is an external event, for example) the tests in this system test the endpoints and the database themselves.

This is done to try and ensure that the constraints we have set are actually upheld.

In order to do this the tests need a running database and a token in order to be run.
As we are using MSSQL database and that does not support an in-memory variant, a docker image is needed.
We did consider using an Sqlite in-memory database, but we went with the docker image so the test and production system use the same database.

If you have a running database on your system, you could use that, but we recommend starting one just for testing, and deleting it after.
Running the tests will create a new image and database just for testing, and reuse that instance for later test sessions for performance reasons.
Note that you must remove the Expecto spinner when running tests in parallel as it deadlocks with requestlogging which writes/locks IO in Dispose.

To run tests:
```
$ ARRANGEMENT_SVC_TEST_JWT_TOKEN=MYTOKEN dotnet run --project Tests -- --no-spinner
```

Alternatively, you can export the variable to avoid typing it each time

```
$ export ARRANGEMENT_SVC_TEST_JWT_TOKEN=MYTOKEN
$ dotnet # .. rest as before
```

Running tests on changes is also supported by running `dotnet watch run --project Tests`

It's possible to disable podman integration to avoid requiring sudo password and migrations.

```
$ NO_PODMAN=1 NO_MIGRATION=1 dotnet # .. rest as before
```

The systems can be stress tested by running in a loop

```
# Run for two minutes
$ dotnet run -- --no-spinner --stress 2 --stress-memory-limit 2048
```

## Migrating the database

- Create a new .sql file with a number prefix (increment the highest existing prefix) with your new migration
- Run `$ dotnet run`

## Shortnames
Shortnames is a way to change the URL of an event.
Events are unique in the database, so only 1 row can have a specific shortname at any given time.

A typical event URL is `https://skjer.bekk.no/events/84427e54-54cd-4a74-8a53-cb0e4cc97004` with a shortname however you can replace the GUID with a string -> `https://skjer-dev.bekk.no/events/my-event`.
Shortnames are added when creating or editing an event.
A shortname can only be taken if:
- It is not currently used by an active event.
- The event that has it has ended (`endDate` in the past).
- The event that has it is cancelled.
