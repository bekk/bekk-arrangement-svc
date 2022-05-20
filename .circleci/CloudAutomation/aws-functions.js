var AWS = require('aws-sdk');
const REGION = 'eu-central-1'
const ECR_ENDPOINT = '882089634282.dkr.ecr.eu-central-1.amazonaws.com'

var secretsmanagerClient = new AWS.SecretsManager({
    endpoint: 'https://secretsmanager.eu-central-1.amazonaws.com',
    region: REGION
});

var ecsClient = new AWS.ECS({
    endpoint: 'https://ecs.eu-central-1.amazonaws.com',
    region: REGION
});


function createTaskDefinition(secret, staticVariables, imageName, taskdefinition, appName) { 
    taskdefinition.containerDefinitions[0]['environment'] = addSecretsToEnvironmentVariables(secret, staticVariables);
    taskdefinition.containerDefinitions[0]['image'] = `${ECR_ENDPOINT}/${appName}:${imageName}`;
    return taskdefinition;
}

function addSecretsToEnvironmentVariables(secrets, environmentVariables) {
    console.log('Merging environment variables with secrets');
    const secretsAsJson = JSON.parse(secrets);
    return environmentVariables.map(({name, value}) => {
        const secretValue = secretsAsJson[name];
        return { 
            name,
            value: secretValue || value
        }
    });
}

async function getSecret(secretName) { 
    console.log(`Retrieving secrets with name ${secretName}`);
    return new Promise((resolve, reject) => {
        secretsmanagerClient.getSecretValue({SecretId: secretName}, function(err, data) {
            if(err) {
                if(err.code === 'ResourceNotFoundException')
                    console.log('The requested secret ' + secretName + ' was not found');
                else if(err.code === 'InvalidRequestException')
                    console.log('The request was invalid due to: ' + err.message);
                else if(err.code === 'InvalidParameterException')
                    console.log('The request had invalid params: ' + err.message);
                reject(err.message);
            }
            else {
                if(data.SecretString !== '') {
                    console.log('Secret retrieved');
                    resolve(data.SecretString);
                } else {
                    console.log('Secret string was empty. Check aws secrets manager');
                }
            }
        });
    });
}

async function registerTaskDefinition(taskDefinition) {
    console.log('Register task definition');
    return new Promise((resolve, reject) => {
        ecsClient.registerTaskDefinition(taskDefinition, function(err, data) {
            if(err) { 
                reject(err);
            }
            else {
                console.log(`Registered task definition ${data}`)
                resolve(data);
            }
        })
    });
}

async function updateService(taskdefinitionName, serviceName, clusterName) {
    console.log(`Updating service ${serviceName}`)
    var params = {
        service: serviceName, 
        taskDefinition: taskdefinitionName,
        cluster: clusterName
       };
    return new Promise((resolve, reject) => {
        ecsClient.updateService(params, function(err, data) {
            if (err) {
                console.log(err);
                reject(err);
            }       
            else  {
                console.log(data);
                console.log(`Successfully updated service ${serviceName} to use task definition ${taskdefinitionName}`)
                resolve(data);
            }
        });
    });
}

module.exports.getSecret = getSecret;
module.exports.registerTaskDefinition = registerTaskDefinition;
module.exports.createTaskDefinition = createTaskDefinition;
module.exports.updateService = updateService;

