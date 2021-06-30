# Arrangement Service

An F# service for collecting and maintaining data about events.

## Environments
- Development = https://skjer-dev.bekk.no/

## Requirements

- .NET Core SDK 3.0
- SQL server
- Latest version of Mono

### Recommended tools

- Ionide in Visual Studio Code IDE (or Rider)
- Azure Data studio (works on all OS)
- Docker

## Start local

### Your first Fsharp project?

- Take a look at https://fsharp.org/use/mac/ to setup your develop environment.

### First time setup

- Open the project from the `src` folder in the terminal to avoid error messages in Visual Studio Code
- You will need to create the database `arrangement-db` on you local SQL server, if it does not already exist. Use the query ```CREATE DATABASE [arrangement-db]```. 
- In `appsettings.json`, make sure your database is running on this address. If this doesn't work you can try to connect to the development database. You can find the address for the development database in the secrets manager on AWS.

### Run the app

- In the src folder run `$ dotnet watch run`
- The service runs at  `http://localhost:5000/` (currently no Swagger docs)
- If everything works, `http://localhost:5000/health` should return 200 OK.

### Run the tests
- In the src folder run `$ dotnet watch run`

#### 🔥Hot tip🔥

Brew yourself some `tmux`, navigate to the root folder and run

```
./tmux.sh
```

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
- Make sure the table has at least one row of valid data (it's used to generate the `DbSchema`)
- Delete the `obj` and `bin` folders
- Delete the file `DbSchema.json`
- Run `$ dotnet run`
- The migration should be performed, and generate a new `DbSchema`. If it does not, try to delete the obj and bin folders again, and run or build it one more time.
