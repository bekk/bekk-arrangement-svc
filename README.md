# Arrangement Service

An F# service for collecting and maintaining data about events.

## Environments

- Development = https://skjer-dev.bekk.no/

## Requirements

- .NET Core SDK 6.0
- SQL server

### Recommended tools

- Ionide in Visual Studio Code IDE (or Rider)
- Azure Data studio (works on all OS)
- Docker

## Start local

### Your first Fsharp project?

- Take a look at https://fsharp.org/use/mac/ to setup your develop environment.

### First time setup

- Open the project from the `src` folder in the terminal to avoid error messages in Visual Studio Code
- You will need to create the database `arrangement-db` on you local SQL server, if it does not already exist. Use the query `CREATE DATABASE [arrangement-db]`. If you dont have a local SQL server -> https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver15&pivots=cs1-cmd
- In `appsettings.json`, make sure your database is running on this address. If this doesn't work you can try to connect to the development database. You can find the address for the development database in the secrets manager on AWS.

### Run the app

- In the src folder run `$ dotnet watch run`
- The service runs at `http://localhost:5000/` (currently no Swagger docs)
- If everything works, `http://localhost:5000/health` should return 200 OK.
- 
## Deploy app

### Deploy to development

Push to the master branch will automatically deploy the app to development.
If you are on a branch the app will not be deployed **_Unless_** the branch name contains DEVELOPMENT.

### Deployment to production

Create a release from the master branch, and it should deploy to production.

### Deployment configuration

Deployment configuration can be found in the folder `.circleci`.
App deployment is done by the `aws-robot.js` which is found at `.circleci/CloudAutomation/aws-robot.js`.

## Architecture graph

TODO.

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
