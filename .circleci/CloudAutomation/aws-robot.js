/*
 * About aws-robot
 *
 * Aws robot is meant to be used by a build agent to create a task definition
 * with all environment variables set. If it is has a git tag it will create
 * a production release. If not, it will create development release and auto update the service.
 * It is also recommended that you do a local dry run before push. Which is done by:
 *
 * npm install
 * node aws-robot.js dryrun
 *
 * What you need todo:
 *
 *
 * 1. Create a circleci build for this project
 * 2. Add AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, as enviroment variables to circleci. The values is stored in lastpass
 * 3. Create a repository in aws ECS
 * 4. Update taskdefinition.x.json and environment.x.json
 * 5. Add all secret app settings to aws secrets manager
 * ( You will need to create a service manually.)
 */
const aws = require('./aws-functions.js');
var taskdefinitionDevelopment = require('./taskdefinition.development.json');
var taskdefinitionProduction = require('./taskdefinition.production.json');
var staticDevelopmentVariables = require('./environment.development.json');
var staticProductionVariables = require('./environment.production.json');

const REPOSITORY_NAME = 'arrangement-svc'; //The name of the aws repository name. Not GITHUB
const SERVICE_NAME_DEV = 'arrangement-svc-dev';
const SERVICE_NAME_PROD = 'arrangement-svc-prod';

//DON'T TOUCH. SHOULD NEVER BE CHANGED
const CLUSTER_NAME_DEV = 'ecs-cluster-dev';
const DEVELOPMENT_SECRETS = 'development_secrets';
const CLUSTER_NAME_PROD = 'ecs-cluster-prod';
const PRODUCTION_SECRETS = 'production_secrets';

async function run() {
  const branchName = process.env.CIRCLE_BRANCH;
  const isDryRun =
    process.argv[2] === 'dryrun' || process.env.CIRCLE_SHA1 === null;
  const hasTag = !!process.env.CIRCLE_TAG;
  const isMaster = branchName === 'master';
  const hasBranchNameDEVELOPMENT = branchName.includes('DEVELOPMENT');

  console.log(`Branch: ${branchName}. Tag; ${process.env.CIRCLE_TAG}`);

  try {
    if (isDryRun) {
      console.log(
        'DRY RUN. Will retrieve secrets, but will not register task definition or update service'
      );
      const secrets = await aws.getSecret(DEVELOPMENT_SECRETS);
      const taskDefinition = aws.createTaskDefinition(
        secrets,
        staticDevelopmentVariables,
        'DRYRUN-TEST',
        taskdefinitionDevelopment,
        REPOSITORY_NAME
      );
      console.log(
        'Would have created a task definition like this:',
        taskDefinition
      );
    } else if (hasTag) {
      console.log(
        `Creating task definition for production for release with tag ${process.env.CIRCLE_TAG}`
      );
      const secrets = await aws.getSecret(PRODUCTION_SECRETS);
      const taskDefinition = aws.createTaskDefinition(
        secrets,
        staticProductionVariables,
        process.env.CIRCLE_TAG,
        taskdefinitionProduction,
        REPOSITORY_NAME
      );
      const registeredTaskDefinition = await aws.registerTaskDefinition(
        taskDefinition
      );
      console.log(
        `Successfully created taskdefinition with name ${registeredTaskDefinition.taskDefinition.family}:${registeredTaskDefinition.taskDefinition.revision}.`
      );
      const updateService = await aws.updateService(
        `${registeredTaskDefinition.taskDefinition.family}:${registeredTaskDefinition.taskDefinition.revision}`,
        SERVICE_NAME_PROD,
        CLUSTER_NAME_PROD
      );
    } else if (isMaster || hasBranchNameDEVELOPMENT) {
      console.log(
        `Started creating development task definition for commit ${process.env.CIRCLE_SHA1}`
      );
      const secrets = await aws.getSecret(DEVELOPMENT_SECRETS);
      const taskDefinition = aws.createTaskDefinition(
        secrets,
        staticDevelopmentVariables,
        process.env.CIRCLE_SHA1,
        taskdefinitionDevelopment,
        REPOSITORY_NAME
      );
      const registeredTaskDefinition = await aws.registerTaskDefinition(
        taskDefinition
      );
      const updateService = await aws.updateService(
        `${registeredTaskDefinition.taskDefinition.family}:${registeredTaskDefinition.taskDefinition.revision}`,
        SERVICE_NAME_DEV,
        CLUSTER_NAME_DEV
      );
    } else {
      console.log(
        'Will not deploy app since you are not on master or a tagged build'
      );
    }
  } catch (err) {
    console.log(`FAILED: ${err}`);
    process.exit(1);
  }
  console.log('Done');
}
run();
